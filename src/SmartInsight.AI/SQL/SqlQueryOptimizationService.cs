using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Service that provides a facade for query optimization operations
    /// </summary>
    public class SqlQueryOptimizationService : ISqlQueryOptimizationService
    {
        private readonly ILogger<SqlQueryOptimizationService> _logger;
        private readonly IQueryOptimizer _queryOptimizer;
        private readonly ISqlValidator _sqlValidator;
        
        /// <summary>
        /// Creates a new SqlQueryOptimizationService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="queryOptimizer">Query optimizer</param>
        /// <param name="sqlValidator">SQL validator</param>
        public SqlQueryOptimizationService(
            ILogger<SqlQueryOptimizationService> logger,
            IQueryOptimizer queryOptimizer,
            ISqlValidator sqlValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryOptimizer = queryOptimizer ?? throw new ArgumentNullException(nameof(queryOptimizer));
            _sqlValidator = sqlValidator ?? throw new ArgumentNullException(nameof(sqlValidator));
        }
        
        /// <inheritdoc />
        public async Task<OptimizationResult> OptimizeQueryAsync(
            string sqlQuery, 
            Dictionary<string, object>? parameters = null, 
            bool applyOptimizations = true,
            bool validateBefore = true,
            bool validateAfter = true,
            CancellationToken cancellationToken = default)
        {
            var result = new OptimizationResult
            {
                OriginalSql = sqlQuery,
                OptimizedSql = sqlQuery,
                IsSuccessful = true,
                Parameters = parameters ?? new Dictionary<string, object>()
            };
            
            try
            {
                // Validate the query before optimization if requested
                if (validateBefore)
                {
                    var validationResult = await _sqlValidator.ValidateSqlAsync(sqlQuery, parameters, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = "SQL validation failed before optimization";
                        result.ValidationIssues.AddRange(validationResult.Issues);
                        return result;
                    }
                }
                
                // Analyze the query for optimization opportunities
                var analysisResult = await _queryOptimizer.AnalyzeSqlAsync(sqlQuery, parameters, cancellationToken);
                
                // Apply optimizations if requested
                if (applyOptimizations && analysisResult.IsOptimized)
                {
                    result.OptimizedSql = analysisResult.OptimizedSql;
                    result.PerformanceImprovementFactor = analysisResult.EstimatedImprovementFactor;
                }
                
                // Add optimization suggestions to the result
                result.Suggestions.AddRange(analysisResult.Suggestions);
                
                // Estimate query cost
                var costEstimate = await _queryOptimizer.EstimateQueryCostAsync(
                    applyOptimizations ? result.OptimizedSql : result.OriginalSql, 
                    parameters, 
                    cancellationToken);
                
                result.EstimatedCost = costEstimate;
                
                // Validate the optimized query if requested
                if (validateAfter && result.OriginalSql != result.OptimizedSql)
                {
                    var validationResult = await _sqlValidator.ValidateSqlAsync(result.OptimizedSql, parameters, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        // If optimized query is invalid, revert to original
                        _logger.LogWarning("Optimized SQL failed validation. Reverting to original SQL.");
                        result.OptimizedSql = result.OriginalSql;
                        result.ValidationIssues.AddRange(validationResult.Issues);
                        result.ErrorMessage = "Optimized SQL failed validation. Using original SQL.";
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL query optimization: {SqlQuery}", sqlQuery);
                result.IsSuccessful = false;
                result.ErrorMessage = $"Error during optimization: {ex.Message}";
                return result;
            }
        }
        
        /// <inheritdoc />
        public async Task<PaginationResult> PaginateQueryAsync(
            string sqlQuery, 
            int pageSize, 
            int pageNumber = 1, 
            Dictionary<string, object>? parameters = null, 
            bool validateQuery = true,
            CancellationToken cancellationToken = default)
        {
            var result = new PaginationResult
            {
                OriginalSql = sqlQuery,
                PaginatedSql = sqlQuery,
                IsSuccessful = true,
                Parameters = parameters ?? new Dictionary<string, object>(),
                PageSize = pageSize,
                PageNumber = pageNumber
            };
            
            try
            {
                // Validate input parameters
                if (pageSize <= 0) throw new ArgumentException("Page size must be greater than zero", nameof(pageSize));
                if (pageNumber <= 0) throw new ArgumentException("Page number must be greater than zero", nameof(pageNumber));
                
                // Validate the query before pagination if requested
                if (validateQuery)
                {
                    var validationResult = await _sqlValidator.ValidateSqlAsync(sqlQuery, parameters, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = "SQL validation failed before pagination";
                        result.ValidationIssues.AddRange(validationResult.Issues);
                        return result;
                    }
                }
                
                // Apply pagination
                result.PaginatedSql = await _queryOptimizer.ApplyPaginationAsync(
                    sqlQuery, 
                    pageSize, 
                    pageNumber, 
                    cancellationToken);
                
                // Calculate total pages and records if possible
                // Note: In a real system, this would require a separate count query
                result.TotalRecords = -1; // Unknown without a count query
                result.TotalPages = -1;   // Unknown without a count query
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL query pagination: {SqlQuery}, PageSize: {PageSize}, PageNumber: {PageNumber}", 
                    sqlQuery, pageSize, pageNumber);
                result.IsSuccessful = false;
                result.ErrorMessage = $"Error during pagination: {ex.Message}";
                return result;
            }
        }
        
        /// <inheritdoc />
        public async Task<OptimizationBatchResult> OptimizeBatchAsync(
            IEnumerable<string> sqlQueries, 
            bool applyOptimizations = true,
            CancellationToken cancellationToken = default)
        {
            var result = new OptimizationBatchResult
            {
                IsSuccessful = true,
                Results = new List<OptimizationResult>()
            };
            
            try
            {
                foreach (var query in sqlQueries)
                {
                    var optimizationResult = await OptimizeQueryAsync(
                        query, 
                        null, 
                        applyOptimizations, 
                        true, 
                        true, 
                        cancellationToken);
                    
                    result.Results.Add(optimizationResult);
                    
                    // If any optimization fails, mark the batch as failed
                    if (!optimizationResult.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                    }
                }
                
                // Calculate average performance improvement
                double totalImprovement = 0;
                int optimizedCount = 0;
                
                foreach (var optimizationResult in result.Results)
                {
                    if (optimizationResult.PerformanceImprovementFactor > 1.0)
                    {
                        totalImprovement += optimizationResult.PerformanceImprovementFactor;
                        optimizedCount++;
                    }
                }
                
                result.AverageImprovementFactor = optimizedCount > 0 
                    ? totalImprovement / optimizedCount 
                    : 1.0;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL batch optimization");
                result.IsSuccessful = false;
                result.ErrorMessage = $"Error during batch optimization: {ex.Message}";
                return result;
            }
        }
    }
} 