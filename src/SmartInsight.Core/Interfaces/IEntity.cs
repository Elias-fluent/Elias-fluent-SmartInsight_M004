namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for entities in the knowledge graph
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Type of the entity (e.g., Person, Organization, Document)
    /// </summary>
    string Type { get; }
    
    /// <summary>
    /// Name or label of the entity
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Tenant ID that owns this entity
    /// </summary>
    string TenantId { get; }
    
    /// <summary>
    /// Source reference where the entity was extracted from
    /// </summary>
    string? Source { get; }
    
    /// <summary>
    /// Dictionary of properties for the entity
    /// </summary>
    IDictionary<string, object> Properties { get; }
    
    /// <summary>
    /// Confidence score for entity extraction (0.0 to 1.0)
    /// </summary>
    float ConfidenceScore { get; }
    
    /// <summary>
    /// Vector embedding for semantic operations
    /// </summary>
    float[]? Embedding { get; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    DateTimeOffset UpdatedAt { get; }
} 