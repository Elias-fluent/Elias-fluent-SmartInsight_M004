using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for executing SQL queries safely
    /// </summary>
    public interface ISqlExecutionService
    {
        /// <summary>
        /// Executes a validated SQL query
        /// </summary>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The execution result</returns>
        Task<SqlExecutionResult> ExecuteQueryAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a SQL query and returns a single result
        /// </summary>
        /// <typeparam name="T">The type of result to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result object</returns>
        Task<T?> ExecuteScalarAsync<T>(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a SQL query and returns multiple results
        /// </summary>
        /// <typeparam name="T">The type of result to return</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of result objects</returns>
        Task<List<T>> ExecuteQueryAsync<T>(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a non-query SQL command (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <param name="parameters">The parameters for the command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of rows affected</returns>
        Task<int> ExecuteNonQueryAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a SQL generation result directly
        /// </summary>
        /// <param name="generationResult">The SQL generation result to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The execution result</returns>
        Task<SqlExecutionResult> ExecuteGenerationResultAsync(
            SqlGenerationResult generationResult, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Safely sanitizes error messages for display to users
        /// </summary>
        /// <param name="error">The original error message</param>
        /// <returns>A sanitized error message safe for display</returns>
        string SanitizeErrorMessage(string error);
    }
} 