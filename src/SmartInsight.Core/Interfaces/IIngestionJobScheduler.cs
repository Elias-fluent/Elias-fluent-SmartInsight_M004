using SmartInsight.Core.Models;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for scheduling and managing data ingestion jobs
/// </summary>
public interface IIngestionJobScheduler
{
    /// <summary>
    /// Schedules a new recurring ingestion job
    /// </summary>
    /// <param name="jobDefinition">Definition of the job to schedule</param>
    /// <returns>The scheduled job ID</returns>
    Task<string> ScheduleJobAsync(IngestionJobDefinition jobDefinition);
    
    /// <summary>
    /// Updates an existing job's definition
    /// </summary>
    /// <param name="jobDefinition">Updated job definition</param>
    /// <returns>Whether the job was updated successfully</returns>
    Task<bool> UpdateJobAsync(IngestionJobDefinition jobDefinition);
    
    /// <summary>
    /// Retrieves a job by its ID
    /// </summary>
    /// <param name="jobId">The ID of the job to retrieve</param>
    /// <returns>The job definition, or null if not found</returns>
    Task<IngestionJobDefinition?> GetJobAsync(string jobId);
    
    /// <summary>
    /// Retrieves all jobs for a given tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Collection of job definitions</returns>
    Task<IEnumerable<IngestionJobDefinition>> GetJobsForTenantAsync(Guid tenantId);
    
    /// <summary>
    /// Triggers immediate execution of a job
    /// </summary>
    /// <param name="jobId">The ID of the job to trigger</param>
    /// <returns>Whether the job was triggered successfully</returns>
    Task<bool> TriggerJobNowAsync(string jobId);
    
    /// <summary>
    /// Pauses a scheduled job
    /// </summary>
    /// <param name="jobId">The ID of the job to pause</param>
    /// <returns>Whether the job was paused successfully</returns>
    Task<bool> PauseJobAsync(string jobId);
    
    /// <summary>
    /// Resumes a paused job
    /// </summary>
    /// <param name="jobId">The ID of the job to resume</param>
    /// <returns>Whether the job was resumed successfully</returns>
    Task<bool> ResumeJobAsync(string jobId);
    
    /// <summary>
    /// Permanently removes a job
    /// </summary>
    /// <param name="jobId">The ID of the job to delete</param>
    /// <returns>Whether the job was deleted successfully</returns>
    Task<bool> DeleteJobAsync(string jobId);
} 