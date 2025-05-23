using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Standard implementation of IConnectorConfiguration
/// </summary>
public class ConnectorConfiguration : IConnectorConfiguration
{
    /// <summary>
    /// Unique identifier for this configuration
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// Name of this configuration
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Connector ID this configuration belongs to
    /// </summary>
    public string ConnectorId { get; }
    
    /// <summary>
    /// Tenant ID this configuration belongs to
    /// </summary>
    public Guid TenantId { get; }
    
    /// <summary>
    /// Connection parameters
    /// </summary>
    public IDictionary<string, string> ConnectionParameters { get; }
    
    /// <summary>
    /// Secure credentials
    /// </summary>
    [JsonIgnore]
    public ISecureCredentialStore Credentials { get; }
    
    /// <summary>
    /// Additional settings for the connector
    /// </summary>
    public IDictionary<string, object> Settings { get; }
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; }
    
    /// <summary>
    /// When this configuration was last modified
    /// </summary>
    public DateTime ModifiedAt { get; private set; }
    
    /// <summary>
    /// Who created this configuration
    /// </summary>
    public string CreatedBy { get; }
    
    /// <summary>
    /// Who last modified this configuration
    /// </summary>
    public string ModifiedBy { get; private set; }
    
    /// <summary>
    /// Whether this configuration is enabled
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Creates a new connector configuration
    /// </summary>
    /// <param name="connectorId">Connector ID this configuration belongs to</param>
    /// <param name="name">Name of this configuration</param>
    /// <param name="tenantId">Tenant ID this configuration belongs to</param>
    /// <param name="credentials">Secure credential store</param>
    /// <param name="connectionParameters">Connection parameters</param>
    /// <param name="settings">Additional settings</param>
    /// <param name="createdBy">Who created this configuration</param>
    /// <param name="isEnabled">Whether this configuration is enabled</param>
    public ConnectorConfiguration(
        string connectorId,
        string name,
        Guid tenantId,
        ISecureCredentialStore credentials,
        IDictionary<string, string>? connectionParameters = null,
        IDictionary<string, object>? settings = null,
        string createdBy = "system",
        bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connectorId));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        
        Id = Guid.NewGuid();
        ConnectorId = connectorId;
        Name = name;
        TenantId = tenantId;
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        ConnectionParameters = connectionParameters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
        Settings = settings?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
        ModifiedAt = CreatedAt;
        CreatedBy = createdBy;
        ModifiedBy = createdBy;
        IsEnabled = isEnabled;
    }
    
    /// <summary>
    /// Creates a new connector configuration from existing data
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="connectorId">Connector ID this configuration belongs to</param>
    /// <param name="name">Name of this configuration</param>
    /// <param name="tenantId">Tenant ID this configuration belongs to</param>
    /// <param name="credentials">Secure credential store</param>
    /// <param name="connectionParameters">Connection parameters</param>
    /// <param name="settings">Additional settings</param>
    /// <param name="createdAt">When this configuration was created</param>
    /// <param name="modifiedAt">When this configuration was last modified</param>
    /// <param name="createdBy">Who created this configuration</param>
    /// <param name="modifiedBy">Who last modified this configuration</param>
    /// <param name="isEnabled">Whether this configuration is enabled</param>
    [JsonConstructor]
    public ConnectorConfiguration(
        Guid id,
        string connectorId,
        string name,
        Guid tenantId,
        ISecureCredentialStore credentials,
        IDictionary<string, string> connectionParameters,
        IDictionary<string, object> settings,
        DateTime createdAt,
        DateTime modifiedAt,
        string createdBy,
        string modifiedBy,
        bool isEnabled)
    {
        Id = id;
        ConnectorId = connectorId;
        Name = name;
        TenantId = tenantId;
        Credentials = credentials;
        ConnectionParameters = connectionParameters;
        Settings = settings;
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
        CreatedBy = createdBy;
        ModifiedBy = modifiedBy;
        IsEnabled = isEnabled;
    }
    
    /// <summary>
    /// Gets a configuration value by key
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>Configuration value or default</returns>
    public T GetValue<T>(string key, T defaultValue = default)
    {
        // First look in connection parameters for string values
        if (typeof(T) == typeof(string) && ConnectionParameters.TryGetValue(key, out var strValue))
        {
            return (T)(object)strValue;
        }
        
        // Then look in settings for other types
        if (Settings.TryGetValue(key, out var objValue))
        {
            if (objValue is T typedValue)
            {
                return typedValue;
            }
            
            try
            {
                // Try to convert using JSON serialization
                var json = JsonSerializer.Serialize(objValue);
                var result = JsonSerializer.Deserialize<T>(json);
                return result ?? defaultValue;
            }
            catch
            {
                // If conversion fails, return default
                return defaultValue;
            }
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Gets all configuration values
    /// </summary>
    /// <returns>Dictionary of configuration values</returns>
    public IDictionary<string, object> GetAll()
    {
        var result = new Dictionary<string, object>(Settings);
        
        // Add connection parameters as strings
        foreach (var param in ConnectionParameters)
        {
            result[param.Key] = param.Value;
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>True if the key exists, otherwise false</returns>
    public bool HasValue(string key)
    {
        return ConnectionParameters.ContainsKey(key) || Settings.ContainsKey(key);
    }
    
    /// <summary>
    /// Gets connection parameters for the connector
    /// </summary>
    /// <returns>Dictionary of connection parameters</returns>
    public IDictionary<string, string> GetConnectionParameters()
    {
        return new Dictionary<string, string>(ConnectionParameters);
    }
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>Result indicating if configuration is valid</returns>
    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();
        var warnings = new List<string>();
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError(nameof(Name), "Name cannot be empty"));
        
        if (string.IsNullOrWhiteSpace(ConnectorId))
            errors.Add(new ValidationError(nameof(ConnectorId), "Connector ID cannot be empty"));
        
        if (TenantId == Guid.Empty)
            errors.Add(new ValidationError(nameof(TenantId), "Tenant ID cannot be empty"));
        
        // Add warnings if there are no connection parameters
        if (ConnectionParameters.Count == 0)
            warnings.Add("No connection parameters defined");
        
        return errors.Count > 0 
            ? ValidationResult.Failure(errors, warnings) 
            : ValidationResult.Success(warnings);
    }
    
    /// <summary>
    /// Updates the connection parameter value
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <param name="value">Parameter value</param>
    public void SetConnectionParameter(string key, string value)
    {
        ConnectionParameters[key] = value;
        UpdateModified();
    }
    
    /// <summary>
    /// Updates the setting value
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="value">Setting value</param>
    public void SetSetting(string key, object value)
    {
        Settings[key] = value;
        UpdateModified();
    }
    
    /// <summary>
    /// Gets a serializable version of this configuration (with credentials redacted)
    /// </summary>
    /// <returns>Serializable configuration</returns>
    public IDictionary<string, object> ToSerializable()
    {
        var result = new Dictionary<string, object>
        {
            { "id", Id },
            { "name", Name },
            { "connectorId", ConnectorId },
            { "tenantId", TenantId },
            { "connectionParameters", GetRedactedConnectionParameters() },
            { "settings", Settings },
            { "createdAt", CreatedAt },
            { "modifiedAt", ModifiedAt },
            { "createdBy", CreatedBy },
            { "modifiedBy", ModifiedBy },
            { "isEnabled", IsEnabled },
            { "credentialKeys", Credentials.GetCredentialKeys().ToList() }
        };
        
        return result;
    }
    
    /// <summary>
    /// Updates the modified timestamp and user
    /// </summary>
    /// <param name="modifiedBy">User who made the modification</param>
    private void UpdateModified(string? modifiedBy = null)
    {
        ModifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(modifiedBy))
            ModifiedBy = modifiedBy;
    }
    
    /// <summary>
    /// Gets a redacted copy of the connection parameters (sensitive values hidden)
    /// </summary>
    /// <returns>Dictionary with redacted values</returns>
    private IDictionary<string, string> GetRedactedConnectionParameters()
    {
        var result = new Dictionary<string, string>();
        
        foreach (var param in ConnectionParameters)
        {
            result[param.Key] = IsSensitiveParameter(param.Key) ? "********" : param.Value;
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if a parameter name indicates a sensitive value
    /// </summary>
    /// <param name="paramName">Parameter name</param>
    /// <returns>True if sensitive, otherwise false</returns>
    private static bool IsSensitiveParameter(string paramName)
    {
        var sensitiveKeywords = new[] 
        { 
            "password", "secret", "key", "token", "credential", "auth", 
            "apikey", "api_key", "accesskey", "access_key" 
        };
        
        return sensitiveKeywords.Any(keyword => 
            paramName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
} 