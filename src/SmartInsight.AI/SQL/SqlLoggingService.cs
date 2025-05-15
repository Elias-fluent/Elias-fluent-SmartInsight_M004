using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Implementation of SQL-specific logging service
    /// </summary>
    public class SqlLoggingService : ISqlLoggingService
    {
        private readonly ILogger<SqlLoggingService> _logger;
        private readonly List<SqlLogEntry> _logEntries;
        private readonly object _lock = new object();
        private readonly int _maxLogEntries;
        private readonly bool _enableQueryOriginTracking;
        private readonly bool _enablePerfMetricsLogging;
        private readonly bool _enableSecurityEventLogging;
        
        /// <summary>
        /// Creates a new SqlLoggingService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="maxLogEntries">Maximum number of in-memory log entries to retain</param>
        /// <param name="enableQueryOriginTracking">Whether to track query origins</param>
        /// <param name="enablePerfMetricsLogging">Whether to log performance metrics</param>
        /// <param name="enableSecurityEventLogging">Whether to log security events</param>
        public SqlLoggingService(
            ILogger<SqlLoggingService> logger,
            int maxLogEntries = 10000,
            bool enableQueryOriginTracking = true,
            bool enablePerfMetricsLogging = true,
            bool enableSecurityEventLogging = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logEntries = new List<SqlLogEntry>();
            _maxLogEntries = Math.Max(100, maxLogEntries); // Minimum 100 entries
            _enableQueryOriginTracking = enableQueryOriginTracking;
            _enablePerfMetricsLogging = enablePerfMetricsLogging;
            _enableSecurityEventLogging = enableSecurityEventLogging;
            
            _logger.LogInformation("SQL Logging Service initialized. MaxLogEntries: {MaxLogEntries}, " +
                                  "QueryOriginTracking: {QueryOriginTracking}, " + 
                                  "PerfMetricsLogging: {PerfMetricsLogging}, " +
                                  "SecurityEventLogging: {SecurityEventLogging}",
                _maxLogEntries,
                _enableQueryOriginTracking,
                _enablePerfMetricsLogging,
                _enableSecurityEventLogging);
        }
        
        /// <inheritdoc />
        public Task<Guid> LogTemplateSelectionAsync(
            string query,
            TemplateSelectionResult selectionResult,
            TenantContext? tenantContext = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Attempted to log template selection with empty query");
                return Task.FromResult(Guid.Empty);
            }
            
            var logEntry = new SqlLogEntry
            {
                OperationType = SqlOperationType.Other,
                OriginalQuery = query,
                IsSuccessful = selectionResult.IsSuccessful,
                ErrorMessage = selectionResult.ErrorMessage,
                TemplateId = selectionResult.SelectedTemplate?.Id
            };
            
            if (tenantContext != null)
            {
                logEntry.TenantId = tenantContext.TenantId;
                logEntry.UserId = tenantContext.UserId;
            }
            
            AddLogEntry(logEntry);
            
            // Enhanced logging
            if (_enableQueryOriginTracking && !string.IsNullOrEmpty(selectionResult.QueryContext))
            {
                _logger.LogInformation("Template selection query context: {QueryContext}", selectionResult.QueryContext);
            }
            
            if (_enableSecurityEventLogging && !selectionResult.IsSuccessful)
            {
                _logger.LogWarning("Template selection failed. Query: {Query}, Error: {Error}", 
                    query, selectionResult.ErrorMessage);
            }
            
            return Task.FromResult(logEntry.Id);
        }
        
        /// <inheritdoc />
        public Task<Guid> LogSqlGenerationAsync(
            string query,
            SqlGenerationResult generationResult,
            TenantContext? tenantContext = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Attempted to log SQL generation with empty query");
                return Task.FromResult(Guid.Empty);
            }
            
            var operationType = DetermineOperationType(generationResult.Sql);
            
            var logEntry = new SqlLogEntry
            {
                OperationType = operationType,
                OriginalQuery = query,
                GeneratedSql = generationResult.Sql,
                IsSuccessful = generationResult.IsSuccessful,
                ErrorMessage = generationResult.ErrorMessage,
                TemplateId = generationResult.TemplateId
            };
            
            if (tenantContext != null)
            {
                logEntry.TenantId = tenantContext.TenantId;
                logEntry.UserId = tenantContext.UserId;
            }
            
            AddLogEntry(logEntry);
            
            // Enhanced security logging
            if (_enableSecurityEventLogging && !generationResult.IsSuccessful)
            {
                _logger.LogWarning("SQL generation failed. Query: {Query}, Error: {Error}", 
                    query, generationResult.ErrorMessage);
            }
            else if (generationResult.IsSuccessful)
            {
                // Log more details for successful generations
                _logger.LogInformation("SQL generated successfully. OperationType: {OperationType}, " +
                                      "TemplateId: {TemplateId}, ParameterCount: {ParameterCount}",
                    operationType,
                    generationResult.TemplateId,
                    generationResult.Parameters?.Count ?? 0);
            }
            
            return Task.FromResult(logEntry.Id);
        }
        
        /// <inheritdoc />
        public Task<Guid> LogSqlExecutionAsync(
            SqlExecutionResult executionResult, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default)
        {
            if (executionResult == null)
            {
                _logger.LogWarning("Attempted to log SQL execution with null execution result");
                return Task.FromResult(Guid.Empty);
            }
            
            var operationType = DetermineOperationType(executionResult.ExecutedQuery);
            
            var logEntry = new SqlLogEntry
            {
                OperationType = operationType,
                OriginalQuery = executionResult.SqlGenerated,
                GeneratedSql = executionResult.ExecutedQuery,
                IsSuccessful = executionResult.IsSuccessful,
                ErrorMessage = executionResult.ErrorMessage,
                ExecutionTimeMs = executionResult.ExecutionTimeMs,
                RowsAffected = executionResult.RowsAffected
            };
            
            if (tenantContext != null)
            {
                logEntry.TenantId = tenantContext.TenantId;
                logEntry.UserId = tenantContext.UserId;
            }
            
            AddLogEntry(logEntry);
            
            // Enhanced performance metrics logging
            if (_enablePerfMetricsLogging)
            {
                var performanceCategory = CategorizePerformance(executionResult.ExecutionTimeMs);
                _logger.LogInformation("SQL execution performance: {PerformanceCategory}, " +
                                      "Time: {ExecutionTimeMs}ms, Rows: {RowsAffected}, " +
                                      "Operation: {OperationType}",
                    performanceCategory,
                    executionResult.ExecutionTimeMs,
                    executionResult.RowsAffected,
                    operationType);
                
                // Log slow queries with more detail
                if (performanceCategory == "Slow" || performanceCategory == "VerySlow")
                {
                    _logger.LogWarning("Slow SQL execution detected. Time: {ExecutionTimeMs}ms, " +
                                      "SQL: {ExecutedQuery}",
                        executionResult.ExecutionTimeMs,
                        executionResult.ExecutedQuery);
                }
            }
            
            // Enhanced security event logging
            if (_enableSecurityEventLogging && !executionResult.IsSuccessful)
            {
                _logger.LogWarning("SQL execution failed. Error: {Error}", executionResult.ErrorMessage);
            }
            
            return Task.FromResult(logEntry.Id);
        }
        
        /// <inheritdoc />
        public Task<Guid> LogValidationIssueAsync(
            string query,
            string sql,
            SqlValidationResult validationResult,
            TenantContext? tenantContext = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sql))
            {
                _logger.LogWarning("Attempted to log validation issue with empty SQL");
                return Task.FromResult(Guid.Empty);
            }
            
            var operationType = DetermineOperationType(sql);
            var securityIssues = validationResult.Issues
                .Where(i => i.Category == ValidationCategory.Security)
                .ToList();
            
            var logEntry = new SqlLogEntry
            {
                OperationType = operationType,
                OriginalQuery = query,
                GeneratedSql = sql,
                IsSuccessful = false,
                ErrorMessage = string.Join("; ", validationResult.Issues.Select(i => i.Description))
            };
            
            if (tenantContext != null)
            {
                logEntry.TenantId = tenantContext.TenantId;
                logEntry.UserId = tenantContext.UserId;
            }
            
            AddLogEntry(logEntry);
            
            // Enhanced security event logging
            if (_enableSecurityEventLogging && securityIssues.Any())
            {
                foreach (var issue in securityIssues)
                {
                    _logger.LogWarning("SQL security validation issue: {Description}, Severity: {Severity}", 
                        issue.Description, issue.Severity);
                }
                
                // For critical security issues, log at error level
                if (securityIssues.Any(i => i.Severity == ValidationSeverity.Error))
                {
                    _logger.LogError("Critical SQL security issues detected in query: {Query}", query);
                }
            }
            
            // Log performance issues separately
            var perfIssues = validationResult.Issues
                .Where(i => i.Category == ValidationCategory.Performance)
                .ToList();
            
            if (_enablePerfMetricsLogging && perfIssues.Any())
            {
                foreach (var issue in perfIssues)
                {
                    _logger.LogInformation("SQL performance issue: {Description}, Recommendation: {Recommendation}", 
                        issue.Description, issue.Recommendation);
                }
            }
            
            return Task.FromResult(logEntry.Id);
        }
        
        /// <inheritdoc />
        public Task<Guid> LogSqlErrorAsync(
            string query,
            Exception exception,
            TenantContext? tenantContext = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Attempted to log SQL error with empty query");
                return Task.FromResult(Guid.Empty);
            }

            if (exception == null)
            {
                _logger.LogWarning("Attempted to log SQL error with null exception");
                return Task.FromResult(Guid.Empty);
            }
            
            var logEntry = new SqlLogEntry
            {
                OperationType = SqlOperationType.Unknown, // Can't determine from exception
                OriginalQuery = query,
                IsSuccessful = false,
                ErrorMessage = exception.Message
            };
            
            if (tenantContext != null)
            {
                logEntry.TenantId = tenantContext.TenantId;
                logEntry.UserId = tenantContext.UserId;
            }
            
            AddLogEntry(logEntry);
            
            // Enhanced security logging
            if (_enableSecurityEventLogging)
            {
                _logger.LogWarning(exception, "SQL error occurred. Query: {Query}", query);
            }
            
            return Task.FromResult(logEntry.Id);
        }
        
        /// <inheritdoc />
        public Task<List<SqlLogEntry>> GetSqlLogsAsync(
            SqlLogFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            List<SqlLogEntry> filteredLogs;
            
            lock (_lock)
            {
                filteredLogs = _logEntries.AsQueryable()
                    .Where(l => filter == null || 
                               ((!filter.TenantId.HasValue || l.TenantId == filter.TenantId) &&
                                (!filter.UserId.HasValue || l.UserId == filter.UserId) &&
                                (!filter.OperationType.HasValue || l.OperationType == filter.OperationType) &&
                                (!filter.IsSuccessful.HasValue || l.IsSuccessful == filter.IsSuccessful) &&
                                (!filter.FromDate.HasValue || l.Timestamp >= filter.FromDate) &&
                                (!filter.ToDate.HasValue || l.Timestamp <= filter.ToDate) &&
                                (string.IsNullOrEmpty(filter.TemplateId) || l.TemplateId == filter.TemplateId) &&
                                (string.IsNullOrEmpty(filter.QueryContains) || l.OriginalQuery.Contains(filter.QueryContains, StringComparison.OrdinalIgnoreCase))))
                    .OrderByDescending(l => l.Timestamp)
                    .Take(filter?.MaxResults ?? 100)
                    .ToList();
            }
            
            return Task.FromResult(filteredLogs);
        }
        
        /// <inheritdoc />
        public Task<SqlLogStatistics> GetSqlStatisticsAsync(
            DateTime fromDate,
            DateTime toDate,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default)
        {
            if (toDate < fromDate)
            {
                _logger.LogWarning("Invalid date range for SQL statistics: {FromDate} to {ToDate}", fromDate, toDate);
                toDate = fromDate.AddDays(1);
            }
            
            var statistics = new SqlLogStatistics();
            
            lock (_lock)
            {
                var relevantLogs = _logEntries
                    .Where(l => l.Timestamp >= fromDate && 
                               l.Timestamp <= toDate &&
                               (!tenantId.HasValue || l.TenantId == tenantId))
                    .ToList();
                
                statistics.TotalQueries = relevantLogs.Count;
                statistics.SuccessfulQueries = relevantLogs.Count(l => l.IsSuccessful);
                statistics.FailedQueries = relevantLogs.Count(l => !l.IsSuccessful);
                
                // Calculate additional statistics
                if (relevantLogs.Any())
                {
                    var execTimes = relevantLogs
                        .Where(l => l.ExecutionTimeMs.HasValue)
                        .Select(l => l.ExecutionTimeMs!.Value) // Using null-forgiving operator since we filter non-nulls
                        .ToList();
                    
                    if (execTimes.Any())
                    {
                        statistics.AverageExecutionTimeMs = execTimes.Average();
                        statistics.MinExecutionTimeMs = execTimes.Min();
                        statistics.MaxExecutionTimeMs = execTimes.Max();
                    }
                    
                    statistics.OperationCounts = relevantLogs
                        .GroupBy(l => l.OperationType)
                        .ToDictionary(g => g.Key, g => g.Count());
                    
                    // Filter out null error messages and use non-null values for dictionary keys
                    statistics.ErrorCounts = relevantLogs
                        .Where(l => !l.IsSuccessful && !string.IsNullOrEmpty(l.ErrorMessage))
                        .GroupBy(l => l.ErrorMessage!)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
            }
            
            return Task.FromResult(statistics);
        }
        
        /// <inheritdoc />
        public Task<SqlLogStatistics> GetQueryStatisticsAsync(
            DateTime startDate,
            DateTime endDate,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default)
        {
            // Filter logs by date range and tenant
            List<SqlLogEntry> filteredLogs;
            lock (_lock)
            {
                filteredLogs = _logEntries
                    .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .Where(l => !tenantId.HasValue || l.TenantId == tenantId)
                    .ToList();
            }
            
            if (filteredLogs.Count == 0)
            {
                return Task.FromResult(new SqlLogStatistics
                {
                    TotalQueries = 0,
                    SuccessfulQueries = 0,
                    FailedQueries = 0
                });
            }
            
            // Calculate statistics
            var statistics = new SqlLogStatistics
            {
                TotalQueries = filteredLogs.Count,
                SuccessfulQueries = filteredLogs.Count(l => l.IsSuccessful),
                FailedQueries = filteredLogs.Count(l => !l.IsSuccessful)
            };
            
            // Calculate execution time statistics
            var executionTimes = filteredLogs
                .Where(l => l.ExecutionTimeMs.HasValue)
                .Select(l => l.ExecutionTimeMs!.Value)  // Using null-forgiving operator since we filter non-nulls
                .ToList();
                
            if (executionTimes.Any())
            {
                statistics.AverageExecutionTimeMs = executionTimes.Average();
                statistics.MinExecutionTimeMs = executionTimes.Min();
                statistics.MaxExecutionTimeMs = executionTimes.Max();
            }
            
            // Calculate operation type counts
            statistics.OperationCounts = filteredLogs
                .GroupBy(l => l.OperationType)
                .ToDictionary(g => g.Key, g => g.Count());
                
            // Calculate error counts
            statistics.ErrorCounts = filteredLogs
                .Where(l => !l.IsSuccessful && !string.IsNullOrEmpty(l.ErrorMessage))
                .GroupBy(l => l.ErrorMessage ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
                
            return Task.FromResult(statistics);
        }

        /// <inheritdoc />
        public Task<List<SqlLogEntry>> GetRecentQueriesAsync(
            int limit,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default)
        {
            limit = Math.Max(1, Math.Min(limit, 100)); // Ensure limit is between 1 and 100
            
            List<SqlLogEntry> result;
            lock (_lock)
            {
                result = _logEntries
                    .Where(l => !tenantId.HasValue || l.TenantId == tenantId)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToList();
            }
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<List<SqlLogEntry>> GetPopularQueriesAsync(
            int limit,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default)
        {
            limit = Math.Max(1, Math.Min(limit, 100)); // Ensure limit is between 1 and 100
            
            List<SqlLogEntry> result;
            lock (_lock)
            {
                // Group by original query, count occurrences, then take top N
                result = _logEntries
                    .Where(l => !tenantId.HasValue || l.TenantId == tenantId)
                    .GroupBy(l => l.OriginalQuery)
                    .Select(g => new 
                    { 
                        QueryGroup = g, 
                        Count = g.Count() 
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(limit)
                    .Select(x => x.QueryGroup.First()) // Take the first log entry from each group
                    .ToList();
            }
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<List<SqlLogEntry>> GetSlowQueriesAsync(
            long executionTimeThresholdMs,
            int limit,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default)
        {
            limit = Math.Max(1, Math.Min(limit, 100)); // Ensure limit is between 1 and 100
            
            List<SqlLogEntry> result;
            lock (_lock)
            {
                result = _logEntries
                    .Where(l => l.ExecutionTimeMs.HasValue && l.ExecutionTimeMs.Value >= executionTimeThresholdMs)
                    .Where(l => !tenantId.HasValue || l.TenantId == tenantId)
                    .OrderByDescending(l => l.ExecutionTimeMs)
                    .Take(limit)
                    .ToList();
            }
            
            return Task.FromResult(result);
        }
        
        private void AddLogEntry(SqlLogEntry entry)
        {
            lock (_lock)
            {
                _logEntries.Add(entry);
                
                // Implement log rotation - remove oldest entries when limit is reached
                if (_logEntries.Count > _maxLogEntries)
                {
                    int removeCount = _logEntries.Count - _maxLogEntries;
                    _logEntries.RemoveRange(0, removeCount);
                    _logger.LogInformation("SQL log rotation: removed {RemoveCount} oldest log entries", removeCount);
                }
            }
        }
        
        private SqlOperationType DetermineOperationType(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return SqlOperationType.Other;
            
            sql = sql.TrimStart();
            
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Select;
            if (sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Insert;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Update;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Delete;
            
            return SqlOperationType.Other;
        }
        
        private string CategorizePerformance(long executionTimeMs)
        {
            if (executionTimeMs < 10) return "VeryFast";
            if (executionTimeMs < 100) return "Fast";
            if (executionTimeMs < 500) return "Normal";
            if (executionTimeMs < 2000) return "Slow";
            return "VerySlow";
        }
    }
} 