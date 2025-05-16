using SmartInsight.Core.Enums;

namespace SmartInsight.Core.Models;

/// <summary>
/// Defines a data ingestion job
/// </summary>
public class IngestionJobDefinition
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for the job
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the job
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// ID of the data source to ingest from
    /// </summary>
    public Guid DataSourceId { get; set; }
    
    /// <summary>
    /// Current status of the job
    /// </summary>
    public IngestionStatus Status { get; set; } = IngestionStatus.Scheduled;
    
    /// <summary>
    /// JSON-serialized extraction parameters for the job
    /// </summary>
    public string? ExtractionParametersJson { get; set; }
    
    /// <summary>
    /// The tenant ID that owns this job
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Cron expression defining the job schedule
    /// </summary>
    public string? CronExpression { get; set; }
    
    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the job was last modified
    /// </summary>
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
    public int FailureCount { get; set; } = 0;
    
    /// <summary>
    /// Determines if the job is paused
    /// </summary>
    public bool IsPaused { get; set; } = false;
    
    /// <summary>
    /// Maximum retries on failure
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
    
    /// <summary>
    /// JSON-serialized notification configuration
    /// </summary>
    public string? NotificationConfigJson { get; set; }
} 