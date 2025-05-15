using Microsoft.Extensions.Configuration;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Security;
using System;
using System.Collections.Concurrent;
using System.Data.Common;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Manages connection strings for the application with support for tenant-specific connections
/// </summary>
public class ConnectionStringManager : IConnectionStringManager
{
    private readonly IConfiguration _configuration;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly ConcurrentDictionary<string, string> _connectionStringCache;
    private readonly string _encryptionKey;

    /// <summary>
    /// Initializes a new connection string manager
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="tenantAccessor">Optional tenant accessor for multi-tenant scenarios</param>
    public ConnectionStringManager(
        IConfiguration configuration,
        ITenantAccessor? tenantAccessor = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tenantAccessor = tenantAccessor;
        _connectionStringCache = new ConcurrentDictionary<string, string>();
        
        // Get the encryption key from configuration
        _encryptionKey = _configuration["ConnectionStrings:EncryptionKey"] 
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_ENCRYPTION_KEY") 
            ?? "SmartInsight_Default_Key";
    }

    /// <summary>
    /// Gets a connection string for the specified name, decrypting if necessary
    /// </summary>
    /// <param name="name">Name of the connection string</param>
    /// <param name="forTenant">Whether to get a tenant-specific connection if available</param>
    /// <param name="decrypt">Whether to decrypt encrypted connection strings</param>
    /// <returns>The connection string</returns>
    public string GetConnectionString(string name, bool forTenant = true, bool decrypt = true)
    {
        // Try to get from cache first
        string cacheKey = GetCacheKey(name, forTenant);
        if (_connectionStringCache.TryGetValue(cacheKey, out string? cachedConnection) && !string.IsNullOrEmpty(cachedConnection))
        {
            return cachedConnection;
        }

        // Check for tenant-specific connection string if needed
        string connectionStringName = name;
        if (forTenant && _tenantAccessor != null && 
            _tenantAccessor.IsMultiTenantContext() &&
            _tenantAccessor.GetCurrentTenantId().HasValue)
        {
            var tenantId = _tenantAccessor.GetCurrentTenantId()!.Value;
            var tenantConnectionString = _configuration.GetConnectionString($"Tenant_{tenantId}_{name}");
            
            if (!string.IsNullOrEmpty(tenantConnectionString))
            {
                connectionStringName = $"Tenant_{tenantId}_{name}";
            }
            else
            {
                // Use tenant name prefix convention
                var tenantConnectionName = $"Tenant_{tenantId}";
                tenantConnectionString = _configuration.GetConnectionString(tenantConnectionName);
                
                if (!string.IsNullOrEmpty(tenantConnectionString))
                {
                    connectionStringName = tenantConnectionName;
                }
            }
        }
        
        // Get the connection string from configuration
        var connectionString = _configuration.GetConnectionString(connectionStringName);
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Check if we have a default connection as fallback
            if (name != "DefaultConnection")
            {
                connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException($"Connection string '{connectionStringName}' not found, and no default connection is available.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");
            }
        }

        // Decrypt if needed
        if (decrypt && IsEncrypted(connectionString))
        {
            try
            {
                connectionString = EncryptionUtility.Decrypt(connectionString, _encryptionKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to decrypt connection string '{connectionStringName}'", ex);
            }
        }
        
        // Cache the connection string
        _connectionStringCache.TryAdd(cacheKey, connectionString);
        
        return connectionString;
    }
    
    /// <summary>
    /// Builds a full connection string with additional parameters like Application Name
    /// </summary>
    /// <param name="baseConnectionString">Base connection string</param>
    /// <param name="additionalProperties">Additional connection properties</param>
    /// <returns>Complete connection string</returns>
    public string BuildConnectionString(string baseConnectionString, IDictionary<string, string>? additionalProperties = null)
    {
        if (string.IsNullOrEmpty(baseConnectionString))
        {
            throw new ArgumentException("Base connection string cannot be empty", nameof(baseConnectionString));
        }
        
        // If no additional properties, return base connection string
        if (additionalProperties == null || additionalProperties.Count == 0)
        {
            return baseConnectionString;
        }
        
        // Parse the connection string
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = baseConnectionString
        };
        
        // Add or update properties
        foreach (var property in additionalProperties)
        {
            builder[property.Key] = property.Value;
        }
        
        return builder.ConnectionString;
    }
    
    /// <summary>
    /// Gets a connection string with tenant-specific application name
    /// </summary>
    /// <param name="name">Name of the connection string</param>
    /// <param name="applicationNamePrefix">Prefix for the application name</param>
    /// <returns>Connection string with tenant-specific application name</returns>
    public string GetConnectionStringWithApplicationName(string name, string applicationNamePrefix)
    {
        var baseConnectionString = GetConnectionString(name);
        var additionalProperties = new Dictionary<string, string>();
        
        // Add tenant-specific application name if applicable
        if (_tenantAccessor != null && 
            _tenantAccessor.IsMultiTenantContext() &&
            _tenantAccessor.GetCurrentTenantId().HasValue)
        {
            var tenantId = _tenantAccessor.GetCurrentTenantId()!.Value.ToString();
            additionalProperties["Application Name"] = $"{applicationNamePrefix}_Tenant_{tenantId}";
        }
        else
        {
            additionalProperties["Application Name"] = $"{applicationNamePrefix}_Admin";
        }
        
        // Add connection pooling settings
        additionalProperties["Pooling"] = "true";
        additionalProperties["Minimum Pool Size"] = "1";
        additionalProperties["Maximum Pool Size"] = "100";
        
        return BuildConnectionString(baseConnectionString, additionalProperties);
    }
    
    /// <summary>
    /// Encrypts a connection string
    /// </summary>
    /// <param name="connectionString">Connection string to encrypt</param>
    /// <returns>Encrypted connection string</returns>
    public string EncryptConnectionString(string connectionString)
    {
        return EncryptionUtility.Encrypt(connectionString, _encryptionKey);
    }
    
    /// <summary>
    /// Checks if a connection string is encrypted
    /// </summary>
    /// <param name="connectionString">Connection string to check</param>
    /// <returns>True if the connection string is encrypted</returns>
    private bool IsEncrypted(string connectionString)
    {
        // Basic heuristic: encrypted strings are Base64 encoded and don't contain typical connection string delimiters
        return !connectionString.Contains('=') && IsBase64String(connectionString);
    }
    
    /// <summary>
    /// Checks if a string is Base64 encoded
    /// </summary>
    /// <param name="value">String to check</param>
    /// <returns>True if the string is Base64 encoded</returns>
    private bool IsBase64String(string value)
    {
        try
        {
            var data = Convert.FromBase64String(value);
            return data.Length > 0;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets a cache key for a connection string
    /// </summary>
    /// <param name="name">Connection string name</param>
    /// <param name="forTenant">Whether it's for a tenant</param>
    /// <returns>Cache key</returns>
    private string GetCacheKey(string name, bool forTenant)
    {
        if (!forTenant || _tenantAccessor == null || !_tenantAccessor.IsMultiTenantContext())
        {
            return $"Global_{name}";
        }
        
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        return tenantId.HasValue ? $"Tenant_{tenantId}_{name}" : $"Global_{name}";
    }
} 