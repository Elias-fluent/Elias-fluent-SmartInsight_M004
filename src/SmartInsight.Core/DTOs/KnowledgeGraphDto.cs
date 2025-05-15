namespace SmartInsight.Core.DTOs;

/// <summary>
/// DTO for entity information in the knowledge graph
/// </summary>
public record EntityDto
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of the entity (e.g., Person, Organization, Document)
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Name or label of the entity
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Tenant ID that owns this entity
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    
    /// <summary>
    /// Source reference where the entity was extracted from
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// Dictionary of properties for the entity
    /// </summary>
    public IDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Confidence score for entity extraction (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; init; } = 1.0f;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// DTO for creating a new entity in the knowledge graph
/// </summary>
public record CreateEntityDto
{
    /// <summary>
    /// Type of the entity (e.g., Person, Organization, Document)
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// Name or label of the entity
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Tenant ID that owns this entity
    /// </summary>
    public required string TenantId { get; init; }
    
    /// <summary>
    /// Source reference where the entity was extracted from
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// Dictionary of properties for the entity
    /// </summary>
    public IDictionary<string, object>? Properties { get; init; }
    
    /// <summary>
    /// Confidence score for entity extraction (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; init; } = 1.0f;
    
    /// <summary>
    /// Vector embedding for semantic operations
    /// </summary>
    public float[]? Embedding { get; init; }
}

/// <summary>
/// DTO for relation information in the knowledge graph
/// </summary>
public record RelationDto
{
    /// <summary>
    /// Unique identifier for the relation
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// ID of the source entity
    /// </summary>
    public string SourceEntityId { get; init; } = string.Empty;
    
    /// <summary>
    /// ID of the target entity
    /// </summary>
    public string TargetEntityId { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of relation
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Tenant ID that owns this relation
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    
    /// <summary>
    /// Dictionary of properties for the relation
    /// </summary>
    public IDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Confidence score for relation extraction (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; init; } = 1.0f;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// DTO for creating a new relation in the knowledge graph
/// </summary>
public record CreateRelationDto
{
    /// <summary>
    /// ID of the source entity
    /// </summary>
    public required string SourceEntityId { get; init; }
    
    /// <summary>
    /// ID of the target entity
    /// </summary>
    public required string TargetEntityId { get; init; }
    
    /// <summary>
    /// Type of relation
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// Tenant ID that owns this relation
    /// </summary>
    public required string TenantId { get; init; }
    
    /// <summary>
    /// Dictionary of properties for the relation
    /// </summary>
    public IDictionary<string, object>? Properties { get; init; }
    
    /// <summary>
    /// Confidence score for relation extraction (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; init; } = 1.0f;
} 