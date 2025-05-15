using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for validating SQL queries against security and performance rules
    /// </summary>
    public interface ISqlValidator
    {
        /// <summary>
        /// Validates a SQL query against security and performance rules
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="parameters">The parameters used in the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation result</returns>
        Task<SqlValidationResult> ValidateSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a SQL query specifically against security rules
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="parameters">The parameters used in the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation result with security issues</returns>
        Task<SqlValidationResult> ValidateSecurityAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a SQL query specifically against performance rules
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="parameters">The parameters used in the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation result with performance issues</returns>
        Task<SqlValidationResult> ValidatePerformanceAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a SQL template against security and performance rules
        /// </summary>
        /// <param name="template">The SQL template to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation result</returns>
        Task<SqlValidationResult> ValidateTemplateAsync(
            SqlTemplate template, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a SQL query is safe to execute
        /// </summary>
        /// <param name="sql">The SQL query to check</param>
        /// <param name="parameters">The parameters used in the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the query is safe, false otherwise</returns>
        Task<bool> IsSqlSafeAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);
    }
} 