using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a vector index for similarity search operations
/// </summary>
public class VectorIndex : BaseMultiTenantEntity
{
    /// <summary>
    /// Name of the vector index
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the vector index
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of the vector index (e.g., HNSW, PQ, IVF)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string IndexType { get; set; } = "HNSW";

    /// <summary>
    /// Dimension of vectors stored in this index
    /// </summary>
    [Required]
    public int Dimensions { get; set; } = 768;

    /// <summary>
    /// Metric used for similarity calculation (e.g., Cosine, Euclidean)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Metric { get; set; } = "Cosine";

    /// <summary>
    /// Status of the index (e.g., Building, Ready, Error)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Ready";

    /// <summary>
    /// Configuration parameters for the index stored as JSON
    /// </summary>
    public string? IndexParameters { get; set; }

    /// <summary>
    /// When the index was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the index was last updated or rebuilt
    /// </summary>
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>
    /// Number of vectors stored in the index
    /// </summary>
    public int VectorCount { get; set; } = 0;

    /// <summary>
    /// External ID in the vector database system
    /// </summary>
    [MaxLength(255)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Database name or collection name in the vector database
    /// </summary>
    [MaxLength(100)]
    public string? CollectionName { get; set; }

    /// <summary>
    /// Type of data stored in the vectors (e.g., Document, Entity, Knowledge)
    /// </summary>
    [MaxLength(50)]
    public string DataType { get; set; } = "Knowledge";

    /// <summary>
    /// Name of the embedding model used
    /// </summary>
    [MaxLength(100)]
    public string? EmbeddingModel { get; set; }

    /// <summary>
    /// Navigation property to knowledge nodes stored in this index
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<KnowledgeNode>? KnowledgeNodes { get; set; }

    /// <summary>
    /// Retention policy for vectors in days (0 = indefinite)
    /// </summary>
    public int RetentionDays { get; set; } = 0;

    /// <summary>
    /// Search parameters stored as JSON
    /// </summary>
    public string? SearchParameters { get; set; }
} 