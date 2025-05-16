using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Entity for storing ingestion job data in the database
/// </summary>
public class IngestionJobEntity
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    [Key]
    [Required]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for the job
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the job
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// ID of the data source to ingest from
    /// </summary>
    [Required]
    public Guid DataSourceId { get; set; }
    
    /// <summary>
    /// Current status of the job (maps to IngestionStatus enum)
    /// </summary>
    [Required]
    public int Status { get; set; }
    
    /// <summary>
    /// JSON-serialized extraction parameters for the job
    /// </summary>
    public string? ExtractionParametersJson { get; set; }
    
    /// <summary>
    /// The tenant ID that owns this job
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Cron expression defining the job schedule
    /// </summary>
    [MaxLength(100)]
    public string? CronExpression { get; set; }
    
    /// <summary>
    /// When the job was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the job was last modified
    /// </summary>
    [Required]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the job was last executed
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }
    
    /// <summary>
    /// Result of the last execution
    /// </summary>
    public string? LastExecutionResult { get; set; }
    
    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    [Required]
    public int FailureCount { get; set; } = 0;
    
    /// <summary>
    /// Determines if the job is paused
    /// </summary>
    [Required]
    public bool IsPaused { get; set; } = false;
    
    /// <summary>
    /// Maximum retries on failure
    /// </summary>
    [Required]
    public int MaxRetryCount { get; set; } = 3;
    
    /// <summary>
    /// JSON-serialized notification configuration
    /// </summary>
    public string? NotificationConfigJson { get; set; }
} 