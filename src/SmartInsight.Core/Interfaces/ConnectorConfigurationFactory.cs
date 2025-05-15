using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Factory for creating connector configurations
/// </summary>
public static class ConnectorConfigurationFactory
{
    /// <summary>
    /// Creates a new connector configuration
    /// </summary>
    /// <param name="connectorId">Connector ID this configuration belongs to</param>
    /// <param name="name">Name of this configuration</param>
    /// <param name="tenantId">Tenant ID this configuration belongs to</param>
    /// <param name="connectionParameters">Connection parameters</param>
    /// <param name="credentialStore">Secure credential store (optional - creates a new MemoryCredentialStore if not provided)</param>
    /// <param name="settings">Additional settings</param>
    /// <param name="createdBy">Who created this configuration</param>
    /// <param name="isEnabled">Whether this configuration is enabled</param>
    /// <returns>New connector configuration</returns>
    public static IConnectorConfiguration Create(
        string connectorId,
        string name,
        Guid tenantId,
        IDictionary<string, string>? connectionParameters = null,
        ISecureCredentialStore? credentialStore = null,
        IDictionary<string, object>? settings = null,
        string createdBy = "system",
        bool isEnabled = true)
    {
        return new ConnectorConfiguration(
            connectorId,
            name,
            tenantId,
            credentialStore ?? new MemoryCredentialStore(),
            connectionParameters,
            settings,
            createdBy,
            isEnabled);
    }
    
    /// <summary>
    /// Creates a configuration with credentials extracted from connection parameters
    /// </summary>
    /// <param name="connectorId">Connector ID this configuration belongs to</param>
    /// <param name="name">Name of this configuration</param>
    /// <param name="tenantId">Tenant ID this configuration belongs to</param>
    /// <param name="mixedParameters">Connection parameters including sensitive credentials</param>
    /// <param name="settings">Additional settings</param>
    /// <param name="createdBy">Who created this configuration</param>
    /// <param name="isEnabled">Whether this configuration is enabled</param>
    /// <returns>New connector configuration with sensitive parameters moved to credential store</returns>
    public static IConnectorConfiguration CreateWithExtractedCredentials(
        string connectorId,
        string name,
        Guid tenantId,
        IDictionary<string, string> mixedParameters,
        IDictionary<string, object>? settings = null,
        string createdBy = "system",
        bool isEnabled = true)
    {
        // Create a new credential store
        var credentialStore = new MemoryCredentialStore();
        
        // Safe parameters to keep in the connection parameters
        var safeParameters = new Dictionary<string, string>();
        
        foreach (var param in mixedParameters)
        {
            if (IsSensitiveParameter(param.Key))
            {
                // Move sensitive parameters to credential store
                credentialStore.SetCredential(param.Key, param.Value);
            }
            else
            {
                // Keep non-sensitive parameters in connection parameters
                safeParameters[param.Key] = param.Value;
            }
        }
        
        return new ConnectorConfiguration(
            connectorId,
            name,
            tenantId,
            credentialStore,
            safeParameters,
            settings,
            createdBy,
            isEnabled);
    }
    
    /// <summary>
    /// Determines if a parameter name is considered sensitive
    /// </summary>
    /// <param name="paramName">Parameter name</param>
    /// <returns>True if sensitive, false otherwise</returns>
    private static bool IsSensitiveParameter(string paramName)
    {
        if (string.IsNullOrWhiteSpace(paramName))
            return false;
            
        var normalizedName = paramName.ToLowerInvariant();
        
        return normalizedName.Contains("password") ||
               normalizedName.Contains("secret") ||
               normalizedName.Contains("key") ||
               normalizedName.Contains("token") ||
               normalizedName.Contains("credential");
    }
} 