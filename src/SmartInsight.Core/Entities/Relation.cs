using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SmartInsight.Core.Enums;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a relationship between two entities in the knowledge graph
/// </summary>
public class Relation : BaseMultiTenantEntity
{
    /// <summary>
    /// ID of the source entity in the relationship
    /// </summary>
    [Required]
    public Guid SourceEntityId { get; set; }

    /// <summary>
    /// Navigation property to the source entity
    /// </summary>
    [ForeignKey("SourceEntityId")]
    public virtual Entity? SourceEntity { get; set; }

    /// <summary>
    /// ID of the target entity in the relationship
    /// </summary>
    [Required]
    public Guid TargetEntityId { get; set; }

    /// <summary>
    /// Navigation property to the target entity
    /// </summary>
    [ForeignKey("TargetEntityId")]
    public virtual Entity? TargetEntity { get; set; }

    /// <summary>
    /// Type of the relationship
    /// </summary>
    [Required]
    public RelationType Type { get; set; }

    /// <summary>
    /// Label or name of the relationship
    /// </summary>
    [MaxLength(100)]
    public string? Label { get; set; }

    /// <summary>
    /// Confidence score of the relationship extraction (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; set; } = 1.0f;

    /// <summary>
    /// Strength of the relationship (0.0 to 1.0)
    /// </summary>
    public float Strength { get; set; } = 1.0f;

    /// <summary>
    /// ID of the document where this relationship was discovered
    /// </summary>
    public Guid? SourceDocumentId { get; set; }

    /// <summary>
    /// Navigation property to the source document
    /// </summary>
    [ForeignKey("SourceDocumentId")]
    public virtual Document? SourceDocument { get; set; }

    /// <summary>
    /// When the relationship was discovered
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional properties of the relationship stored as JSON
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Whether the relationship is directional
    /// </summary>
    public bool IsDirectional { get; set; } = true;

    /// <summary>
    /// Frequency of occurrence across all documents
    /// </summary>
    public int Frequency { get; set; } = 1;

    /// <summary>
    /// Custom relationship type name (if Type is Custom)
    /// </summary>
    [MaxLength(100)]
    public string? CustomType { get; set; }

    /// <summary>
    /// Description or additional context for the relationship
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Snippet of text that evidences this relationship
    /// </summary>
    public string? Evidence { get; set; }
} 