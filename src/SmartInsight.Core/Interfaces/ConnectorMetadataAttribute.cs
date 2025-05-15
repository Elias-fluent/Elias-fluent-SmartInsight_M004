using System;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Provides metadata information for connector types
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ConnectorMetadataAttribute : Attribute
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
    /// Description of the connector
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Version of the connector
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Author information
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// Connector documentation URL
    /// </summary>
    public string? DocumentationUrl { get; set; }
    
    /// <summary>
    /// Connector capabilities
    /// </summary>
    public string[]? Capabilities { get; set; }
    
    /// <summary>
    /// Connection schema (field definitions and validation rules)
    /// </summary>
    public string? ConnectionSchema { get; set; }
    
    /// <summary>
    /// Creates a new connector metadata attribute
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="name">Display name</param>
    /// <param name="sourceType">Data source type</param>
    public ConnectorMetadataAttribute(string id, string name, string sourceType)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));
            
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("Source type cannot be empty", nameof(sourceType));
            
        Id = id;
        Name = name;
        SourceType = sourceType;
    }
} 