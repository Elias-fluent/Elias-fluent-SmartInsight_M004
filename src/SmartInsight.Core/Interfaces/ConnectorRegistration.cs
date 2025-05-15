using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents a connector registration in the registry
/// </summary>
public class ConnectorRegistration
{
    /// <summary>
    /// Unique identifier for this connector
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Display name for this connector
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Type of data source this connector handles
    /// </summary>
    public string SourceType { get; }
    
    /// <summary>
    /// Connector implementation type
    /// </summary>
    public Type ConnectorType { get; }
    
    /// <summary>
    /// Additional metadata about this connector
    /// </summary>
    public IDictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// Creates a new connector registration
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="name">Display name</param>
    /// <param name="sourceType">Data source type</param>
    /// <param name="connectorType">Connector implementation type</param>
    /// <param name="metadata">Additional metadata</param>
    public ConnectorRegistration(
        string id,
        string name,
        string sourceType,
        Type connectorType,
        IDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));
            
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("Source type cannot be empty", nameof(sourceType));
            
        if (connectorType == null)
            throw new ArgumentNullException(nameof(connectorType));
            
        if (!typeof(IDataSourceConnector).IsAssignableFrom(connectorType))
            throw new ArgumentException($"Type {connectorType.FullName} does not implement IDataSourceConnector", nameof(connectorType));
            
        Id = id;
        Name = name;
        SourceType = sourceType;
        ConnectorType = connectorType;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Gets the value of a metadata property
    /// </summary>
    /// <typeparam name="T">Expected property type</typeparam>
    /// <param name="key">Property key</param>
    /// <param name="defaultValue">Default value if property doesn't exist or is wrong type</param>
    /// <returns>Property value or default</returns>
    public T GetMetadataValue<T>(string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(key) || !Metadata.TryGetValue(key, out var value))
            return defaultValue;
            
        if (value is T typedValue)
            return typedValue;
            
        try
        {
            // Try to convert the value
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Creates a new instance of this connector
    /// </summary>
    /// <returns>Connector instance</returns>
    public IDataSourceConnector CreateInstance()
    {
        return (IDataSourceConnector)Activator.CreateInstance(ConnectorType)!;
    }
    
    /// <summary>
    /// Creates a new instance of this connector with constructor parameters
    /// </summary>
    /// <param name="parameters">Constructor parameters</param>
    /// <returns>Connector instance</returns>
    public IDataSourceConnector CreateInstance(params object[] parameters)
    {
        return (IDataSourceConnector)Activator.CreateInstance(ConnectorType, parameters)!;
    }
} 