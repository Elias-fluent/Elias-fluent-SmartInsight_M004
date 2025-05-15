using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a log of chat interactions between users and the system
/// </summary>
public class ConversationLog : BaseMultiTenantEntity
{
    /// <summary>
    /// Unique conversation session ID
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// ID of the user who initiated the conversation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    /// <summary>
    /// User's query or message
    /// </summary>
    [Required]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// System's response to the query
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Generated SQL query, if applicable
    /// </summary>
    public string? GeneratedSql { get; set; }

    /// <summary>
    /// When the query was received
    /// </summary>
    public DateTime QueryTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the response was sent
    /// </summary>
    public DateTime? ResponseTimestamp { get; set; }

    /// <summary>
    /// Time taken to process the query in milliseconds
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// User's feedback on the response quality (1-5 scale)
    /// </summary>
    public int? FeedbackRating { get; set; }

    /// <summary>
    /// Additional feedback comments from the user
    /// </summary>
    [MaxLength(1000)]
    public string? FeedbackComments { get; set; }

    /// <summary>
    /// When feedback was provided
    /// </summary>
    public DateTime? FeedbackTimestamp { get; set; }

    /// <summary>
    /// Whether the query was successful
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Error message if the query failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Context or state information for the conversation, stored as JSON
    /// </summary>
    public string? ConversationContext { get; set; }

    /// <summary>
    /// Sequence number of this message in the conversation
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Source of knowledge used to answer the query
    /// </summary>
    [MaxLength(255)]
    public string? KnowledgeSource { get; set; }

    /// <summary>
    /// Language model used to generate the response
    /// </summary>
    [MaxLength(100)]
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Tokens used in processing the query and generating the response
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Tags or categories for the conversation
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }
} 