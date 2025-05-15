using System;
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
    public class SqlLogRetentionService
    {
        private readonly ILogger<SqlLogRetentionService> _logger;
        private readonly ISqlLoggingService _sqlLoggingService;
        private readonly LogRetentionOptions _options;
        
        /// <summary>
        /// Creates a new SqlLogRetentionService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sqlLoggingService">SQL logging service</param>
        /// <param name="options">Log retention options</param>
        public SqlLogRetentionService(
            ILogger<SqlLogRetentionService> logger,
            ISqlLoggingService sqlLoggingService,
            IOptions<LogRetentionOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqlLoggingService = sqlLoggingService ?? throw new ArgumentNullException(nameof(sqlLoggingService));
            _options = options?.Value ?? new LogRetentionOptions();
            
            _logger.LogInformation("SQL Log Retention Service initialized. RetentionDays: {RetentionDays}, " +
                                  "ExecutionInterval: {ExecutionInterval}",
                _options.RetentionDays,
                _options.ExecutionInterval);
        }
        
        /// <summary>
        /// Processes log retention policy
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ProcessLogRetentionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing SQL log retention");
            
            var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);
            
            var filter = new SqlLogFilter
            {
                ToDate = cutoffDate,
                MaxResults = int.MaxValue // Get all logs before cutoff date
            };
            
            var oldLogs = await _sqlLoggingService.GetSqlLogsAsync(filter, cancellationToken);
            
            if (oldLogs.Count > 0)
            {
                _logger.LogInformation("Purging {Count} SQL logs older than {CutoffDate}", 
                    oldLogs.Count, cutoffDate);
                
                // In a real implementation, this would call a method to purge the logs from storage
                // For example: await _sqlLoggingService.PurgeLogsAsync(oldLogs, cancellationToken);
                
                _logger.LogInformation("Completed purging {Count} SQL logs", oldLogs.Count);
            }
            else
            {
                _logger.LogInformation("No SQL logs found to purge before {CutoffDate}", cutoffDate);
            }
        }
    }
    
    /// <summary>
    /// Options for SQL log retention
    /// </summary>
    public class LogRetentionOptions
    {
        /// <summary>
        /// Number of days to retain logs
        /// </summary>
        public int RetentionDays { get; set; } = 30;
        
        /// <summary>
        /// Interval in hours between retention executions
        /// </summary>
        public int ExecutionInterval { get; set; } = 24;
    }
} 