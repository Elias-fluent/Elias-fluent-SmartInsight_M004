using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Exceptions;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Security;
using SmartInsight.Data.Repositories;
using System.Security.Cryptography;
using System.Text.Json;

namespace SmartInsight.Knowledge.Connectors;

/// <summary>
/// Implementation of the credential management system
/// </summary>
public class CredentialManager : ICredentialManager
{
    private readonly IRepository<Credential> _repository;
    private readonly CredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CredentialManager> _logger;
    private readonly string _encryptionKey;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Creates a new credential manager
    /// </summary>
    /// <param name="repository">Generic repository for credentials</param>
    /// <param name="credentialRepository">Specialized credential repository</param>
    /// <param name="unitOfWork">Unit of work for transactions</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger</param>
    public CredentialManager(
        IRepository<Credential> repository,
        CredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<CredentialManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get encryption key from configuration or environment
        _encryptionKey = configuration["Credentials:EncryptionKey"] 
            ?? Environment.GetEnvironmentVariable("CREDENTIAL_ENCRYPTION_KEY") 
            ?? throw new InvalidOperationException("No encryption key configured for credentials");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task StoreCredentialAsync(
        string key, 
        string value, 
        string? source = null, 
        string? group = null, 
        string? metadata = null, 
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        if (value == null)
            throw new ArgumentNullException(nameof(value));
            
        try
        {
            await _lock.WaitAsync();
            
            try
            {
                // Check if credential already exists
                var existingCredential = await _credentialRepository.GetByKeyAsync(key);
                if (existingCredential != null)
                {
                    // Update existing credential
                    EncryptCredentialValue(value, out string encryptedValue, out string iv);
                    
                    existingCredential.EncryptedValue = encryptedValue;
                    existingCredential.IV = iv;
                    existingCredential.Source = source;
                    existingCredential.Group = group;
                    existingCredential.Metadata = metadata;
                    existingCredential.ExpiresAt = expiresAt;
                    existingCredential.ModifiedAt = DateTime.UtcNow;
                    existingCredential.IsEnabled = true;
                    
                    await _repository.UpdateAsync(existingCredential);
                    await _unitOfWork.SaveChangesAsync();
                    
                    _logger.LogDebug("Updated credential: {Key}", key);
                }
                else
                {
                    // Create new credential
                    EncryptCredentialValue(value, out string encryptedValue, out string iv);
                    
                    var newCredential = new Credential
                    {
                        Key = key,
                        EncryptedValue = encryptedValue,
                        IV = iv,
                        Source = source,
                        Group = group,
                        Metadata = metadata,
                        ExpiresAt = expiresAt,
                        IsEnabled = true
                    };
                    
                    await _repository.AddAsync(newCredential);
                    await _unitOfWork.SaveChangesAsync();
                    
                    _logger.LogDebug("Created credential: {Key}", key);
                }
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store credential: {Key}", key);
            throw CredentialException.Storage(key, ex);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetCredentialAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        try
        {
            var credential = await _credentialRepository.GetByKeyAsync(key);
            if (credential == null || !credential.IsEnabled)
            {
                _logger.LogWarning("Credential not found or disabled: {Key}", key);
                return null;
            }
            
            // Check for expiration
            if (credential.ExpiresAt.HasValue && credential.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Credential has expired: {Key}", key);
                return null;
            }
            
            // Update access statistics (fire and forget)
            _ = _credentialRepository.UpdateLastAccessedAsync(credential.Id);
            
            // Decrypt and return the value
            try
            {
                var decryptedValue = DecryptCredentialValue(credential.EncryptedValue, credential.IV);
                return decryptedValue;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Failed to decrypt credential: {Key}", key);
                throw CredentialException.Decryption(key, ex);
            }
        }
        catch (CredentialException)
        {
            throw; // Re-throw credential exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credential: {Key}", key);
            throw CredentialException.Retrieval(key, ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasCredentialAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        try
        {
            var credential = await _credentialRepository.GetByKeyAsync(key);
            if (credential == null || !credential.IsEnabled)
                return false;
                
            // Check for expiration
            if (credential.ExpiresAt.HasValue && credential.ExpiresAt < DateTime.UtcNow)
                return false;
                
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking credential existence: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCredentialAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        try
        {
            await _lock.WaitAsync();
            
            try
            {
                var credential = await _credentialRepository.GetByKeyAsync(key);
                if (credential == null)
                    return false;
                    
                await _repository.DeleteAsync(credential);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogDebug("Deleted credential: {Key}", key);
                return true;
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete credential: {Key}", key);
            throw new CredentialException(
                $"Failed to delete credential: {key}", 
                CredentialOperationType.Other, 
                key, 
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetCredentialKeysAsync(string? source = null, string? group = null)
    {
        try
        {
            var keys = await _credentialRepository.GetCredentialKeysAsync(source, group);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credential keys");
            throw new CredentialException(
                "Failed to retrieve credential keys", 
                CredentialOperationType.Retrieval, 
                null, 
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RotateCredentialAsync(string key, string newValue, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        if (newValue == null)
            throw new ArgumentNullException(nameof(newValue));
            
        try
        {
            await _lock.WaitAsync();
            
            try
            {
                var credential = await _credentialRepository.GetByKeyAsync(key);
                if (credential == null)
                    return false;
                    
                // Store rotation history
                var rotationEntry = new CredentialRotationEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Reason = reason ?? "Manual rotation"
                };
                
                var rotationHistory = new List<CredentialRotationEntry>();
                
                // Parse existing history if available
                if (!string.IsNullOrEmpty(credential.RotationHistory))
                {
                    try
                    {
                        rotationHistory = JsonSerializer.Deserialize<List<CredentialRotationEntry>>(
                            credential.RotationHistory, _jsonOptions) ?? new List<CredentialRotationEntry>();
                    }
                    catch
                    {
                        // If parsing fails, start with a new history
                        rotationHistory = new List<CredentialRotationEntry>();
                    }
                }
                
                // Add new entry and keep max 10 entries
                rotationHistory.Add(rotationEntry);
                if (rotationHistory.Count > 10)
                    rotationHistory = rotationHistory.Skip(rotationHistory.Count - 10).ToList();
                
                // Encrypt new value
                EncryptCredentialValue(newValue, out string encryptedValue, out string iv);
                
                // Update credential
                credential.EncryptedValue = encryptedValue;
                credential.IV = iv;
                credential.LastRotatedAt = DateTime.UtcNow;
                credential.ModifiedAt = DateTime.UtcNow;
                credential.RotationHistory = JsonSerializer.Serialize(rotationHistory, _jsonOptions);
                
                await _repository.UpdateAsync(credential);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Rotated credential: {Key}", key);
                return true;
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate credential: {Key}", key);
            throw CredentialException.Rotation(key, ex);
        }
    }

    /// <inheritdoc />
    public async Task<CredentialInfo?> GetCredentialInfoAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        try
        {
            var credential = await _credentialRepository.GetByKeyAsync(key);
            if (credential == null)
                return null;
                
            return new CredentialInfo
            {
                Key = credential.Key,
                CreatedAt = credential.CreatedAt,
                ModifiedAt = credential.ModifiedAt,
                Source = credential.Source,
                Group = credential.Group,
                ExpiresAt = credential.ExpiresAt,
                AccessCount = credential.AccessCount,
                LastAccessedAt = credential.LastAccessedAt,
                LastRotatedAt = credential.LastRotatedAt,
                IsEnabled = credential.IsEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credential info: {Key}", key);
            throw new CredentialException(
                $"Failed to retrieve credential info: {key}", 
                CredentialOperationType.Retrieval, 
                key, 
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<CredentialValidationResult> ValidateCredentialAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Credential key cannot be empty", nameof(key));
            
        try
        {
            var credential = await _credentialRepository.GetByKeyAsync(key);
            if (credential == null)
                return CredentialValidationResult.Failure($"Credential not found: {key}");
                
            var issues = new List<string>();
            
            // Check if enabled
            if (!credential.IsEnabled)
                issues.Add("Credential is disabled");
                
            // Check for expiration
            if (credential.ExpiresAt.HasValue && credential.ExpiresAt < DateTime.UtcNow)
                issues.Add($"Credential expired on {credential.ExpiresAt}");
                
            // Validate decryption
            try
            {
                DecryptCredentialValue(credential.EncryptedValue, credential.IV);
            }
            catch
            {
                issues.Add("Credential decryption failed");
            }
            
            if (issues.Count > 0)
                return CredentialValidationResult.Failure(issues.ToArray());
                
            return CredentialValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate credential: {Key}", key);
            throw CredentialException.Validation(key, ex.Message, ex);
        }
    }
    
    /// <summary>
    /// Encrypts a credential value
    /// </summary>
    /// <param name="value">Value to encrypt</param>
    /// <param name="encryptedValue">Resulting encrypted value</param>
    /// <param name="iv">Initialization vector used for encryption</param>
    private void EncryptCredentialValue(string value, out string encryptedValue, out string iv)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = DeriveKeyBytes(_encryptionKey);
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(value);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            // Store IV and encrypted bytes
            iv = Convert.ToBase64String(aes.IV);
            encryptedValue = Convert.ToBase64String(cipherBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw new CryptographicException("Failed to encrypt credential value", ex);
        }
    }
    
    /// <summary>
    /// Decrypts a credential value
    /// </summary>
    /// <param name="encryptedValue">Encrypted value</param>
    /// <param name="ivBase64">Base64-encoded initialization vector</param>
    /// <returns>Decrypted value</returns>
    private string DecryptCredentialValue(string encryptedValue, string ivBase64)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = DeriveKeyBytes(_encryptionKey);
            aes.IV = Convert.FromBase64String(ivBase64);
            
            using var decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(encryptedValue);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            
            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw new CryptographicException("Failed to decrypt credential value", ex);
        }
    }
    
    /// <summary>
    /// Derives a 256-bit key from the master key
    /// </summary>
    /// <param name="masterKey">Master encryption key</param>
    /// <returns>Derived key bytes</returns>
    private static byte[] DeriveKeyBytes(string masterKey)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(masterKey));
    }
}

/// <summary>
/// Entry for credential rotation history
/// </summary>
internal class CredentialRotationEntry
{
    /// <summary>
    /// When the rotation occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Reason for the rotation
    /// </summary>
    public string Reason { get; set; } = string.Empty;
} 