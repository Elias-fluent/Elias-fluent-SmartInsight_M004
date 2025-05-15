namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for connector configuration
/// </summary>
public interface IConnectorConfiguration
{
    /// <summary>
    /// Unique identifier for this configuration
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Name of this configuration
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Connector ID this configuration belongs to
    /// </summary>
    string ConnectorId { get; }
    
    /// <summary>
    /// Tenant ID this configuration belongs to
    /// </summary>
    Guid TenantId { get; }
    
    /// <summary>
    /// Connection parameters
    /// </summary>
    IDictionary<string, string> ConnectionParameters { get; }
    
    /// <summary>
    /// Secure credentials
    /// </summary>
    ISecureCredentialStore Credentials { get; }
    
    /// <summary>
    /// Additional settings for the connector
    /// </summary>
    IDictionary<string, object> Settings { get; }
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// When this configuration was last modified
    /// </summary>
    DateTime ModifiedAt { get; }
    
    /// <summary>
    /// Who created this configuration
    /// </summary>
    string CreatedBy { get; }
    
    /// <summary>
    /// Who last modified this configuration
    /// </summary>
    string ModifiedBy { get; }
    
    /// <summary>
    /// Whether this configuration is enabled
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>Result indicating if configuration is valid</returns>
    ValidationResult Validate();
    
    /// <summary>
    /// Updates the connection parameter value
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <param name="value">Parameter value</param>
    void SetConnectionParameter(string key, string value);
    
    /// <summary>
    /// Updates the setting value
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="value">Setting value</param>
    void SetSetting(string key, object value);
    
    /// <summary>
    /// Gets a serializable version of this configuration (with credentials redacted)
    /// </summary>
    /// <returns>Serializable configuration</returns>
    IDictionary<string, object> ToSerializable();
} 