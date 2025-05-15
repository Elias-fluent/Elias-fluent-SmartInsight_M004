using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Serializer for connector configurations
/// </summary>
public static class ConnectorConfigurationSerializer
{
    // Default JSON serialization options
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    
    /// <summary>
    /// Serializes a connector configuration to JSON
    /// </summary>
    /// <param name="configuration">Configuration to serialize</param>
    /// <param name="includeCredentials">Whether to include credential keys (not values) in output</param>
    /// <param name="options">JSON serialization options</param>
    /// <returns>JSON string representation of the configuration</returns>
    public static string ToJson(
        IConnectorConfiguration configuration,
        bool includeCredentials = false,
        JsonSerializerOptions? options = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        
        // Convert to serializable form
        var serializableObj = configuration.ToSerializable();
        
        // Optionally remove credential keys
        if (!includeCredentials && serializableObj.TryGetValue("credentialKeys", out var _))
        {
            serializableObj.Remove("credentialKeys");
        }
        
        return JsonSerializer.Serialize(serializableObj, options ?? _serializerOptions);
    }
    
    /// <summary>
    /// Writes a connector configuration to a JSON file
    /// </summary>
    /// <param name="configuration">Configuration to serialize</param>
    /// <param name="filePath">File path to write to</param>
    /// <param name="includeCredentials">Whether to include credential keys (not values) in output</param>
    /// <param name="options">JSON serialization options</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task WriteToFileAsync(
        IConnectorConfiguration configuration,
        string filePath,
        bool includeCredentials = false,
        JsonSerializerOptions? options = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        
        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Write to file
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var serializableObj = configuration.ToSerializable();
        
        // Optionally remove credential keys
        if (!includeCredentials && serializableObj.TryGetValue("credentialKeys", out var _))
        {
            serializableObj.Remove("credentialKeys");
        }
        
        await JsonSerializer.SerializeAsync(fileStream, serializableObj, options ?? _serializerOptions);
    }
    
    /// <summary>
    /// Creates a configuration from JSON data
    /// </summary>
    /// <param name="json">JSON data</param>
    /// <param name="credentialStore">Credential store to use</param>
    /// <param name="options">JSON deserialization options</param>
    /// <returns>Deserialized configuration</returns>
    public static IConnectorConfiguration FromJson(
        string json,
        ISecureCredentialStore credentialStore,
        JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON data cannot be empty", nameof(json));
        
        if (credentialStore == null)
            throw new ArgumentNullException(nameof(credentialStore));
        
        // Deserialize the data
        var configData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options ?? _serializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize configuration data");
        
        // Extract required fields
        var id = GetGuid(configData, "id");
        var name = GetString(configData, "name");
        var connectorId = GetString(configData, "connectorId");
        var tenantId = GetGuid(configData, "tenantId");
        var createdAt = GetDateTime(configData, "createdAt");
        var modifiedAt = GetDateTime(configData, "modifiedAt", createdAt);
        var createdBy = GetString(configData, "createdBy", "system");
        var modifiedBy = GetString(configData, "modifiedBy", createdBy);
        var isEnabled = GetBool(configData, "isEnabled", true);
        
        // Extract connection parameters
        var connectionParameters = new Dictionary<string, string>();
        if (configData.TryGetValue("connectionParameters", out var connParamsElement) && 
            connParamsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in connParamsElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    connectionParameters[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }
        
        // Extract settings
        var settings = new Dictionary<string, object>();
        if (configData.TryGetValue("settings", out var settingsElement) && 
            settingsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in settingsElement.EnumerateObject())
            {
                settings[prop.Name] = ConvertJsonElementToObject(prop.Value);
            }
        }
        
        // Create configuration
        return new ConnectorConfiguration(
            id,
            connectorId,
            name,
            tenantId,
            credentialStore,
            connectionParameters,
            settings,
            createdAt,
            modifiedAt,
            createdBy,
            modifiedBy,
            isEnabled);
    }
    
    /// <summary>
    /// Reads a configuration from a JSON file
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <param name="credentialStore">Credential store to use</param>
    /// <param name="options">JSON deserialization options</param>
    /// <returns>Deserialized configuration</returns>
    public static async Task<IConnectorConfiguration> ReadFromFileAsync(
        string filePath,
        ISecureCredentialStore credentialStore,
        JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Configuration file not found", filePath);
        
        if (credentialStore == null)
            throw new ArgumentNullException(nameof(credentialStore));
        
        // Read JSON from file
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var configData = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
            fileStream, options ?? _serializerOptions);
        
        if (configData == null)
            throw new InvalidOperationException("Failed to deserialize configuration file");
        
        // Extract required fields
        var id = GetGuid(configData, "id");
        var name = GetString(configData, "name");
        var connectorId = GetString(configData, "connectorId");
        var tenantId = GetGuid(configData, "tenantId");
        var createdAt = GetDateTime(configData, "createdAt");
        var modifiedAt = GetDateTime(configData, "modifiedAt", createdAt);
        var createdBy = GetString(configData, "createdBy", "system");
        var modifiedBy = GetString(configData, "modifiedBy", createdBy);
        var isEnabled = GetBool(configData, "isEnabled", true);
        
        // Extract connection parameters
        var connectionParameters = new Dictionary<string, string>();
        if (configData.TryGetValue("connectionParameters", out var connParamsElement) && 
            connParamsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in connParamsElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    connectionParameters[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }
        
        // Extract settings
        var settings = new Dictionary<string, object>();
        if (configData.TryGetValue("settings", out var settingsElement) && 
            settingsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in settingsElement.EnumerateObject())
            {
                settings[prop.Name] = ConvertJsonElementToObject(prop.Value);
            }
        }
        
        // Create configuration
        return new ConnectorConfiguration(
            id,
            connectorId,
            name,
            tenantId,
            credentialStore,
            connectionParameters,
            settings,
            createdAt,
            modifiedAt,
            createdBy,
            modifiedBy,
            isEnabled);
    }
    
    // Helper methods to extract values from JSON data
    private static Guid GetGuid(Dictionary<string, JsonElement> data, string key, Guid defaultValue = default)
    {
        if (data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(element.GetString(), out var guidValue))
                return guidValue;
        }
        return defaultValue;
    }
    
    private static string GetString(Dictionary<string, JsonElement> data, string key, string defaultValue = "")
    {
        if (data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? defaultValue;
        }
        return defaultValue;
    }
    
    private static DateTime GetDateTime(Dictionary<string, JsonElement> data, string key, DateTime? defaultValue = null)
    {
        if (data.TryGetValue(key, out var element))
        {
            if (element.ValueKind == JsonValueKind.String && 
                DateTime.TryParse(element.GetString(), out var dateValue))
                return dateValue;
                
            if (element.TryGetDateTime(out var dateTimeValue))
                return dateTimeValue;
        }
        return defaultValue ?? DateTime.UtcNow;
    }
    
    private static bool GetBool(Dictionary<string, JsonElement> data, string key, bool defaultValue = false)
    {
        if (data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.True || 
            element.ValueKind == JsonValueKind.False)
        {
            return element.GetBoolean();
        }
        return defaultValue;
    }
    
    private static object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElementToObject(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(e => ConvertJsonElementToObject(e))
                .ToList(),
            _ => element.ToString() ?? string.Empty
        };
    }
} 