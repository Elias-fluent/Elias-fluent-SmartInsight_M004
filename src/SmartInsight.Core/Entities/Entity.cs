using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SmartInsight.Core.Enums;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a named entity extracted from documents
/// </summary>
public class Entity : BaseMultiTenantEntity
{
    /// <summary>
    /// Name of the entity
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the entity
    /// </summary>
    [Required]
    public EntityType Type { get; set; }

    /// <summary>
    /// Confidence score of the entity extraction (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; set; } = 1.0f;

    /// <summary>
    /// Additional attributes of the entity stored as JSON
    /// </summary>
    public string? Attributes { get; set; }

    /// <summary>
    /// Description or summary of the entity
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// URI to additional information about the entity
    /// </summary>
    [MaxLength(2048)]
    public string? InfoUri { get; set; }

    /// <summary>
    /// Normalized form of the entity name
    /// </summary>
    [MaxLength(255)]
    public string? NormalizedName { get; set; }

    /// <summary>
    /// ID of the document this entity was first extracted from
    /// </summary>
    public Guid? SourceDocumentId { get; set; }

    /// <summary>
    /// Navigation property to the source document
    /// </summary>
    [ForeignKey("SourceDocumentId")]
    public virtual Document? SourceDocument { get; set; }

    /// <summary>
    /// Vector embedding of the entity for similarity search
    /// </summary>
    public string? VectorEmbedding { get; set; }

    /// <summary>
    /// Frequency of occurrence across all documents
    /// </summary>
    public int Frequency { get; set; } = 1;

    /// <summary>
    /// When the entity was first discovered
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the entity was last referenced in any document
    /// </summary>
    public DateTime? LastReferencedAt { get; set; }

    /// <summary>
    /// Navigation property to relationships where this entity is the source
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Relation>? SourceRelations { get; set; }

    /// <summary>
    /// Navigation property to relationships where this entity is the target
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Relation>? TargetRelations { get; set; }

    /// <summary>
    /// Custom entity type name (if Type is Custom)
    /// </summary>
    [MaxLength(100)]
    public string? CustomType { get; set; }

    /// <summary>
    /// External ID if the entity is linked to an external system
    /// </summary>
    [MaxLength(255)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Importance score of the entity (0.0 to 1.0)
    /// </summary>
    public float Importance { get; set; } = 0.5f;
} 