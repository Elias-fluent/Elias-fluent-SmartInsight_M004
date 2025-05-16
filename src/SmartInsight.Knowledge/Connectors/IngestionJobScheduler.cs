using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartInsight.Core.Enums;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Models;
using SmartInsight.Knowledge.Connectors.Interfaces;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SmartInsight.Knowledge.Connectors;

/// <summary>
/// Implementation of the ingestion job scheduler using Hangfire
/// </summary>
public class IngestionJobScheduler : IIngestionJobScheduler
{
    private readonly IIngestionJobRepository _repository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IJobNotificationService _notificationService;
    private readonly ILogger<IngestionJobScheduler> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new ingestion job scheduler
    /// </summary>
    /// <param name="repository">Job repository</param>
    /// <param name="backgroundJobClient">Hangfire background job client</param>
    /// <param name="recurringJobManager">Hangfire recurring job manager</param>
    /// <param name="notificationService">Notification service</param>
    /// <param name="logger">Logger</param>
    /// <param name="serviceProvider">Service provider for resolving dependencies</param>
    public IngestionJobScheduler(
        IIngestionJobRepository repository,
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        IJobNotificationService notificationService,
        ILogger<IngestionJobScheduler> logger,
        IServiceProvider serviceProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteJobAsync(string jobId)
    {
        _logger.LogInformation("Deleting ingestion job {JobId}", jobId);
        
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to delete non-existent job: {JobId}", jobId);
            return false;
        }
        
        // Remove from Hangfire
        if (!string.IsNullOrEmpty(job.CronExpression))
        {
            _recurringJobManager.RemoveIfExists(jobId);
        }
        
        // Delete from repository
        var result = await _repository.DeleteJobAsync(jobId);
        
        _logger.LogInformation("Ingestion job {JobId} deleted successfully: {Result}", jobId, result);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IngestionJobDefinition?> GetJobAsync(string jobId)
    {
        return await _repository.GetJobByIdAsync(jobId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IngestionJobDefinition>> GetJobsForTenantAsync(Guid tenantId)
    {
        return await _repository.GetJobsByTenantIdAsync(tenantId);
    }

    /// <inheritdoc/>
    public async Task<bool> PauseJobAsync(string jobId)
    {
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to pause non-existent job: {JobId}", jobId);
            return false;
        }
        
        // Update status and paused flag
        job.IsPaused = true;
        job.Status = IngestionStatus.Paused;
        
        // Update repository
        var result = await _repository.UpdateJobAsync(job);
        
        // Send notification
        await _notificationService.SendNotificationAsync(job, IngestionStatus.Paused);
        
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> ResumeJobAsync(string jobId)
    {
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to resume non-existent job: {JobId}", jobId);
            return false;
        }
        
        // Make sure the job is actually paused
        if (!job.IsPaused)
        {
            _logger.LogWarning("Attempted to resume a job that is not paused: {JobId}", jobId);
            return false;
        }
        
        // Update status and paused flag
        job.IsPaused = false;
        job.Status = IngestionStatus.Scheduled;
        
        // Update repository
        var result = await _repository.UpdateJobAsync(job);
        
        // If job has a cron expression, check if we need to reschedule it
        if (!string.IsNullOrEmpty(job.CronExpression))
        {
            ScheduleRecurringJob(job);
        }
        
        return result;
    }

    /// <inheritdoc/>
    public async Task<string> ScheduleJobAsync(IngestionJobDefinition jobDefinition)
    {
        _logger.LogInformation("Scheduling ingestion job: {JobName} for data source {DataSourceId}", 
            jobDefinition.Name, jobDefinition.DataSourceId);
        
        // Assign a new ID if not set
        if (string.IsNullOrEmpty(jobDefinition.Id))
        {
            jobDefinition.Id = Guid.NewGuid().ToString();
        }
        
        // Store job in repository
        var jobId = await _repository.CreateJobAsync(jobDefinition);
        
        // Schedule with Hangfire if a cron expression is provided
        if (!string.IsNullOrEmpty(jobDefinition.CronExpression) && !jobDefinition.IsPaused)
        {
            ScheduleRecurringJob(jobDefinition);
        }
        
        _logger.LogInformation("Ingestion job scheduled with ID: {JobId}", jobId);
        return jobId;
    }

    /// <inheritdoc/>
    public async Task<bool> TriggerJobNowAsync(string jobId)
    {
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Attempted to trigger non-existent job: {JobId}", jobId);
            return false;
        }
        
        // Check if job is paused
        if (job.IsPaused)
        {
            _logger.LogWarning("Cannot trigger paused job: {JobId}", jobId);
            return false;
        }
        
        _logger.LogInformation("Triggering immediate execution of job {JobId}", jobId);
        
        // Queue background job for immediate execution
        _backgroundJobClient.Enqueue(() => ExecuteJobAsync(jobId, CancellationToken.None));
        
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateJobAsync(IngestionJobDefinition jobDefinition)
    {
        var existingJob = await _repository.GetJobByIdAsync(jobDefinition.Id);
        if (existingJob == null)
        {
            _logger.LogWarning("Attempted to update non-existent job: {JobId}", jobDefinition.Id);
            return false;
        }
        
        // Update repository
        var result = await _repository.UpdateJobAsync(jobDefinition);
        
        // Update Hangfire schedule if cron expression changed or pause state changed
        if (result && !string.IsNullOrEmpty(jobDefinition.CronExpression) && 
            (existingJob.CronExpression != jobDefinition.CronExpression || 
             existingJob.IsPaused != jobDefinition.IsPaused))
        {
            if (jobDefinition.IsPaused)
            {
                _recurringJobManager.RemoveIfExists(jobDefinition.Id);
            }
            else
            {
                ScheduleRecurringJob(jobDefinition);
            }
        }
        
        return result;
    }
    
    private void ScheduleRecurringJob(IngestionJobDefinition job)
    {
        if (string.IsNullOrEmpty(job.CronExpression) || job.IsPaused)
        {
            return;
        }
        
        _logger.LogInformation("Scheduling recurring job {JobId} with cron: {Cron}", job.Id, job.CronExpression);
        
        // Note: We use the "America/New_York" timezone as an example. In a production system,
        // you might want to make this configurable or use UTC.
        _recurringJobManager.AddOrUpdate(
            job.Id, 
            () => ExecuteJobAsync(job.Id, CancellationToken.None),
            job.CronExpression,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") }
        );
    }
    
    /// <summary>
    /// Executes a specific job by ID
    /// </summary>
    /// <param name="jobId">The ID of the job to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 0)] // Let our retry mechanism handle retries
    public async Task ExecuteJobAsync(string jobId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting execution of job {JobId}", jobId);
        
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found during execution", jobId);
            return;
        }
        
        if (job.IsPaused)
        {
            _logger.LogWarning("Cannot execute paused job: {JobId}", jobId);
            return;
        }
        
        // Update job status to Running
        await _repository.UpdateJobStatusAsync(jobId, IngestionStatus.Running);
        
        // Record execution time
        var executionTime = DateTime.UtcNow;
        await _repository.UpdateJobExecutionTimeAsync(jobId, executionTime);
        
        try
        {
            // Get the data source connector factory
            using var scope = _serviceProvider.CreateScope();
            var connectorFactory = scope.ServiceProvider.GetRequiredService<IDataSourceConnectorFactory>();
            
            // Get the data source
            var dataSourceRepository = scope.ServiceProvider.GetRequiredService<IRepository<Core.Entities.DataSource>>();
            var dataSource = await dataSourceRepository.GetByIdAsync(job.DataSourceId);
            
            if (dataSource == null)
            {
                throw new InvalidOperationException($"Data source not found: {job.DataSourceId}");
            }
            
            // Get the connector type as string
            string connectorType = dataSource.Type == DataSourceType.Custom 
                ? dataSource.CustomConnectorType ?? "unknown" 
                : dataSource.Type.ToString();
            
            // Create connector
            var connector = connectorFactory.GetConnector(connectorType);
            if (connector == null)
            {
                throw new InvalidOperationException($"Connector not found for data source type: {connectorType}");
            }
            
            // Parse the extraction parameters from JSON
            var extractionParams = !string.IsNullOrEmpty(job.ExtractionParametersJson) 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(job.ExtractionParametersJson) 
                : new Dictionary<string, string>();
            
            // Parse connection parameters from data source
            var connectionParams = !string.IsNullOrEmpty(dataSource.ConnectionParameters)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(dataSource.ConnectionParameters)
                : new Dictionary<string, string>();
            
            // Add basic connection parameters if not present in the JSON
            if (!string.IsNullOrEmpty(dataSource.ConnectionString) && !connectionParams.ContainsKey("connectionString"))
                connectionParams["connectionString"] = dataSource.ConnectionString;
            
            if (!string.IsNullOrEmpty(dataSource.Username) && !connectionParams.ContainsKey("username"))
                connectionParams["username"] = dataSource.Username;
            
            // Create connector configuration using the factory
            var connectorConfig = ConnectorConfigurationFactory.Create(
                connectorId: connectorType,
                name: dataSource.Name,
                tenantId: job.TenantId,
                connectionParameters: connectionParams);
            
            // Initialize with proper connector configuration
            await connector.InitializeAsync(connectorConfig, cancellationToken);
            
            // Validate connection with proper parameters
            var validationResult = await connector.ValidateConnectionAsync(connectionParams);
            if (!validationResult.IsValid)
            {
                string errorMsg = validationResult.Errors.Any() 
                    ? string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
                    : "Connection validation failed";
                throw new InvalidOperationException($"Failed to validate connection to data source: {errorMsg}");
            }
            
            // Connect with proper parameters
            var connectionResult = await connector.ConnectAsync(connectionParams, cancellationToken);
            if (!connectionResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to connect to data source: {connectionResult.ErrorMessage}");
            }
            
            // Convert string params to object params for extraction
            var extractionFilterParams = new Dictionary<string, object>();
            if (extractionParams != null)
            {
                foreach (var param in extractionParams)
                {
                    extractionFilterParams[param.Key] = param.Value;
                }
            }
            
            // Create extraction parameters with default target (we'll extract all available structures)
            var extractionParameters = new ExtractionParameters(
                targetStructures: new[] { "*" },  // Default to all structures
                filterCriteria: extractionFilterParams
            );
            
            var extractionResult = await connector.ExtractDataAsync(extractionParameters, cancellationToken);
            
            if (!extractionResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to extract data: {extractionResult.ErrorMessage}");
            }
            
            var data = extractionResult.Data;
            
            if (data == null)
            {
                throw new InvalidOperationException("No data extracted from source");
            }
            
            // Process the extracted data
            // In a real implementation, you would process the data here,
            // for example, by storing it in a database or processing it with AI
            _logger.LogInformation("Successfully extracted {Count} items from data source {DataSourceId}", 
                data.Count, job.DataSourceId);
            
            // Disconnect from the data source
            await connector.DisconnectAsync(cancellationToken);
            
            // Update job status to Completed
            await _repository.UpdateJobStatusAsync(jobId, IngestionStatus.Completed, 
                $"Successfully extracted {data.Count} items");
            
            // Reset failure count on success
            await _repository.ResetFailureCountAsync(jobId);
            
            // Send notification on completion if configured
            await _notificationService.SendNotificationAsync(job, IngestionStatus.Completed, 
                $"Successfully extracted {data.Count} items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobId}: {Message}", jobId, ex.Message);
            
            // Increment failure count
            var failureCount = await _repository.IncrementFailureCountAsync(jobId);
            
            // Update job status to Failed
            await _repository.UpdateJobStatusAsync(jobId, IngestionStatus.Failed, ex.Message);
            
            // Send failure notification
            await _notificationService.SendNotificationAsync(job, IngestionStatus.Failed, ex.Message);
            
            // Check if max retries reached
            if (failureCount >= job.MaxRetryCount)
            {
                _logger.LogWarning("Job {JobId} reached maximum retry count of {MaxRetries}. Auto-pausing job.", 
                    jobId, job.MaxRetryCount);
                
                // Auto-pause job when max retries reached
                job.IsPaused = true;
                await _repository.UpdateJobAsync(job);
                
                // Remove from recurring jobs if it has a schedule
                if (!string.IsNullOrEmpty(job.CronExpression))
                {
                    _recurringJobManager.RemoveIfExists(jobId);
                }
                
                await _notificationService.SendNotificationAsync(job, IngestionStatus.Paused, 
                    $"Job auto-paused after reaching maximum retry count of {job.MaxRetryCount}");
            }
        }
    }
} 