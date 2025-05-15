namespace SmartInsight.Core.Enums;

/// <summary>
/// Status of data ingestion jobs
/// </summary>
public enum IngestionStatus
{
    /// <summary>
    /// Job is scheduled but not started yet
    /// </summary>
    Scheduled,
    
    /// <summary>
    /// Job is currently in the queue
    /// </summary>
    Queued,
    
    /// <summary>
    /// Job is currently running
    /// </summary>
    Running,
    
    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Job failed with errors
    /// </summary>
    Failed,
    
    /// <summary>
    /// Job was cancelled by user or system
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Job is partially completed with some errors
    /// </summary>
    PartialSuccess
} 