using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents information about a data structure in a data source
/// </summary>
public class DataStructureInfo
{
    /// <summary>
    /// Name of the data structure
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Schema or namespace the data structure belongs to
    /// </summary>
    public string? Schema { get; }
    
    /// <summary>
    /// Type of the data structure (e.g., table, view, file, collection)
    /// </summary>
    public string Type { get; }
    
    /// <summary>
    /// Description or comments about the data structure
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Fields or columns in the data structure
    /// </summary>
    public IEnumerable<FieldInfo> Fields { get; }
    
    /// <summary>
    /// Additional metadata about the data structure
    /// </summary>
    public IDictionary<string, object>? Metadata { get; }
    
    /// <summary>
    /// Approximate number of records in the data structure
    /// </summary>
    public long? RecordCount { get; }
    
    /// <summary>
    /// Full path or identifier to access the data structure
    /// </summary>
    public string FullPath { get; }
    
    /// <summary>
    /// Parent structure if this is a nested structure
    /// </summary>
    public string? ParentName { get; }
    
    /// <summary>
    /// Creates a new data structure info
    /// </summary>
    /// <param name="name">Name of the data structure</param>
    /// <param name="type">Type of the data structure</param>
    /// <param name="fields">Fields or columns in the data structure</param>
    /// <param name="schema">Schema or namespace the data structure belongs to</param>
    /// <param name="description">Description or comments about the data structure</param>
    /// <param name="metadata">Additional metadata about the data structure</param>
    /// <param name="recordCount">Approximate number of records in the data structure</param>
    /// <param name="fullPath">Full path or identifier to access the data structure</param>
    /// <param name="parentName">Parent structure name</param>
    public DataStructureInfo(
        string name, 
        string type, 
        IEnumerable<FieldInfo> fields,
        string? schema = null,
        string? description = null,
        IDictionary<string, object>? metadata = null,
        long? recordCount = null,
        string? fullPath = null,
        string? parentName = null)
    {
        Name = name;
        Type = type;
        Schema = schema;
        Description = description;
        Fields = fields;
        Metadata = metadata;
        RecordCount = recordCount;
        FullPath = fullPath ?? (Schema != null ? $"{Schema}.{Name}" : Name);
        ParentName = parentName;
    }
}

/// <summary>
/// Represents information about a field or column in a data structure
/// </summary>
public class FieldInfo
{
    /// <summary>
    /// Name of the field
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Data type of the field
    /// </summary>
    public string DataType { get; }
    
    /// <summary>
    /// Description or comments about the field
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Whether the field is nullable
    /// </summary>
    public bool IsNullable { get; }
    
    /// <summary>
    /// Whether the field is part of the primary key
    /// </summary>
    public bool IsPrimaryKey { get; }
    
    /// <summary>
    /// Maximum length of the field (for string types)
    /// </summary>
    public int? MaxLength { get; }
    
    /// <summary>
    /// Precision of the field (for numeric types)
    /// </summary>
    public int? Precision { get; }
    
    /// <summary>
    /// Scale of the field (for numeric types)
    /// </summary>
    public int? Scale { get; }
    
    /// <summary>
    /// Default value of the field
    /// </summary>
    public object? DefaultValue { get; }
    
    /// <summary>
    /// Additional metadata about the field
    /// </summary>
    public IDictionary<string, object>? Metadata { get; }
    
    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool IsRequired { get; }
    
    /// <summary>
    /// Additional field properties
    /// </summary>
    public IDictionary<string, object>? Properties { get; }
    
    /// <summary>
    /// Creates a new field info
    /// </summary>
    /// <param name="name">Name of the field</param>
    /// <param name="dataType">Data type of the field</param>
    /// <param name="isNullable">Whether the field is nullable</param>
    /// <param name="description">Description or comments about the field</param>
    /// <param name="isPrimaryKey">Whether the field is part of the primary key</param>
    /// <param name="maxLength">Maximum length of the field (for string types)</param>
    /// <param name="precision">Precision of the field (for numeric types)</param>
    /// <param name="scale">Scale of the field (for numeric types)</param>
    /// <param name="defaultValue">Default value of the field</param>
    /// <param name="metadata">Additional metadata about the field</param>
    /// <param name="isRequired">Whether the field is required</param>
    /// <param name="properties">Additional field properties</param>
    public FieldInfo(
        string name, 
        string dataType, 
        bool isNullable,
        string? description = null,
        bool isPrimaryKey = false,
        int? maxLength = null,
        int? precision = null,
        int? scale = null,
        object? defaultValue = null,
        IDictionary<string, object>? metadata = null,
        bool isRequired = false,
        IDictionary<string, object>? properties = null)
    {
        Name = name;
        DataType = dataType;
        IsNullable = isNullable;
        Description = description;
        IsPrimaryKey = isPrimaryKey;
        MaxLength = maxLength;
        Precision = precision;
        Scale = scale;
        DefaultValue = defaultValue;
        Metadata = metadata;
        IsRequired = isRequired;
        Properties = properties;
    }
} 