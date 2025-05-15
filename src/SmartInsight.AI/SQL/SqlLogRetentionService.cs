using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Service for managing SQL log retention
    /// </summary>
    public class SqlLogRetentionService : ISqlLogRetentionService
    {
        private readonly ILogger<SqlLogRetentionService> _logger;
        private readonly ISqlLoggingService _sqlLoggingService;
        private readonly SqlLogRetentionOptions _options;
        
        /// <summary>
        /// Creates a new SqlLogRetentionService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sqlLoggingService">SQL logging service</param>
        /// <param name="options">Log retention options</param>
        public SqlLogRetentionService(
            ILogger<SqlLogRetentionService> logger,
            ISqlLoggingService sqlLoggingService,
            IOptions<SqlLogRetentionOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqlLoggingService = sqlLoggingService ?? throw new ArgumentNullException(nameof(sqlLoggingService));
            _options = options?.Value ?? new SqlLogRetentionOptions();
            
            _logger.LogInformation("SQL Log Retention Service initialized. DefaultRetentionDays: {DefaultRetentionDays}, " +
                                  "ErrorLogRetentionDays: {ErrorLogRetentionDays}, " +
                                  "MaxLogsToDeletePerRun: {MaxLogsToDeletePerRun}",
                _options.DefaultRetentionDays,
                _options.ErrorLogRetentionDays,
                _options.MaxLogsToDeletePerRun);
        }
        
        /// <inheritdoc />
        public async Task<LogCleanupResult> CleanupLogsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing SQL log retention");
            
            var result = new LogCleanupResult
            {
                IsSuccessful = true,
                LogsDeleted = 0,
                ExecutionTimeMs = 0,
                LogsDeletedByType = new Dictionary<LogType, long>()
            };
            
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would delete logs from a database
                // Here we'll just simulate log cleanup
                
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.DefaultRetentionDays);
                
                // Get the logs for statistics purposes (in real app this would use a more efficient query)
                var filter = new SqlLogFilter
                {
                    ToDate = cutoffDate,
                    MaxResults = _options.MaxLogsToDeletePerRun 
                };
                
                var oldLogs = await _sqlLoggingService.GetSqlLogsAsync(filter, cancellationToken);
                
                if (oldLogs.Count > 0)
                {
                    _logger.LogInformation("Simulating purge of {Count} SQL logs older than {CutoffDate}", 
                        oldLogs.Count, cutoffDate);
                    
                    // Track logs deleted by type
                    var logsByType = oldLogs
                        .GroupBy(l => MapOperationTypeToLogType(l.OperationType))
                        .ToDictionary(g => g.Key, g => (long)g.Count());
                    
                    foreach (var kvp in logsByType)
                    {
                        result.LogsDeletedByType[kvp.Key] = kvp.Value;
                    }
                    
                    result.LogsDeleted = oldLogs.Count;
                    result.OldestLogAfterCleanup = cutoffDate;
                }
                else
                {
                    _logger.LogInformation("No SQL logs found to purge before {CutoffDate}", cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up logs");
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
            }
            
            var endTime = DateTime.UtcNow;
            result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            
            return result;
        }
        
        /// <inheritdoc />
        public async Task<LogCleanupResult> CleanupLogsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing SQL log retention for tenant {TenantId}", tenantId);
            
            var result = new LogCleanupResult
            {
                IsSuccessful = true,
                LogsDeleted = 0,
                ExecutionTimeMs = 0,
                LogsDeletedByType = new Dictionary<LogType, long>()
            };
            
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would delete logs from a database for a specific tenant
                // Here we'll just simulate log cleanup
                
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.DefaultRetentionDays);
                
                // Get the logs for statistics purposes (in real app this would use a more efficient query)
                var filter = new SqlLogFilter
                {
                    ToDate = cutoffDate,
                    TenantId = tenantId,
                    MaxResults = _options.MaxLogsToDeletePerRun 
                };
                
                var oldLogs = await _sqlLoggingService.GetSqlLogsAsync(filter, cancellationToken);
                
                if (oldLogs.Count > 0)
                {
                    _logger.LogInformation("Simulating purge of {Count} SQL logs for tenant {TenantId} older than {CutoffDate}", 
                        oldLogs.Count, tenantId, cutoffDate);
                    
                    // Track logs deleted by type
                    var logsByType = oldLogs
                        .GroupBy(l => MapOperationTypeToLogType(l.OperationType))
                        .ToDictionary(g => g.Key, g => (long)g.Count());
                    
                    foreach (var kvp in logsByType)
                    {
                        result.LogsDeletedByType[kvp.Key] = kvp.Value;
                    }
                    
                    result.LogsDeleted = oldLogs.Count;
                    result.OldestLogAfterCleanup = cutoffDate;
                }
                else
                {
                    _logger.LogInformation("No SQL logs found to purge for tenant {TenantId} before {CutoffDate}", 
                        tenantId, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up logs for tenant {TenantId}", tenantId);
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
            }
            
            var endTime = DateTime.UtcNow;
            result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            
            return result;
        }
        
        /// <inheritdoc />
        public Task<long> GetLogCountAsync(CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would query the database
            // Here we'll just return a simulated count
            return Task.FromResult(1000L);
        }
        
        /// <inheritdoc />
        public Task<Dictionary<LogType, long>> GetLogCountByTypeAsync(CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would query the database
            // Here we'll just return a simulated count by type
            var result = new Dictionary<LogType, long>
            {
                { LogType.QueryGeneration, 250 },
                { LogType.QueryExecution, 500 },
                { LogType.TemplateSelection, 100 },
                { LogType.ParameterValidation, 50 },
                { LogType.Error, 20 },
                { LogType.Security, 30 },
                { LogType.Performance, 40 },
                { LogType.Audit, 10 }
            };
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<SqlLogRetentionOptions> GetRetentionSettingsAsync(CancellationToken cancellationToken = default)
        {
            // In a real implementation, this might load from configuration or database
            // Here we'll just return the current options
            return Task.FromResult(_options);
        }
        
        /// <inheritdoc />
        public Task UpdateRetentionSettingsAsync(SqlLogRetentionOptions settings, CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would update settings in database or configuration
            // Here we'll just update the in-memory options
            _options.DefaultRetentionDays = settings.DefaultRetentionDays;
            _options.ErrorLogRetentionDays = settings.ErrorLogRetentionDays;
            _options.PerformanceLogRetentionDays = settings.PerformanceLogRetentionDays;
            _options.SecurityLogRetentionDays = settings.SecurityLogRetentionDays;
            _options.AuditLogRetentionDays = settings.AuditLogRetentionDays;
            _options.QueryBatchSize = settings.QueryBatchSize;
            _options.MaxLogsToDeletePerRun = settings.MaxLogsToDeletePerRun;
            _options.CompressInsteadOfDelete = settings.CompressInsteadOfDelete;
            
            _logger.LogInformation("Updated retention settings. DefaultRetentionDays: {DefaultRetentionDays}, " +
                                  "ErrorLogRetentionDays: {ErrorLogRetentionDays}",
                _options.DefaultRetentionDays,
                _options.ErrorLogRetentionDays);
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Maps SQL operation type to log type
        /// </summary>
        private LogType MapOperationTypeToLogType(SqlOperationType operationType)
        {
            return operationType switch
            {
                SqlOperationType.Select => LogType.QueryExecution,
                SqlOperationType.Insert => LogType.QueryExecution,
                SqlOperationType.Update => LogType.QueryExecution,
                SqlOperationType.Delete => LogType.QueryExecution,
                _ => LogType.Audit
            };
        }
    }
} 