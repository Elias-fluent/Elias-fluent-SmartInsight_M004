using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a domain-specific vocabulary term with definitions and synonyms
/// </summary>
public class Term : BaseMultiTenantEntity
{
    /// <summary>
    /// Name of the term
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Definition of the term
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// Short form or abbreviation of the term, if applicable
    /// </summary>
    [MaxLength(50)]
    public string? Abbreviation { get; set; }

    /// <summary>
    /// List of synonyms for the term, stored as JSON array
    /// </summary>
    public string? Synonyms { get; set; }

    /// <summary>
    /// Domain or category the term belongs to
    /// </summary>
    [MaxLength(100)]
    public string? Domain { get; set; }

    /// <summary>
    /// Source of the term definition
    /// </summary>
    [MaxLength(500)]
    public string? Source { get; set; }

    /// <summary>
    /// URI to additional information about the term
    /// </summary>
    [MaxLength(2048)]
    public string? InfoUri { get; set; }

    /// <summary>
    /// Whether the term has been manually verified by a user
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Who verified the term
    /// </summary>
    [MaxLength(256)]
    public string? VerifiedBy { get; set; }

    /// <summary>
    /// When the term was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Version or revision of the term
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Notes or additional information about the term
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Related terms, stored as JSON array of term IDs
    /// </summary>
    public string? RelatedTerms { get; set; }

    /// <summary>
    /// Tags or labels associated with the term
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Frequency of use or occurrence in the knowledge corpus
    /// </summary>
    public int UsageFrequency { get; set; } = 0;

    /// <summary>
    /// Importance or relevance score (0.0 to 1.0)
    /// </summary>
    public float ImportanceScore { get; set; } = 0.5f;
} 