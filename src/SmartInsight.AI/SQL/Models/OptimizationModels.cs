using System.Collections.Generic;
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
        /// Error message if optimization failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// List of optimization suggestions
        /// </summary>
        public List<OptimizationSuggestion> Suggestions { get; set; } = new List<OptimizationSuggestion>();
        
        /// <summary>
        /// Estimated cost of executing the query
        /// </summary>
        public QueryCostEstimate? EstimatedCost { get; set; }
        
        /// <summary>
        /// Validation issues identified during optimization
        /// </summary>
        public List<SqlValidationIssue> ValidationIssues { get; set; } = new List<SqlValidationIssue>();
        
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
} 