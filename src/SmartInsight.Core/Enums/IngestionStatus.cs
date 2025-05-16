namespace SmartInsight.Core.Enums;

/// <summary>
/// Status of a data ingestion job
/// </summary>
public enum IngestionStatus
{
    /// <summary>
    /// The job is scheduled but has not run yet
    /// </summary>
    Scheduled,
    
    /// <summary>
    /// The job is currently running
    /// </summary>
    Running,
    
    /// <summary>
    /// The job completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// The job failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// The job is currently paused
    /// </summary>
    Paused,
    
    /// <summary>
    /// The job was cancelled
    /// </summary>
    Cancelled
} 