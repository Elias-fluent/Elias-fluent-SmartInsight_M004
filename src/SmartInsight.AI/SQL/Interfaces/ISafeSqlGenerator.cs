using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Main interface for the Safe SQL Generator system, serving as a facade for all SQL generation components
    /// </summary>
    public interface ISafeSqlGenerator
    {
        /// <summary>
        /// Generates and executes a SQL query from a natural language query
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL execution result</returns>
        Task<SqlExecutionResult> GenerateAndExecuteAsync(
            string query, 
            Guid? tenantId = null,
            Guid? userId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a SQL query from a natural language query without executing it
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL generation result</returns>
        Task<SqlGenerationResult> GenerateOnlyAsync(
            string query, 
            Guid? tenantId = null, 
            Guid? userId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates and executes a SQL query using intent detection results
        /// </summary>
        /// <param name="query">The original natural language query</param>
        /// <param name="intentResult">The intent detection result</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL execution result</returns>
        Task<SqlExecutionResult> GenerateAndExecuteWithIntentAsync(
            string query, 
            IntentDetectionResult intentResult, 
            Guid? tenantId = null, 
            Guid? userId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates and executes a SQL query using reasoning results
        /// </summary>
        /// <param name="query">The original natural language query</param>
        /// <param name="reasoningResult">The reasoning result</param>
        /// <param name="tenantId">Optional tenant ID</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL execution result</returns>
        Task<SqlExecutionResult> GenerateAndExecuteWithReasoningAsync(
            string query, 
            ReasoningResult reasoningResult, 
            Guid? tenantId = null, 
            Guid? userId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a previously generated SQL query
        /// </summary>
        /// <param name="generationResult">The SQL generation result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL execution result</returns>
        Task<SqlExecutionResult> ExecuteGeneratedSqlAsync(
            SqlGenerationResult generationResult, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a SQL query without executing it
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation result</returns>
        Task<SqlValidationResult> ValidateSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the SQL template repository for managing templates
        /// </summary>
        ITemplateRepository TemplateRepository { get; }

        /// <summary>
        /// Gets the SQL logging service for query statistics
        /// </summary>
        ISqlLoggingService LoggingService { get; }
    }
} 