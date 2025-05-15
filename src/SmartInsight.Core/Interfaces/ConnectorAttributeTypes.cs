using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Provides connection parameter metadata for connector types
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ConnectionParameterAttribute : Attribute
{
    /// <summary>
    /// Name/key of the parameter
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Display name of the parameter
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// Description of the parameter
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Data type of the parameter
    /// </summary>
    public string Type { get; }
    
    /// <summary>
    /// Whether the parameter is required
    /// </summary>
    public bool IsRequired { get; set; } = true;
    
    /// <summary>
    /// Whether the parameter is a sensitive credential
    /// </summary>
    public bool IsSecret { get; set; } = false;
    
    /// <summary>
    /// Default value for the parameter
    /// </summary>
    public object? DefaultValue { get; set; }
    
    /// <summary>
    /// Group this parameter belongs to
    /// </summary>
    public string? Group { get; set; }
    
    /// <summary>
    /// Order for display purposes
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Creates a new connection parameter attribute
    /// </summary>
    /// <param name="name">Name/key of the parameter</param>
    /// <param name="displayName">Display name of the parameter</param>
    /// <param name="description">Description of the parameter</param>
    /// <param name="type">Data type of the parameter</param>
    public ConnectionParameterAttribute(
        string name,
        string displayName,
        string description, 
        string type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));
            
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
            
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));
            
        Name = name;
        DisplayName = displayName;
        Description = description;
        Type = type;
    }
}

/// <summary>
/// Defines parameter validation rules
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ParameterValidationAttribute : Attribute
{
    /// <summary>
    /// Name of the parameter to validate
    /// </summary>
    public string ParameterName { get; }
    
    /// <summary>
    /// Validation type (e.g., "regex", "min", "max", "required", etc.)
    /// </summary>
    public string ValidationType { get; }
    
    /// <summary>
    /// Validation rule/constraint
    /// </summary>
    public string ValidationRule { get; }
    
    /// <summary>
    /// Error message to display if validation fails
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// Creates a new parameter validation attribute
    /// </summary>
    /// <param name="parameterName">Name of the parameter to validate</param>
    /// <param name="validationType">Validation type</param>
    /// <param name="validationRule">Validation rule/constraint</param>
    public ParameterValidationAttribute(
        string parameterName, 
        string validationType, 
        string validationRule)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            throw new ArgumentException("Parameter name cannot be empty", nameof(parameterName));
            
        if (string.IsNullOrWhiteSpace(validationType))
            throw new ArgumentException("Validation type cannot be empty", nameof(validationType));
            
        ParameterName = parameterName;
        ValidationType = validationType;
        ValidationRule = validationRule;
        ErrorMessage = $"Invalid value for {parameterName}";
    }
}

/// <summary>
/// Defines parameter enum options for dropdown/list values
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ParameterEnumValueAttribute : Attribute
{
    /// <summary>
    /// Name of the parameter this enum value is for
    /// </summary>
    public string ParameterName { get; }
    
    /// <summary>
    /// Actual value of the enum option
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Display text for the enum option
    /// </summary>
    public string DisplayText { get; }
    
    /// <summary>
    /// Description of the enum option
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Creates a new parameter enum value attribute
    /// </summary>
    /// <param name="parameterName">Name of the parameter this enum value is for</param>
    /// <param name="value">Actual value of the enum option</param>
    /// <param name="displayText">Display text for the enum option</param>
    public ParameterEnumValueAttribute(
        string parameterName,
        string value,
        string displayText)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            throw new ArgumentException("Parameter name cannot be empty", nameof(parameterName));
            
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty", nameof(value));
            
        if (string.IsNullOrWhiteSpace(displayText))
            throw new ArgumentException("Display text cannot be empty", nameof(displayText));
            
        ParameterName = parameterName;
        Value = value;
        DisplayText = displayText;
    }
}

/// <summary>
/// Defines connector capabilities
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ConnectorCapabilitiesAttribute : Attribute
{
    /// <summary>
    /// Whether the connector supports incremental extraction
    /// </summary>
    public bool SupportsIncremental { get; set; } = false;
    
    /// <summary>
    /// Whether the connector supports advanced filtering
    /// </summary>
    public bool SupportsAdvancedFiltering { get; set; } = false;
    
    /// <summary>
    /// Whether the connector can resume from a checkpoint
    /// </summary>
    public bool SupportsResume { get; set; } = false;
    
    /// <summary>
    /// Whether the connector supports scheduled execution
    /// </summary>
    public bool SupportsScheduling { get; set; } = true;
    
    /// <summary>
    /// Whether the connector supports schema discovery
    /// </summary>
    public bool SupportsSchemaDiscovery { get; set; } = false;
    
    /// <summary>
    /// Whether the connector supports data previews
    /// </summary>
    public bool SupportsPreview { get; set; } = false;
    
    /// <summary>
    /// Whether the connector supports complex transformations
    /// </summary>
    public bool SupportsTransformation { get; set; } = false;
    
    /// <summary>
    /// Whether the connector supports progress reporting
    /// </summary>
    public bool SupportsProgressReporting { get; set; } = false;
    
    /// <summary>
    /// Maximum number of concurrent extractions supported
    /// </summary>
    public int MaxConcurrentExtractions { get; set; } = 1;
    
    /// <summary>
    /// Types of authentication supported by the connector
    /// </summary>
    public string[]? SupportedAuthentications { get; set; }
    
    /// <summary>
    /// Data source types this connector supports
    /// </summary>
    public string[]? SupportedSourceTypes { get; set; }
}

/// <summary>
/// Defines connector category
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ConnectorCategoryAttribute : Attribute
{
    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Creates a new connector category attribute
    /// </summary>
    /// <param name="name">Category name</param>
    public ConnectorCategoryAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));
            
        Name = name;
    }
}

/// <summary>
/// Defines schema information for a connector data structure
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ConnectorSchemaAttribute : Attribute
{
    /// <summary>
    /// Schema identifier
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Schema display name
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Schema description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Schema JSON definition
    /// </summary>
    public string SchemaDefinition { get; }
    
    /// <summary>
    /// Creates a new connector schema attribute
    /// </summary>
    /// <param name="id">Schema identifier</param>
    /// <param name="name">Schema display name</param>
    /// <param name="schemaDefinition">Schema JSON definition</param>
    public ConnectorSchemaAttribute(
        string id,
        string name,
        string schemaDefinition)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Schema ID cannot be empty", nameof(id));
            
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Schema name cannot be empty", nameof(name));
            
        if (string.IsNullOrWhiteSpace(schemaDefinition))
            throw new ArgumentException("Schema definition cannot be empty", nameof(schemaDefinition));
            
        Id = id;
        Name = name;
        SchemaDefinition = schemaDefinition;
    }
} 