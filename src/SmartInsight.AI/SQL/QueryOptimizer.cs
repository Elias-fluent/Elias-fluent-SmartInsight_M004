using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Implementation of the query optimizer for improving SQL query performance
    /// </summary>
    public class QueryOptimizer : IQueryOptimizer
    {
        private readonly ILogger<QueryOptimizer> _logger;
        private readonly ISqlValidator _sqlValidator;
        
        // Common optimization patterns
        private static readonly List<(string Pattern, string Replacement, string Description, OptimizationImpact Impact)> _optimizationPatterns = new List<(string, string, string, OptimizationImpact)>
        {
            // Replace SELECT * with specific columns where possible
            (@"SELECT\s+\*\s+FROM", "SELECT [columns] FROM", "Replace 'SELECT *' with specific columns", OptimizationImpact.Medium),
            
            // Add missing indexes hint
            (@"FROM\s+(\w+)(\s+WHERE|\s+JOIN|\s+ORDER|\s+GROUP|\s+HAVING|\s*$)", "FROM $1 WITH (INDEX=[index_name])$2", "Add missing index hint", OptimizationImpact.High),
            
            // Convert subqueries to joins where appropriate
            (@"SELECT.*WHERE.*IN\s*\(\s*SELECT", "SELECT... JOIN...", "Convert subquery to JOIN", OptimizationImpact.High),
            
            // Add TOP/LIMIT for unbounded results
            (@"^(?!\s*SELECT\s+TOP|\s*SELECT\s+.*\s+LIMIT|\s*SELECT\s+.*\s+FETCH)(\s*SELECT)", "$1 TOP 1000", "Add row limiting clause", OptimizationImpact.Medium),
            
            // Optimize COUNT(*) operations
            (@"\bCOUNT\s*\(\s*\*\s*\)", "COUNT(1)", "Optimize COUNT(*) to COUNT(1)", OptimizationImpact.Low),
            
            // Use EXISTS instead of COUNT for existence checks
            (@"SELECT\s+COUNT\s*\(\s*\*\s*\)\s+FROM.*WHERE.*>\s*0", "SELECT CASE WHEN EXISTS (SELECT 1 FROM...) THEN 1 ELSE 0 END", "Use EXISTS instead of COUNT for existence checks", OptimizationImpact.Medium),
            
            // Avoid ORDER BY for unneeded ordering
            (@"ORDER BY.*LIMIT 1", "/* Remove ORDER BY for LIMIT 1 */", "Remove unnecessary ORDER BY for LIMIT 1", OptimizationImpact.Medium),
            
            // Add NOLOCK hint for read-only operations (consider only for non-critical data)
            (@"FROM\s+(\w+)(\s+WHERE|\s+JOIN|\s+ORDER|\s+GROUP|\s+HAVING|\s*$)", "FROM $1 WITH (NOLOCK)$2", "Add NOLOCK hint for read-only queries", OptimizationImpact.Medium),
            
            // Use date ranges for date filtering
            (@"WHERE\s+(\w+)\s*>=\s*@startDate\s+AND\s+\1\s*<=\s*@endDate", "WHERE $1 BETWEEN @startDate AND @endDate", "Use BETWEEN for date ranges", OptimizationImpact.Low)
        };
        
        /// <summary>
        /// Creates a new QueryOptimizer
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sqlValidator">SQL validator for checking query validity</param>
        public QueryOptimizer(
            ILogger<QueryOptimizer> logger,
            ISqlValidator sqlValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqlValidator = sqlValidator ?? throw new ArgumentNullException(nameof(sqlValidator));
        }
        
        /// <inheritdoc />
        public async Task<string> OptimizeSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // First analyze the SQL to get optimization suggestions
                var analysis = await AnalyzeSqlAsync(sql, parameters, cancellationToken);
                
                // Return the optimized SQL if available, otherwise return the original SQL
                return analysis.IsOptimized ? analysis.OptimizedSql : analysis.OriginalSql;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing SQL query: {SqlQuery}", sql);
                return sql; // Return original SQL if optimization fails
            }
        }
        
        /// <inheritdoc />
        public async Task<OptimizationAnalysisResult> AnalyzeSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new OptimizationAnalysisResult
            {
                OriginalSql = sql,
                OptimizedSql = sql, // Start with the original SQL
                EstimatedImprovementFactor = 1.0
            };
            
            try
            {
                // First, validate the SQL to ensure it's secure and syntactically correct
                var validationResult = await _sqlValidator.ValidateSecurityAsync(sql, parameters, cancellationToken);
                if (!validationResult.IsValid)
                {
                    // Don't optimize invalid SQL
                    _logger.LogWarning("Cannot optimize invalid SQL query: {SqlQuery}", sql);
                    return result;
                }
                
                // Check for performance issues
                var performanceResult = await _sqlValidator.ValidatePerformanceAsync(sql, parameters, cancellationToken);
                foreach (var issue in performanceResult.Issues)
                {
                    result.Suggestions.Add(new OptimizationSuggestion
                    {
                        Description = issue.Description,
                        Impact = GetImpactFromSeverity(issue.Severity),
                        Applied = false,
                        BeforeAfterSql = null
                    });
                }
                
                // Apply optimizations
                string optimizedSql = sql;
                double improvementFactor = 1.0;
                
                // Check for specific optimization patterns and apply them
                foreach (var (pattern, replacement, description, impact) in _optimizationPatterns)
                {
                    string beforeSql = optimizedSql;
                    
                    // Try to apply the optimization (simplified for demonstration purposes)
                    bool canApply = CanApplyOptimization(pattern, optimizedSql, parameters);
                    
                    if (canApply && pattern == @"SELECT\s+\*\s+FROM" && TryOptimizeSelectStar(ref optimizedSql))
                    {
                        // Successfully applied SELECT * optimization
                        result.Suggestions.Add(new OptimizationSuggestion
                        {
                            Description = description,
                            Impact = impact,
                            Applied = true,
                            BeforeAfterSql = $"Before: {beforeSql}\nAfter: {optimizedSql}"
                        });
                        
                        improvementFactor += GetImprovementFactor(impact);
                    }
                    else if (canApply && pattern == @"^(?!\s*SELECT\s+TOP|\s*SELECT\s+.*\s+LIMIT|\s*SELECT\s+.*\s+FETCH)(\s*SELECT)" &&
                            TryAddRowLimit(ref optimizedSql))
                    {
                        // Successfully applied row limiting
                        result.Suggestions.Add(new OptimizationSuggestion
                        {
                            Description = description,
                            Impact = impact,
                            Applied = true,
                            BeforeAfterSql = $"Before: {beforeSql}\nAfter: {optimizedSql}"
                        });
                        
                        improvementFactor += GetImprovementFactor(impact);
                    }
                    else if (canApply && pattern == @"\bCOUNT\s*\(\s*\*\s*\)" &&
                            TryOptimizeCountStar(ref optimizedSql))
                    {
                        // Successfully applied COUNT(*) optimization
                        result.Suggestions.Add(new OptimizationSuggestion
                        {
                            Description = description,
                            Impact = impact,
                            Applied = true,
                            BeforeAfterSql = $"Before: {beforeSql}\nAfter: {optimizedSql}"
                        });
                        
                        improvementFactor += GetImprovementFactor(impact);
                    }
                    else if (Regex.IsMatch(optimizedSql, pattern, RegexOptions.IgnoreCase))
                    {
                        // Add suggestion without applying
                        result.Suggestions.Add(new OptimizationSuggestion
                        {
                            Description = description,
                            Impact = impact,
                            Applied = false,
                            BeforeAfterSql = null
                        });
                    }
                }
                
                // Check for missing WHERE clause
                if (Regex.IsMatch(optimizedSql, @"SELECT.*FROM\s+\w+\s+(?!WHERE)(?:ORDER|GROUP|HAVING|UNION|$)", RegexOptions.IgnoreCase))
                {
                    result.Suggestions.Add(new OptimizationSuggestion
                    {
                        Description = "Query is missing a WHERE clause, which may return too many rows",
                        Impact = OptimizationImpact.High,
                        Applied = false,
                        BeforeAfterSql = null
                    });
                }
                
                // Check for function calls on indexed columns
                if (Regex.IsMatch(optimizedSql, @"WHERE\s+\w+\s*\(\s*\w+\s*\)", RegexOptions.IgnoreCase))
                {
                    result.Suggestions.Add(new OptimizationSuggestion
                    {
                        Description = "Function calls on indexed columns prevent index usage",
                        Impact = OptimizationImpact.High,
                        Applied = false,
                        BeforeAfterSql = null
                    });
                }
                
                // Update result with optimized SQL
                result.OptimizedSql = optimizedSql;
                result.EstimatedImprovementFactor = improvementFactor;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing SQL query for optimization: {SqlQuery}", sql);
                return result; // Return original result if analysis fails
            }
        }
        
        /// <inheritdoc />
        public async Task<QueryCostEstimate> EstimateQueryCostAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new QueryCostEstimate
            {
                EstimatedExecutionTimeMs = 0,
                EstimatedRowCount = 0,
                EstimatedMemoryBytes = 0,
                EstimatedIoOperations = 0,
                CostLevel = QueryCostLevel.Low
            };
            
            try
            {
                // Basic heuristics for cost estimation
                
                // Estimate based on query type
                if (IsSelectQuery(sql))
                {
                    // SELECT query costs - check for various factors
                    int baseRowCount = 100; // Default estimate
                    
                    // Check for WHERE clause - no WHERE means potentially more rows
                    if (!HasWhereClause(sql))
                    {
                        baseRowCount *= 10;
                    }
                    
                    // Check for JOIN operations - each join multiplies rows
                    int joinCount = CountJoins(sql);
                    if (joinCount > 0)
                    {
                        // Each join can multiply the result set
                        baseRowCount *= (int)Math.Pow(2, joinCount);
                    }
                    
                    // Check for aggregations (GROUP BY) - reduces row count but increases CPU
                    bool hasGroupBy = HasGroupBy(sql);
                    
                    // Estimate row count
                    result.EstimatedRowCount = hasGroupBy ? baseRowCount / 5 : baseRowCount;
                    
                    // Estimate execution time based on row count and complexity
                    result.EstimatedExecutionTimeMs = (long)(result.EstimatedRowCount * (hasGroupBy ? 0.5 : 0.1));
                    
                    // Estimate memory usage
                    result.EstimatedMemoryBytes = result.EstimatedRowCount * 1024; // ~1KB per row estimation
                    
                    // Estimate I/O operations
                    result.EstimatedIoOperations = (long)(result.EstimatedRowCount * 0.1);
                }
                else if (IsUpdateQuery(sql))
                {
                    // UPDATE queries typically affect fewer rows but have write costs
                    result.EstimatedRowCount = 10;
                    result.EstimatedExecutionTimeMs = 100;
                    result.EstimatedMemoryBytes = 5 * 1024;
                    result.EstimatedIoOperations = 20;
                }
                else if (IsDeleteQuery(sql))
                {
                    // DELETE queries - similar to UPDATE
                    result.EstimatedRowCount = 10;
                    result.EstimatedExecutionTimeMs = 120;
                    result.EstimatedMemoryBytes = 5 * 1024;
                    result.EstimatedIoOperations = 25;
                }
                else if (IsInsertQuery(sql))
                {
                    // INSERT queries
                    result.EstimatedRowCount = 1;
                    result.EstimatedExecutionTimeMs = 50;
                    result.EstimatedMemoryBytes = 2 * 1024;
                    result.EstimatedIoOperations = 10;
                }
                
                // Determine the overall cost level based on execution time
                result.CostLevel = DetermineCostLevel(result.EstimatedExecutionTimeMs);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating query cost: {SqlQuery}", sql);
                result.CostLevel = QueryCostLevel.High; // Assume high cost on error
                return result;
            }
        }
        
        /// <inheritdoc />
        public Task<string> ApplyPaginationAsync(
            string sql, 
            int pageSize, 
            int pageNumber = 1, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input parameters
                if (pageSize <= 0) throw new ArgumentException("Page size must be greater than zero", nameof(pageSize));
                if (pageNumber <= 0) throw new ArgumentException("Page number must be greater than zero", nameof(pageNumber));
                
                // Remove any existing pagination
                string cleanSql = RemoveExistingPagination(sql);
                
                // Calculate the offset based on page number and size
                int offset = (pageNumber - 1) * pageSize;
                
                // Check if SQL is already ordered and add minimal ordering if not
                if (!HasOrderByClause(cleanSql))
                {
                    // Add a default ORDER BY clause for consistent paging results
                    // This is a simplification - in a real system, use primary key or another stable column
                    cleanSql += " ORDER BY 1";
                }
                
                // Apply OFFSET/FETCH pagination (modern SQL standard)
                string paginatedSql = $"{cleanSql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                
                return Task.FromResult(paginatedSql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying pagination to SQL query: {SqlQuery}, PageSize: {PageSize}, PageNumber: {PageNumber}", 
                    sql, pageSize, pageNumber);
                return Task.FromResult(sql); // Return original SQL if pagination fails
            }
        }
        
        #region Private Helper Methods
        
        /// <summary>
        /// Determines if the optimization pattern can be applied to the SQL
        /// </summary>
        private bool CanApplyOptimization(string pattern, string sql, Dictionary<string, object>? parameters)
        {
            // Simple check if the pattern matches
            return Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Tries to optimize SELECT * queries by replacing with specific columns
        /// </summary>
        private bool TryOptimizeSelectStar(ref string sql)
        {
            // This is a simplified implementation
            // In a real system, you would need to know the actual table schema
            
            // Replace SELECT * with SELECT [first_column], [second_column], ...
            // For demonstration, we'll use a simple replacement that keeps the original behavior
            // but suggests the optimization
            sql = Regex.Replace(sql, 
                @"SELECT\s+\*", 
                "SELECT /* TODO: List specific needed columns instead of * */\n       *", 
                RegexOptions.IgnoreCase);
            
            return true;
        }
        
        /// <summary>
        /// Tries to add a row limit to queries without one
        /// </summary>
        private bool TryAddRowLimit(ref string sql)
        {
            // Add TOP 1000 to unbounded queries
            if (Regex.IsMatch(sql, @"^\s*SELECT", RegexOptions.IgnoreCase) && 
                !Regex.IsMatch(sql, @"\bTOP\b|\bLIMIT\b|\bFETCH\s+FIRST\b", RegexOptions.IgnoreCase))
            {
                sql = Regex.Replace(sql, 
                    @"^\s*SELECT", 
                    "SELECT TOP 1000 /* Added row limit for performance */", 
                    RegexOptions.IgnoreCase);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Tries to optimize COUNT(*) to COUNT(1)
        /// </summary>
        private bool TryOptimizeCountStar(ref string sql)
        {
            // Replace COUNT(*) with COUNT(1) for performance
            sql = Regex.Replace(sql, 
                @"\bCOUNT\s*\(\s*\*\s*\)", 
                "COUNT(1) /* Optimized from COUNT(*) */", 
                RegexOptions.IgnoreCase);
            
            return true;
        }
        
        /// <summary>
        /// Maps ValidationSeverity to OptimizationImpact
        /// </summary>
        private OptimizationImpact GetImpactFromSeverity(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Critical => OptimizationImpact.Critical,
                ValidationSeverity.Warning => OptimizationImpact.Medium,
                ValidationSeverity.Info => OptimizationImpact.Low,
                _ => OptimizationImpact.Low
            };
        }
        
        /// <summary>
        /// Gets a numeric improvement factor based on the optimization impact
        /// </summary>
        private double GetImprovementFactor(OptimizationImpact impact)
        {
            return impact switch
            {
                OptimizationImpact.Critical => 2.0,
                OptimizationImpact.High => 1.5,
                OptimizationImpact.Medium => 1.2,
                OptimizationImpact.Low => 1.1,
                _ => 1.0
            };
        }
        
        /// <summary>
        /// Determines the cost level based on estimated execution time
        /// </summary>
        private QueryCostLevel DetermineCostLevel(long executionTimeMs)
        {
            return executionTimeMs switch
            {
                < 10 => QueryCostLevel.Negligible,
                < 100 => QueryCostLevel.Low,
                < 500 => QueryCostLevel.Medium,
                < 2000 => QueryCostLevel.High,
                < 10000 => QueryCostLevel.VeryHigh,
                _ => QueryCostLevel.Extreme
            };
        }
        
        /// <summary>
        /// Checks if the SQL is a SELECT query
        /// </summary>
        private bool IsSelectQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*SELECT", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL is an UPDATE query
        /// </summary>
        private bool IsUpdateQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*UPDATE", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL is a DELETE query
        /// </summary>
        private bool IsDeleteQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*DELETE", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL is an INSERT query
        /// </summary>
        private bool IsInsertQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*INSERT", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL has a WHERE clause
        /// </summary>
        private bool HasWhereClause(string sql)
        {
            return Regex.IsMatch(sql, @"\bWHERE\b", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL has an ORDER BY clause
        /// </summary>
        private bool HasOrderByClause(string sql)
        {
            return Regex.IsMatch(sql, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL has a GROUP BY clause
        /// </summary>
        private bool HasGroupBy(string sql)
        {
            return Regex.IsMatch(sql, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Counts the number of JOIN operations in the SQL
        /// </summary>
        private int CountJoins(string sql)
        {
            int count = 0;
            
            // Count explicit JOIN keywords
            count += Regex.Matches(sql, @"\bJOIN\b", RegexOptions.IgnoreCase).Count;
            
            // Count implicit joins (comma syntax)
            count += Regex.Matches(sql, @"FROM\s+\w+\s*,\s*\w+", RegexOptions.IgnoreCase).Count;
            
            return count;
        }
        
        /// <summary>
        /// Removes any existing pagination clauses from SQL
        /// </summary>
        private string RemoveExistingPagination(string sql)
        {
            // Remove SQL Server style pagination (TOP)
            sql = Regex.Replace(sql, @"\bTOP\s+\d+\b", "", RegexOptions.IgnoreCase);
            
            // Remove MySQL/PostgreSQL style pagination (LIMIT/OFFSET)
            sql = Regex.Replace(sql, @"\bLIMIT\s+\d+(\s+OFFSET\s+\d+)?\b", "", RegexOptions.IgnoreCase);
            
            // Remove standard SQL:2008 pagination (OFFSET/FETCH)
            sql = Regex.Replace(sql, @"\bOFFSET\s+\d+\s+ROWS(\s+FETCH\s+(FIRST|NEXT)\s+\d+\s+ROWS\s+ONLY)?\b", "", RegexOptions.IgnoreCase);
            
            return sql.Trim();
        }
        
        #endregion
    }
} 