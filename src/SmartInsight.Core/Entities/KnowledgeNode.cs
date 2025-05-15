using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a node in the knowledge graph with connections to related entities
/// </summary>
public class KnowledgeNode : BaseMultiTenantEntity
{
    /// <summary>
    /// Title or name of the knowledge node
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content or information represented by this node
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Type of the knowledge node (e.g., concept, topic, fact)
    /// </summary>
    [MaxLength(50)]
    public string Type { get; set; } = "Concept";

    /// <summary>
    /// ID of the entity associated with this node, if applicable
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Navigation property to the associated entity
    /// </summary>
    [ForeignKey("EntityId")]
    public virtual Entity? Entity { get; set; }

    /// <summary>
    /// Vector embedding of the node for similarity search
    /// </summary>
    public string? VectorEmbedding { get; set; }

    /// <summary>
    /// IDs of related knowledge nodes, stored as JSON array
    /// </summary>
    public string? RelatedNodeIds { get; set; }

    /// <summary>
    /// Semantic properties stored as JSON
    /// </summary>
    public string? SemanticProperties { get; set; }

    /// <summary>
    /// Importance score of the node (0.0 to 1.0)
    /// </summary>
    public float Importance { get; set; } = 0.5f;

    /// <summary>
    /// Confidence score of the information (0.0 to 1.0)
    /// </summary>
    public float ConfidenceScore { get; set; } = 1.0f;

    /// <summary>
    /// Source document IDs where this knowledge was extracted from, stored as JSON array
    /// </summary>
    public string? SourceDocumentIds { get; set; }

    /// <summary>
    /// When the knowledge was first discovered
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the knowledge was last updated or validated
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }

    /// <summary>
    /// Tags or categories for the knowledge node
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Whether the node has been manually verified by a user
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Who verified the node
    /// </summary>
    [MaxLength(256)]
    public string? VerifiedBy { get; set; }

    /// <summary>
    /// When the node was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Number of times this knowledge node has been retrieved in queries
    /// </summary>
    public int RetrievalCount { get; set; } = 0;

    /// <summary>
    /// ID of the vector index where this node is stored
    /// </summary>
    public Guid? VectorIndexId { get; set; }

    /// <summary>
    /// Navigation property to the vector index
    /// </summary>
    [ForeignKey("VectorIndexId")]
    public virtual VectorIndex? VectorIndex { get; set; }
} 