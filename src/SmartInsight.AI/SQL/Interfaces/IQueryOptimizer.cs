using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for optimizing SQL queries for performance
    /// </summary>
    public interface IQueryOptimizer
    {
        /// <summary>
        /// Optimizes a SQL query for performance
        /// </summary>
        /// <param name="sql">The SQL query to optimize</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The optimized SQL query</returns>
        Task<string> OptimizeSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes a SQL query for potential performance issues
        /// </summary>
        /// <param name="sql">The SQL query to analyze</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The optimization analysis result</returns>
        Task<OptimizationAnalysisResult> AnalyzeSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Estimates the cost of executing a SQL query
        /// </summary>
        /// <param name="sql">The SQL query to analyze</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The estimated cost of the query</returns>
        Task<QueryCostEstimate> EstimateQueryCostAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies pagination to a SQL query
        /// </summary>
        /// <param name="sql">The SQL query to paginate</param>
        /// <param name="pageSize">The page size</param>
        /// <param name="pageNumber">The page number (1-based)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The paginated SQL query</returns>
        Task<string> ApplyPaginationAsync(
            string sql, 
            int pageSize, 
            int pageNumber = 1, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes a SQL query and returns a rich optimization result
        /// </summary>
        /// <param name="sql">The SQL query to optimize</param>
        /// <param name="parameters">The parameters for the query (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The optimization result with detailed information</returns>
        Task<OptimizationResult> OptimizeQueryAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the complexity score of a SQL query (1-10)
        /// </summary>
        /// <param name="sql">The SQL query to analyze</param>
        /// <param name="parameters">The parameters for the query (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Complexity score from 1 (simple) to 10 (very complex)</returns>
        Task<int> GetQueryComplexityAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes the performance characteristics of a SQL query
        /// </summary>
        /// <param name="sql">The SQL query to analyze</param>
        /// <param name="parameters">The parameters for the query (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Detailed performance analysis result</returns>
        Task<QueryPerformanceAnalysis> AnalyzeQueryPerformanceAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of SQL query optimization analysis
    /// </summary>
    public class OptimizationAnalysisResult
    {
        /// <summary>
        /// The original SQL query
        /// </summary>
        public string OriginalSql { get; set; } = null!;

        /// <summary>
        /// The optimized SQL query
        /// </summary>
        public string OptimizedSql { get; set; } = null!;

        /// <summary>
        /// List of optimization suggestions
        /// </summary>
        public List<OptimizationSuggestion> Suggestions { get; set; } = new List<OptimizationSuggestion>();

        /// <summary>
        /// The estimated performance improvement factor
        /// </summary>
        public double EstimatedImprovementFactor { get; set; }

        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool IsOptimized => OriginalSql != OptimizedSql;
    }

    /// <summary>
    /// Represents an optimization suggestion for a SQL query
    /// </summary>
    public class OptimizationSuggestion
    {
        /// <summary>
        /// Description of the suggestion
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Impact level of the suggestion
        /// </summary>
        public OptimizationImpact Impact { get; set; }

        /// <summary>
        /// Whether the suggestion was applied
        /// </summary>
        public bool Applied { get; set; }

        /// <summary>
        /// Before and after SQL if the suggestion was applied
        /// </summary>
        public string? BeforeAfterSql { get; set; }
    }

    /// <summary>
    /// Impact levels for optimization suggestions
    /// </summary>
    public enum OptimizationImpact
    {
        /// <summary>
        /// Low impact optimization
        /// </summary>
        Low,

        /// <summary>
        /// Medium impact optimization
        /// </summary>
        Medium,

        /// <summary>
        /// High impact optimization
        /// </summary>
        High,

        /// <summary>
        /// Critical impact optimization
        /// </summary>
        Critical
    }

    /// <summary>
    /// Estimated cost of executing a SQL query
    /// </summary>
    public class QueryCostEstimate
    {
        /// <summary>
        /// Estimated execution time in milliseconds
        /// </summary>
        public long EstimatedExecutionTimeMs { get; set; }

        /// <summary>
        /// Estimated number of rows returned or affected
        /// </summary>
        public long EstimatedRowCount { get; set; }

        /// <summary>
        /// Estimated memory usage in bytes
        /// </summary>
        public long EstimatedMemoryBytes { get; set; }

        /// <summary>
        /// Estimated disk I/O operations
        /// </summary>
        public long EstimatedIoOperations { get; set; }

        /// <summary>
        /// Overall cost classification
        /// </summary>
        public QueryCostLevel CostLevel { get; set; }
    }

    /// <summary>
    /// Overall cost level of a SQL query
    /// </summary>
    public enum QueryCostLevel
    {
        /// <summary>
        /// Negligible cost
        /// </summary>
        Negligible,

        /// <summary>
        /// Low cost
        /// </summary>
        Low,

        /// <summary>
        /// Medium cost
        /// </summary>
        Medium,

        /// <summary>
        /// High cost
        /// </summary>
        High,

        /// <summary>
        /// Very high cost
        /// </summary>
        VeryHigh,

        /// <summary>
        /// Extreme cost
        /// </summary>
        Extreme
    }
} 