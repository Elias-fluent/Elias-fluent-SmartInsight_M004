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
        /// <param name="query">The original natural language query</param>
        /// <param name="executionResult">The SQL execution result</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogSqlExecutionAsync(
            string query, 
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
        /// <param name="sql">The SQL that caused the error</param>
        /// <param name="error">The error message</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Log entry ID</returns>
        Task<Guid> LogSqlErrorAsync(
            string query, 
            string sql, 
            string error, 
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

    /// <summary>
    /// SQL log statistics
    /// </summary>
    public class SqlLogStatistics
    {
        /// <summary>
        /// Total number of queries
        /// </summary>
        public int TotalQueries { get; set; }

        /// <summary>
        /// Number of successful queries
        /// </summary>
        public int SuccessfulQueries { get; set; }

        /// <summary>
        /// Number of failed queries
        /// </summary>
        public int FailedQueries { get; set; }

        /// <summary>
        /// Average execution time in milliseconds
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Counts by operation type
        /// </summary>
        public Dictionary<SqlOperationType, int> OperationTypeCounts { get; set; } = new Dictionary<SqlOperationType, int>();

        /// <summary>
        /// Most used templates
        /// </summary>
        public Dictionary<string, int> TopTemplates { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Most common validation issues
        /// </summary>
        public List<string> CommonValidationIssues { get; set; } = new List<string>();

        /// <summary>
        /// Most common errors
        /// </summary>
        public List<string> CommonErrors { get; set; } = new List<string>();
    }
} 