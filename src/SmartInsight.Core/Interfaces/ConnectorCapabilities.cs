namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents the capabilities of a connector
/// </summary>
public class ConnectorCapabilities
{
    /// <summary>
    /// Whether the connector supports incremental extraction
    /// </summary>
    public bool SupportsIncremental { get; }
    
    /// <summary>
    /// Whether the connector supports advanced filtering
    /// </summary>
    public bool SupportsAdvancedFiltering { get; }
    
    /// <summary>
    /// Whether the connector can resume from a checkpoint
    /// </summary>
    public bool SupportsResume { get; }
    
    /// <summary>
    /// Whether the connector supports scheduled execution
    /// </summary>
    public bool SupportsScheduling { get; }
    
    /// <summary>
    /// Whether the connector supports schema discovery
    /// </summary>
    public bool SupportsSchemaDiscovery { get; }
    
    /// <summary>
    /// Whether the connector supports data previews
    /// </summary>
    public bool SupportsPreview { get; }
    
    /// <summary>
    /// Whether the connector supports complex transformations
    /// </summary>
    public bool SupportsTransformation { get; }
    
    /// <summary>
    /// Whether the connector supports progress reporting
    /// </summary>
    public bool SupportsProgressReporting { get; }
    
    /// <summary>
    /// Maximum number of concurrent extractions supported
    /// </summary>
    public int MaxConcurrentExtractions { get; }
    
    /// <summary>
    /// Types of authentication supported by the connector
    /// </summary>
    public IReadOnlyList<string> SupportedAuthentications { get; }
    
    /// <summary>
    /// Data source types this connector supports
    /// </summary>
    public IReadOnlyList<string> SupportedSourceTypes { get; }
    
    /// <summary>
    /// Creates a new connector capabilities instance
    /// </summary>
    /// <param name="supportsIncremental">Whether the connector supports incremental extraction</param>
    /// <param name="supportsAdvancedFiltering">Whether the connector supports advanced filtering</param>
    /// <param name="supportsResume">Whether the connector can resume from a checkpoint</param>
    /// <param name="supportsScheduling">Whether the connector supports scheduled execution</param>
    /// <param name="supportsSchemaDiscovery">Whether the connector supports schema discovery</param>
    /// <param name="supportsPreview">Whether the connector supports data previews</param>
    /// <param name="supportsTransformation">Whether the connector supports complex transformations</param>
    /// <param name="supportsProgressReporting">Whether the connector supports progress reporting</param>
    /// <param name="maxConcurrentExtractions">Maximum number of concurrent extractions supported</param>
    /// <param name="supportedAuthentications">Types of authentication supported by the connector</param>
    /// <param name="supportedSourceTypes">Data source types this connector supports</param>
    public ConnectorCapabilities(
        bool supportsIncremental = false,
        bool supportsAdvancedFiltering = false,
        bool supportsResume = false,
        bool supportsScheduling = true,
        bool supportsSchemaDiscovery = false,
        bool supportsPreview = false,
        bool supportsTransformation = false,
        bool supportsProgressReporting = false,
        int maxConcurrentExtractions = 1,
        IEnumerable<string>? supportedAuthentications = null,
        IEnumerable<string>? supportedSourceTypes = null)
    {
        SupportsIncremental = supportsIncremental;
        SupportsAdvancedFiltering = supportsAdvancedFiltering;
        SupportsResume = supportsResume;
        SupportsScheduling = supportsScheduling;
        SupportsSchemaDiscovery = supportsSchemaDiscovery;
        SupportsPreview = supportsPreview;
        SupportsTransformation = supportsTransformation;
        SupportsProgressReporting = supportsProgressReporting;
        MaxConcurrentExtractions = maxConcurrentExtractions;
        
        // Default authentications and source types if not provided
        SupportedAuthentications = supportedAuthentications?.ToList().AsReadOnly() 
            ?? new List<string> { "basic" }.AsReadOnly();
        SupportedSourceTypes = supportedSourceTypes?.ToList().AsReadOnly() 
            ?? new List<string> { "generic" }.AsReadOnly();
    }
}

/// <summary>
/// Metadata about a connector
/// </summary>
public class ConnectorMetadata
{
    /// <summary>
    /// Connector ID
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Human-readable name of the connector
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Version of the connector
    /// </summary>
    public string Version { get; }
    
    /// <summary>
    /// Description of the connector
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Author of the connector
    /// </summary>
    public string Author { get; }
    
    /// <summary>
    /// When the connector was created
    /// </summary>
    public DateTime CreatedAt { get; }
    
    /// <summary>
    /// When the connector was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; }
    
    /// <summary>
    /// Documentation URL for the connector
    /// </summary>
    public string? DocumentationUrl { get; }
    
    /// <summary>
    /// Icon for the connector (Base64 encoded image or URL)
    /// </summary>
    public string? Icon { get; }
    
    /// <summary>
    /// Categories the connector belongs to
    /// </summary>
    public IReadOnlyList<string> Categories { get; }
    
