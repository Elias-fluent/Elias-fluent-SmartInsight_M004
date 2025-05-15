using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for optimizing SQL queries for performance
    /// </summary>
    public interface ISqlQueryOptimizationService
    {
        /// <summary>
        /// Optimizes a SQL query for performance
        /// </summary>
        /// <param name="sqlQuery">The SQL query to optimize</param>
        /// <param name="parameters">Optional query parameters</param>
        /// <param name="applyOptimizations">Whether to apply optimizations or just suggest them</param>
        /// <param name="validateBefore">Whether to validate the query before optimization</param>
        /// <param name="validateAfter">Whether to validate the optimized query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The optimization result</returns>
        Task<OptimizationResult> OptimizeQueryAsync(
            string sqlQuery, 
            Dictionary<string, object>? parameters = null, 
            bool applyOptimizations = true,
            bool validateBefore = true,
            bool validateAfter = true,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Applies pagination to a SQL query
        /// </summary>
        /// <param name="sqlQuery">The SQL query to paginate</param>
        /// <param name="pageSize">The page size</param>
        /// <param name="pageNumber">The page number (1-based)</param>
        /// <param name="parameters">Optional query parameters</param>
        /// <param name="validateQuery">Whether to validate the query before pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The pagination result</returns>
        Task<PaginationResult> PaginateQueryAsync(
            string sqlQuery, 
            int pageSize, 
            int pageNumber = 1, 
            Dictionary<string, object>? parameters = null, 
            bool validateQuery = true,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Optimizes a batch of SQL queries
        /// </summary>
        /// <param name="sqlQueries">The SQL queries to optimize</param>
        /// <param name="applyOptimizations">Whether to apply optimizations or just suggest them</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The batch optimization result</returns>
        Task<OptimizationBatchResult> OptimizeBatchAsync(
            IEnumerable<string> sqlQueries, 
            bool applyOptimizations = true,
            CancellationToken cancellationToken = default);
    }
} 