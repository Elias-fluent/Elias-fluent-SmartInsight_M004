using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for SQL-specific logging
    /// </summary>
    public interface ISqlLoggingService
    {
        /// <summary>
        /// Logs SQL template selection
        /// </summary>
        /// <param name="query">The original natural language query</param>
        /// <param name="selectionResult">The template selection result</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogTemplateSelectionAsync(
            string query, 
            TemplateSelectionResult selectionResult, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs SQL generation
        /// </summary>
        /// <param name="query">The original natural language query</param>
        /// <param name="generationResult">The SQL generation result</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogSqlGenerationAsync(
            string query, 
            SqlGenerationResult generationResult, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs SQL execution
        /// </summary>
        /// <param name="executionResult">The SQL execution result</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogSqlExecutionAsync(
            SqlExecutionResult executionResult, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs a SQL validation issue
        /// </summary>
        /// <param name="query">The original natural language query</param>
        /// <param name="sql">The SQL being validated</param>
        /// <param name="validationResult">The validation result</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogValidationIssueAsync(
            string query, 
            string sql, 
            SqlValidationResult validationResult, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs a SQL error
        /// </summary>
        /// <param name="query">The original natural language query</param>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogSqlErrorAsync(
            string query, 
            Exception exception, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets SQL log entries
        /// </summary>
        /// <param name="filter">Optional filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of log entries</returns>
        Task<List<SqlLogEntry>> GetSqlLogsAsync(
            SqlLogFilter? filter = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets SQL log statistics
        /// </summary>
        /// <param name="fromDate">Start date for statistics</param>
        /// <param name="toDate">End date for statistics</param>
        /// <param name="tenantId">Optional tenant ID filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>SQL log statistics</returns>
        Task<SqlLogStatistics> GetSqlStatisticsAsync(
            DateTime fromDate, 
            DateTime toDate, 
            Guid? tenantId = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets query statistics for a specific time range
        /// </summary>
        /// <param name="startDate">Start date for the range</param>
        /// <param name="endDate">End date for the range</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query statistics</returns>
        Task<SqlLogStatistics> GetQueryStatisticsAsync(
            DateTime startDate,
            DateTime endDate,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the most recent queries
        /// </summary>
        /// <param name="limit">Maximum number of queries to return</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of recent queries</returns>
        Task<List<SqlLogEntry>> GetRecentQueriesAsync(
            int limit,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the most popular queries
        /// </summary>
        /// <param name="limit">Maximum number of queries to return</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of popular queries</returns>
        Task<List<SqlLogEntry>> GetPopularQueriesAsync(
            int limit,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets queries that take longer than a specified threshold
        /// </summary>
        /// <param name="executionTimeThresholdMs">Execution time threshold in milliseconds</param>
        /// <param name="limit">Maximum number of queries to return</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of slow queries</returns>
        Task<List<SqlLogEntry>> GetSlowQueriesAsync(
            long executionTimeThresholdMs,
            int limit,
            Guid? tenantId = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Filter criteria for SQL logs
    /// </summary>
    public class SqlLogFilter
    {
        /// <summary>
        /// Filter by specific tenant ID
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Filter by specific user ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Filter by operation type
        /// </summary>
        public SqlOperationType? OperationType { get; set; }

        /// <summary>
        /// Filter by success status
        /// </summary>
        public bool? IsSuccessful { get; set; }

        /// <summary>
        /// Filter by date range - start
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter by date range - end
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by template ID
        /// </summary>
        public string? TemplateId { get; set; }

        /// <summary>
        /// Filter by text in the original query (partial match)
        /// </summary>
        public string? QueryContains { get; set; }

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; } = 100;
    }
} 