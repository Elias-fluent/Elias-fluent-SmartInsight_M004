using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SmartInsight.Core.Enums;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a document or content item extracted from a data source
/// </summary>
public class Document : BaseMultiTenantEntity
{
    /// <summary>
    /// Title or name of the document
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Type of the document
    /// </summary>
    [Required]
    public DocumentType Type { get; set; }

    /// <summary>
    /// Original content of the document (may be truncated for large documents)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// URI or path to the original document
    /// </summary>
    [MaxLength(2048)]
    public string? SourceUri { get; set; }

    /// <summary>
    /// MIME type of the document
    /// </summary>
    [MaxLength(100)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Size of the document in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Hash of the document content for change detection
    /// </summary>
    [MaxLength(128)]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Metadata about the document stored as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the document was originally created (from source metadata)
    /// </summary>
    public DateTime? OriginalCreatedAt { get; set; }

    /// <summary>
    /// When the document was last modified (from source metadata)
    /// </summary>
    public DateTime? OriginalModifiedAt { get; set; }

    /// <summary>
    /// Who originally created the document (from source metadata)
    /// </summary>
    [MaxLength(256)]
    public string? OriginalAuthor { get; set; }

    /// <summary>
    /// When the document was ingested into the system
    /// </summary>
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the document was last processed for entity extraction
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Status of the document processing
    /// </summary>
    [MaxLength(50)]
    public string ProcessingStatus { get; set; } = "Pending";

    /// <summary>
    /// ID of the data source this document was extracted from
    /// </summary>
    public Guid DataSourceId { get; set; }

    /// <summary>
    /// Navigation property to the data source
    /// </summary>
    [ForeignKey("DataSourceId")]
    public virtual DataSource? DataSource { get; set; }

    /// <summary>
    /// Navigation property to entities extracted from this document
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Entity>? Entities { get; set; }

    /// <summary>
    /// Language of the document content (ISO 639-1 code)
    /// </summary>
    [MaxLength(10)]
    public string? Language { get; set; }

    /// <summary>
    /// Whether the document is considered confidential
    /// </summary>
    public bool IsConfidential { get; set; } = false;

    /// <summary>
    /// Access level required to view this document (0=Public, 1=Internal, 2=Confidential, 3=Restricted)
    /// </summary>
    public int AccessLevel { get; set; } = 0;
} 