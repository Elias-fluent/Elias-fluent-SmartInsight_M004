using System;
using System.Collections.Generic;
using System.Linq;
using SmartInsight.AI.SQL.Interfaces;

namespace SmartInsight.AI.SQL.Models
{
    /// <summary>
    /// Result of SQL query optimization
    /// </summary>
    public class OptimizationResult
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
        /// The optimized query (alias for OptimizedSql for backward compatibility with tests)
        /// </summary>
        public string OptimizedQuery => OptimizedSql;
        
        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// The parameters for the query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Performance improvement factor (1.0 = no improvement)
        /// </summary>
        public double PerformanceImprovementFactor { get; set; } = 1.0;
        
        /// <summary>
        /// Estimated improvement percentage (for backward compatibility with tests)
        /// </summary>
        public double EstimatedImprovementPercentage => (PerformanceImprovementFactor - 1.0) * 100.0;
        
        /// <summary>
        /// Error message if optimization failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// List of optimization suggestions
        /// </summary>
        public List<OptimizationSuggestion> Suggestions { get; set; } = new List<OptimizationSuggestion>();
        
        /// <summary>
        /// Explanation of the optimization (for backward compatibility with tests)
        /// </summary>
        public string Explanation => string.Join("; ", Suggestions.ConvertAll(s => s.Description));
        
        /// <summary>
        /// Estimated cost of executing the query
        /// </summary>
        public QueryCostEstimate? EstimatedCost { get; set; }
        
        /// <summary>
        /// Validation issues identified during optimization
        /// </summary>
        public List<SqlValidationIssue> ValidationIssues { get; set; } = new List<SqlValidationIssue>();
        
        /// <summary>
        /// Index suggestions (for backward compatibility with tests)
        /// </summary>
        public List<string> IndexSuggestions => Suggestions
            .Where(s => s.Description.Contains("index", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Description)
            .ToList();
        
        /// <summary>
        /// Whether the query was actually optimized
        /// </summary>
        public bool IsOptimized => OriginalSql != OptimizedSql;
    }
    
    /// <summary>
    /// Result of batch SQL query optimization
    /// </summary>
    public class OptimizationBatchResult
    {
        /// <summary>
        /// Whether the batch optimization was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Error message if batch optimization failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// List of individual optimization results
        /// </summary>
        public List<OptimizationResult> Results { get; set; } = new List<OptimizationResult>();
        
        /// <summary>
        /// Average performance improvement factor across all queries
        /// </summary>
        public double AverageImprovementFactor { get; set; } = 1.0;
    }
    
    /// <summary>
    /// Result of applying pagination to a SQL query
    /// </summary>
    public class PaginationResult
    {
        /// <summary>
        /// The original SQL query
        /// </summary>
        public string OriginalSql { get; set; } = null!;
        
        /// <summary>
        /// The paginated SQL query
        /// </summary>
        public string PaginatedSql { get; set; } = null!;
        
        /// <summary>
        /// Whether the pagination was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// The parameters for the query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Error message if pagination failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// Total number of records across all pages (if available)
        /// </summary>
        public long TotalRecords { get; set; } = -1;
        
        /// <summary>
        /// Total number of pages (if available)
        /// </summary>
        public int TotalPages { get; set; } = -1;
        
        /// <summary>
        /// Validation issues identified during pagination
        /// </summary>
        public List<SqlValidationIssue> ValidationIssues { get; set; } = new List<SqlValidationIssue>();
    }
    
    /// <summary>
    /// SQL query optimization statistics
    /// </summary>
    public class OptimizationStatistics
    {
        /// <summary>
        /// Number of queries optimized
        /// </summary>
        public int QueriesOptimized { get; set; }
        
        /// <summary>
        /// Number of queries that couldn't be optimized
        /// </summary>
        public int QueriesNotOptimized { get; set; }
        
        /// <summary>
        /// Average performance improvement factor
        /// </summary>
        public double AverageImprovementFactor { get; set; }
        
        /// <summary>
        /// Most common optimization suggestion
        /// </summary>
        public string? MostCommonSuggestion { get; set; }
        
        /// <summary>
        /// Most common query pattern
        /// </summary>
        public string? MostCommonQueryPattern { get; set; }
        
        /// <summary>
        /// Performance improvement distribution by impact level
        /// </summary>
        public Dictionary<OptimizationImpact, int> ImprovementsByLevel { get; set; } = new Dictionary<OptimizationImpact, int>();
        
        /// <summary>
        /// Most common query cost level
        /// </summary>
        public QueryCostLevel MostCommonCostLevel { get; set; }
    }
    
    /// <summary>
    /// Detailed performance analysis for a SQL query
    /// </summary>
    public class QueryPerformanceAnalysis
    {
        /// <summary>
        /// The SQL query that was analyzed
        /// </summary>
        public string Sql { get; set; } = null!;
        
        /// <summary>
        /// Estimated cost score (higher values indicate higher cost)
        /// </summary>
        public double EstimatedCostScore { get; set; }
        
        /// <summary>
        /// List of performance factors affecting the query
        /// </summary>
        public List<string> PerformanceFactors { get; set; } = new List<string>();
        
        /// <summary>
        /// List of recommended indexes to improve performance
        /// </summary>
        public List<string> RecommendedIndexes { get; set; } = new List<string>();
        
        /// <summary>
        /// Whether the query could benefit from caching
        /// </summary>
        public bool CachingRecommended { get; set; }
        
        /// <summary>
        /// Estimated query execution time in milliseconds
        /// </summary>
        public long EstimatedExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Estimated memory usage in kilobytes
        /// </summary>
        public long EstimatedMemoryUsageKb { get; set; }
        
        /// <summary>
        /// Tables affected by the query
        /// </summary>
        public List<string> AffectedTables { get; set; } = new List<string>();
        
        /// <summary>
        /// Potential bottlenecks in the query
        /// </summary>
        public List<string> Bottlenecks { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Represents a query optimization suggestion
    /// </summary>
    public class QueryOptimizationSuggestion
    {
        /// <summary>
        /// The original query before optimization
        /// </summary>
        public string OriginalQuery { get; set; } = null!;
        
        /// <summary>
        /// The optimized query
        /// </summary>
        public string OptimizedQuery { get; set; } = null!;
        
        /// <summary>
        /// Description of the optimization
        /// </summary>
        public string Description { get; set; } = null!;
        
        /// <summary>
        /// Estimated performance improvement percentage
        /// </summary>
        public double ImprovementEstimate { get; set; }
    }
} 