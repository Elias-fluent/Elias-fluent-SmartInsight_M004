using SmartInsight.Core.Models;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Repository interface for managing ingestion job data
/// </summary>
public interface IIngestionJobRepository
{
    /// <summary>
    /// Creates a new job in the repository
    /// </summary>
    /// <param name="jobDefinition">The job definition to create</param>
    /// <returns>The created job's ID</returns>
    Task<string> CreateJobAsync(IngestionJobDefinition jobDefinition);
    
    /// <summary>
    /// Updates an existing job in the repository
    /// </summary>
    /// <param name="jobDefinition">The updated job definition</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> UpdateJobAsync(IngestionJobDefinition jobDefinition);
    
    /// <summary>
    /// Retrieves a job by its ID
    /// </summary>
    /// <param name="jobId">The ID of the job to retrieve</param>
    /// <returns>The job definition, or null if not found</returns>
    Task<IngestionJobDefinition?> GetJobByIdAsync(string jobId);
    
    /// <summary>
    /// Retrieves jobs by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Collection of job definitions</returns>
    Task<IEnumerable<IngestionJobDefinition>> GetJobsByTenantIdAsync(Guid tenantId);
    
    /// <summary>
    /// Retrieves jobs by status
    /// </summary>
    /// <param name="status">The status to filter by</param>
    /// <returns>Collection of job definitions</returns>
    Task<IEnumerable<IngestionJobDefinition>> GetJobsByStatusAsync(Enums.IngestionStatus status);
    
    /// <summary>
    /// Updates a job's status
    /// </summary>
    /// <param name="jobId">The ID of the job to update</param>
    /// <param name="status">The new status</param>
    /// <param name="executionResult">Optional execution result information</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> UpdateJobStatusAsync(string jobId, Enums.IngestionStatus status, string? executionResult = null);
    
    /// <summary>
    /// Permanently removes a job
    /// </summary>
    /// <param name="jobId">The ID of the job to delete</param>
    /// <returns>Whether the deletion was successful</returns>
    Task<bool> DeleteJobAsync(string jobId);
    
    /// <summary>
    /// Updates the job's last execution time
    /// </summary>
    /// <param name="jobId">The ID of the job to update</param>
    /// <param name="executionTime">The execution time to set</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> UpdateJobExecutionTimeAsync(string jobId, DateTime executionTime);
    
    /// <summary>
    /// Increments a job's failure count
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>The new failure count</returns>
    Task<int> IncrementFailureCountAsync(string jobId);
    
    /// <summary>
    /// Resets a job's failure count to zero
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> ResetFailureCountAsync(string jobId);
} 