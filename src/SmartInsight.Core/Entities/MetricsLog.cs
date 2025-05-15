using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a log entry for system performance and usage metrics
/// </summary>
public class MetricsLog : BaseEntity
{
    /// <summary>
    /// Category of the metric (e.g., Performance, Usage, Error)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Name of the specific metric being recorded
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Value of the metric (as a string for flexibility)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional numeric value for aggregate calculations
    /// </summary>
    public double? NumericValue { get; set; }

    /// <summary>
    /// Units of measurement for the metric
    /// </summary>
    [MaxLength(50)]
    public string? Unit { get; set; }

    /// <summary>
    /// When the metric was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Component or service that generated the metric
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// ID of the tenant associated with this metric, if applicable
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// ID of the user associated with this metric, if applicable
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Additional context data stored as JSON
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Tags for easier grouping and filtering
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Severity level (0=Info, 1=Warning, 2=Error, 3=Critical)
    /// </summary>
    public int SeverityLevel { get; set; } = 0;

    /// <summary>
    /// Whether an alert was triggered for this metric
    /// </summary>
    public bool AlertTriggered { get; set; } = false;

    /// <summary>
    /// Status of the alert if triggered (e.g., "Pending", "Acknowledged", "Resolved")
    /// </summary>
    [MaxLength(50)]
    public string? AlertStatus { get; set; }
} 