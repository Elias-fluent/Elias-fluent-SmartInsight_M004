namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for secure credential management
/// </summary>
public interface ICredentialManager
{
    /// <summary>
    /// Stores a credential securely
    /// </summary>
    /// <param name="key">Unique key to identify the credential</param>
    /// <param name="value">Value to store</param>
    /// <param name="source">Optional source identifier (e.g., connector ID)</param>
    /// <param name="group">Optional grouping for organization</param>
    /// <param name="metadata">Optional metadata in JSON format</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <returns>Task representing the async operation</returns>
    Task StoreCredentialAsync(
        string key, 
        string value, 
        string? source = null, 
        string? group = null, 
        string? metadata = null, 
        DateTime? expiresAt = null);
    
    /// <summary>
    /// Retrieves a credential
    /// </summary>
    /// <param name="key">Unique key of the credential</param>
    /// <returns>The credential value or null if not found</returns>
    Task<string?> GetCredentialAsync(string key);
    
    /// <summary>
    /// Checks if a credential exists
    /// </summary>
    /// <param name="key">Unique key of the credential</param>
    /// <returns>True if the credential exists and is valid</returns>
    Task<bool> HasCredentialAsync(string key);
    
    /// <summary>
    /// Deletes a credential
    /// </summary>
    /// <param name="key">Unique key of the credential</param>
    /// <returns>True if the credential was deleted, false if not found</returns>
    Task<bool> DeleteCredentialAsync(string key);
    
    /// <summary>
    /// Gets all credential keys (without values) matching optional filters
    /// </summary>
    /// <param name="source">Optional source filter</param>
    /// <param name="group">Optional group filter</param>
    /// <returns>Collection of credential keys</returns>
    Task<IEnumerable<string>> GetCredentialKeysAsync(string? source = null, string? group = null);
    
    /// <summary>
    /// Rotates a credential (updates its value) with audit trail
    /// </summary>
    /// <param name="key">Unique key of the credential</param>
    /// <param name="newValue">New value for the credential</param>
    /// <param name="reason">Optional reason for rotation</param>
    /// <returns>True if rotation was successful, false if credential not found</returns>
    Task<bool> RotateCredentialAsync(string key, string newValue, string? reason = null);
    
    /// <summary>
    /// Gets information about a credential without exposing the actual value
    /// </summary>
    /// <param name="key">Unique key of the credential</param>
    /// <returns>Credential metadata or null if not found</returns>
    Task<CredentialInfo?> GetCredentialInfoAsync(string key);
    
    /// <summary>
    /// Validates the credential (checks if it exists and hasn't expired)
    /// </summary>
    /// <param name="key">Unique key of the credential</param>
    /// <returns>Validation result with status and any issues</returns>
    Task<CredentialValidationResult> ValidateCredentialAsync(string key);
}

/// <summary>
/// Class representing credential information without the actual value
/// </summary>
public class CredentialInfo
{
    /// <summary>
    /// Unique key for the credential within a tenant
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// When the credential was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the credential was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; }
    
    /// <summary>
    /// Optional source of this credential
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Optional group this credential belongs to
    /// </summary>
    public string? Group { get; set; }
    
    /// <summary>
    /// Expiration date of this credential
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Number of times this credential has been accessed
    /// </summary>
    public int AccessCount { get; set; }
    
    /// <summary>
    /// Last time this credential was accessed
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>
    /// Last time this credential was rotated
    /// </summary>
    public DateTime? LastRotatedAt { get; set; }
    
    /// <summary>
    /// Whether this credential is enabled
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Whether this credential has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}

/// <summary>
/// Credential validation result
/// </summary>
public class CredentialValidationResult
{
    /// <summary>
    /// Whether the credential is valid
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Collection of validation issues, if any
    /// </summary>
    public List<string> Issues { get; set; } = new List<string>();
    
    /// <summary>
    /// Creates a success validation result
    /// </summary>
    /// <returns>Success validation result</returns>
    public static CredentialValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// Creates a failure validation result with issues
    /// </summary>
    /// <param name="issues">Collection of validation issues</param>
    /// <returns>Failure validation result</returns>
    public static CredentialValidationResult Failure(params string[] issues) => new() 
    { 
        IsValid = false, 
        Issues = issues.ToList() 
    };
} 