    /// <summary>
    /// Required connection parameters
    /// </summary>
    public IReadOnlyList<ConnectionParameter> RequiredParameters { get; }
    
    /// <summary>
    /// Optional connection parameters
    /// </summary>
    public IReadOnlyList<ConnectionParameter> OptionalParameters { get; }
    
    /// <summary>
    /// Creates a new connector metadata instance
    /// </summary>
    /// <param name="id">Connector ID</param>
    /// <param name="name">Human-readable name of the connector</param>
    /// <param name="version">Version of the connector</param>
    /// <param name="description">Description of the connector</param>
    /// <param name="author">Author of the connector</param>
    /// <param name="requiredParameters">Required connection parameters</param>
    /// <param name="createdAt">When the connector was created</param>
    /// <param name="updatedAt">When the connector was last updated</param>
    /// <param name="documentationUrl">Documentation URL for the connector</param>
    /// <param name="icon">Icon for the connector (Base64 encoded image or URL)</param>
    /// <param name="categories">Categories the connector belongs to</param>
    /// <param name="optionalParameters">Optional connection parameters</param>
    public ConnectorMetadata(
        string id,
        string name,
        string version,
        string description,
        string author,
        IEnumerable<ConnectionParameter> requiredParameters,
        DateTime? createdAt = null,
        DateTime? updatedAt = null,
        string? documentationUrl = null,
        string? icon = null,
        IEnumerable<string>? categories = null,
        IEnumerable<ConnectionParameter>? optionalParameters = null)
    {
        Id = id;
        Name = name;
        Version = version;
        Description = description;
        Author = author;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        UpdatedAt = updatedAt;
        DocumentationUrl = documentationUrl;
        Icon = icon;
        Categories = categories?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        RequiredParameters = requiredParameters.ToList().AsReadOnly();
        OptionalParameters = optionalParameters?.ToList().AsReadOnly() ?? new List<ConnectionParameter>().AsReadOnly();
    }
}

/// <summary>
/// Describes a connection parameter for a connector
/// </summary>
public class ConnectionParameter
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
    /// Whether the parameter is a sensitive credential
    /// </summary>
    public bool IsSecret { get; }
    
    /// <summary>
    /// Default value for the parameter
    /// </summary>
    public object? DefaultValue { get; }
    
    /// <summary>
    /// Validation constraints for the parameter
    /// </summary>
    public IDictionary<string, object>? Validation { get; }
    
    /// <summary>
    /// Possible values for the parameter (for enum types)
    /// </summary>
    public IReadOnlyList<EnumValue>? EnumValues { get; }
    
    /// <summary>
    /// Whether the parameter is required
    /// </summary>
    public bool IsRequired { get; }
    
    /// <summary>
    /// Group this parameter belongs to
    /// </summary>
    public string? Group { get; }
    
    /// <summary>
    /// Order for display purposes
    /// </summary>
    public int Order { get; }
    
    /// <summary>
    /// Creates a new connection parameter
    /// </summary>
    /// <param name="name">Name/key of the parameter</param>
    /// <param name="displayName">Display name of the parameter</param>
    /// <param name="description">Description of the parameter</param>
    /// <param name="type">Data type of the parameter</param>
    /// <param name="isRequired">Whether the parameter is required</param>
    /// <param name="isSecret">Whether the parameter is a sensitive credential</param>
    /// <param name="defaultValue">Default value for the parameter</param>
    /// <param name="validation">Validation constraints for the parameter</param>
    /// <param name="enumValues">Possible values for the parameter (for enum types)</param>
    /// <param name="group">Group this parameter belongs to</param>
    /// <param name="order">Order for display purposes</param>
    public ConnectionParameter(
        string name,
        string displayName,
        string description,
        string type,
        bool isRequired = true,
        bool isSecret = false,
        object? defaultValue = null,
        IDictionary<string, object>? validation = null,
        IEnumerable<EnumValue>? enumValues = null,
        string? group = null,
        int order = 0)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        Type = type;
        IsRequired = isRequired;
        IsSecret = isSecret;
        DefaultValue = defaultValue;
        Validation = validation;
        EnumValues = enumValues?.ToList().AsReadOnly();
        Group = group;
        Order = order;
    }
}

/// <summary>
/// Represents a value in an enumeration
/// </summary>
public class EnumValue
{
    /// <summary>
    /// Value to store
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Display text for the value
    /// </summary>
    public string DisplayText { get; }
    
    /// <summary>
    /// Description of the value
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Icon for the value
    /// </summary>
    public string? Icon { get; }
    
    /// <summary>
    /// Creates a new enum value
    /// </summary>
    /// <param name="value">Value to store</param>
    /// <param name="displayText">Display text for the value</param>
    /// <param name="description">Description of the value</param>
    /// <param name="icon">Icon for the value</param>
    public EnumValue(string value, string displayText, string? description = null, string? icon = null)
    {
        Value = value;
        DisplayText = displayText;
        Description = description;
        Icon = icon;
    }
} 