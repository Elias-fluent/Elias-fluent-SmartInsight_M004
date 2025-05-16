using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Enums;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Models;
using SmartInsight.Data.Contexts;
using System.Text.Json;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// SQL Server implementation of the ingestion job repository
/// </summary>
public class IngestionJobRepository : IIngestionJobRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<IngestionJobRepository> _logger;

    /// <summary>
    /// Creates a new ingestion job repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public IngestionJobRepository(ApplicationDbContext dbContext, ILogger<IngestionJobRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string> CreateJobAsync(IngestionJobDefinition jobDefinition)
    {
        try
        {
            var entity = new IngestionJobEntity
            {
                Id = jobDefinition.Id,
                Name = jobDefinition.Name,
                Description = jobDefinition.Description,
                DataSourceId = jobDefinition.DataSourceId,
                Status = (int)jobDefinition.Status,
                ExtractionParametersJson = jobDefinition.ExtractionParametersJson,
                TenantId = jobDefinition.TenantId,
                CronExpression = jobDefinition.CronExpression,
                CreatedAt = jobDefinition.CreatedAt,
                ModifiedAt = jobDefinition.ModifiedAt,
                LastExecutionTime = jobDefinition.LastExecutionTime,
                LastExecutionResult = jobDefinition.LastExecutionResult,
                FailureCount = jobDefinition.FailureCount,
                IsPaused = jobDefinition.IsPaused,
                MaxRetryCount = jobDefinition.MaxRetryCount,
                NotificationConfigJson = jobDefinition.NotificationConfigJson
            };

            await _dbContext.IngestionJobs.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ingestion job: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteJobAsync(string jobId)
    {
        try
        {
            var job = await _dbContext.IngestionJobs.FindAsync(jobId);
            
            if (job == null)
            {
                _logger.LogWarning("Attempted to delete non-existent ingestion job: {JobId}", jobId);
                return false;
            }

            _dbContext.IngestionJobs.Remove(job);
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete ingestion job {JobId}: {Message}", jobId, ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IngestionJobDefinition?> GetJobByIdAsync(string jobId)
    {
        try
        {
            var entity = await _dbContext.IngestionJobs.FindAsync(jobId);
            
            if (entity == null)
            {
                return null;
            }

            return MapEntityToDefinition(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ingestion job {JobId}: {Message}", jobId, ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IngestionJobDefinition>> GetJobsByStatusAsync(IngestionStatus status)
    {
        try
        {
            var entities = await _dbContext.IngestionJobs
                .Where(j => j.Status == (int)status)
                .ToListAsync();

            return entities.Select(MapEntityToDefinition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ingestion jobs by status {Status}: {Message}", status, ex.Message);
            return Enumerable.Empty<IngestionJobDefinition>();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IngestionJobDefinition>> GetJobsByTenantIdAsync(Guid tenantId)
    {
        try
        {
            var entities = await _dbContext.IngestionJobs
                .Where(j => j.TenantId == tenantId)
                .ToListAsync();

            return entities.Select(MapEntityToDefinition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ingestion jobs for tenant {TenantId}: {Message}", tenantId, ex.Message);
            return Enumerable.Empty<IngestionJobDefinition>();
        }
    }

    /// <inheritdoc/>
    public async Task<int> IncrementFailureCountAsync(string jobId)
    {
        try
        {
            var job = await _dbContext.IngestionJobs.FindAsync(jobId);
            
            if (job == null)
            {
                _logger.LogWarning("Attempted to increment failure count for non-existent ingestion job: {JobId}", jobId);
                return -1;
            }

            job.FailureCount++;
            job.ModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            return job.FailureCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment failure count for ingestion job {JobId}: {Message}", jobId, ex.Message);
            return -1;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ResetFailureCountAsync(string jobId)
    {
        try
        {
            var job = await _dbContext.IngestionJobs.FindAsync(jobId);
            
            if (job == null)
            {
                _logger.LogWarning("Attempted to reset failure count for non-existent ingestion job: {JobId}", jobId);
                return false;
            }

            job.FailureCount = 0;
            job.ModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset failure count for ingestion job {JobId}: {Message}", jobId, ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateJobAsync(IngestionJobDefinition jobDefinition)
    {
        try
        {
            var entity = await _dbContext.IngestionJobs.FindAsync(jobDefinition.Id);
            
            if (entity == null)
            {
                _logger.LogWarning("Attempted to update non-existent ingestion job: {JobId}", jobDefinition.Id);
                return false;
            }

            entity.Name = jobDefinition.Name;
            entity.Description = jobDefinition.Description;
            entity.DataSourceId = jobDefinition.DataSourceId;
            entity.Status = (int)jobDefinition.Status;
            entity.ExtractionParametersJson = jobDefinition.ExtractionParametersJson;
            entity.CronExpression = jobDefinition.CronExpression;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.LastExecutionTime = jobDefinition.LastExecutionTime;
            entity.LastExecutionResult = jobDefinition.LastExecutionResult;
            entity.FailureCount = jobDefinition.FailureCount;
            entity.IsPaused = jobDefinition.IsPaused;
            entity.MaxRetryCount = jobDefinition.MaxRetryCount;
            entity.NotificationConfigJson = jobDefinition.NotificationConfigJson;

            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ingestion job {JobId}: {Message}", jobDefinition.Id, ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateJobExecutionTimeAsync(string jobId, DateTime executionTime)
    {
        try
        {
            var job = await _dbContext.IngestionJobs.FindAsync(jobId);
            
            if (job == null)
            {
                _logger.LogWarning("Attempted to update execution time for non-existent ingestion job: {JobId}", jobId);
                return false;
            }

            job.LastExecutionTime = executionTime;
            job.ModifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update execution time for ingestion job {JobId}: {Message}", jobId, ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateJobStatusAsync(string jobId, IngestionStatus status, string? executionResult = null)
    {
        try
        {
            var job = await _dbContext.IngestionJobs.FindAsync(jobId);
            
            if (job == null)
            {
                _logger.LogWarning("Attempted to update status for non-existent ingestion job: {JobId}", jobId);
                return false;
            }

            job.Status = (int)status;
            job.ModifiedAt = DateTime.UtcNow;
            
            if (executionResult != null)
            {
                job.LastExecutionResult = executionResult;
            }

            await _dbContext.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for ingestion job {JobId}: {Message}", jobId, ex.Message);
            return false;
        }
    }

    private IngestionJobDefinition MapEntityToDefinition(IngestionJobEntity entity)
    {
        return new IngestionJobDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DataSourceId = entity.DataSourceId,
            Status = (IngestionStatus)entity.Status,
            ExtractionParametersJson = entity.ExtractionParametersJson,
            TenantId = entity.TenantId,
            CronExpression = entity.CronExpression,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            LastExecutionTime = entity.LastExecutionTime,
            LastExecutionResult = entity.LastExecutionResult,
            FailureCount = entity.FailureCount,
            IsPaused = entity.IsPaused,
            MaxRetryCount = entity.MaxRetryCount,
            NotificationConfigJson = entity.NotificationConfigJson
        };
    }
} 