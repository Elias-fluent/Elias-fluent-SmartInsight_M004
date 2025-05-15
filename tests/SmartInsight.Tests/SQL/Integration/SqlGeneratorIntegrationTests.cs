using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using SmartInsight.Tests.SQL.Common.TestData;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Integration
{
    /// <summary>
    /// Integration tests for SQL Generator and related components
    /// </summary>
    public class SqlGeneratorIntegrationTests : SqlTestBase
    {
        private readonly ISqlGenerator _sqlGenerator;
        private readonly ISqlValidator _sqlValidator;
        private readonly IParameterValidator _parameterValidator;
        private readonly ISqlSanitizer _sqlSanitizer;
        private readonly IQueryOptimizer _queryOptimizer;

        public SqlGeneratorIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _sqlGenerator = _serviceProvider.GetRequiredService<ISqlGenerator>();
            _sqlValidator = _serviceProvider.GetRequiredService<ISqlValidator>();
            _parameterValidator = _serviceProvider.GetRequiredService<IParameterValidator>();
            _sqlSanitizer = _serviceProvider.GetRequiredService<ISqlSanitizer>();
            _queryOptimizer = _serviceProvider.GetRequiredService<IQueryOptimizer>();
        }

        [Fact]
        public async Task CompleteWorkflow_GenerateValidateOptimizeExecute_SuccessfullyIntegrates()
        {
            // Arrange
            var template = new SqlTemplate
            {
                Id = "integration-test-template",
                Name = "Integration Test Template",
                Description = "Template for testing integrated SQL generation workflow",
                SqlTemplateText = "SELECT * FROM Users WHERE Name LIKE @namePattern AND (@minAge IS NULL OR Age >= @minAge)",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "namePattern",
                        Type = "String",
                        Required = true,
                        Description = "Name pattern to filter by"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "minAge",
                        Type = "Integer",
                        Required = false,
                        Description = "Minimum age to filter by"
                    }
                },
                Created = DateTime.UtcNow,
                Version = "1.0"
            };

            var parameters = new Dictionary<string, object>
            {
                { "namePattern", "%smith%" },
                { "minAge", 21 }
            };

            // 1. Start with SQL Generation
            _output.WriteLine("Step 1: Generating SQL");
            var generationResult = await _sqlGenerator.GenerateSqlAsync(template, parameters);
            
            // Ensure generation was successful
            Assert.True(generationResult.IsSuccessful, $"SQL generation failed: {generationResult.ErrorMessage}");
            Assert.NotEmpty(generationResult.Sql);
            _output.WriteLine($"Generated SQL: {generationResult.Sql}");

            // 2. Validate the generated SQL
            _output.WriteLine("Step 2: Validating SQL");
            var validationResult = await _sqlValidator.ValidateSqlAsync(generationResult.Sql, parameters);
            
            // Not all validation issues are critical, so check if it's safe to execute
            bool isSafe = await _sqlValidator.IsSqlSafeAsync(generationResult.Sql, parameters);
            Assert.True(isSafe, $"SQL was not deemed safe: {string.Join(", ", validationResult.Issues)}");

            foreach (var issue in validationResult.Issues)
            {
                _output.WriteLine($"Validation Issue: {issue.Description} (Severity: {issue.Severity}, Category: {issue.Category})");
            }

            // 3. Parameterize SQL for safe execution
            _output.WriteLine("Step 3: Parameterizing SQL");
            var parameterizedResult = await _sqlSanitizer.ParameterizeSqlAsync(generationResult.Sql, parameters);
            
            Assert.NotNull(parameterizedResult);
            Assert.NotEmpty(parameterizedResult.ParameterizedSql);
            Assert.NotEmpty(parameterizedResult.Parameters);
            _output.WriteLine($"Parameterized SQL: {parameterizedResult.ParameterizedSql}");

            // 4. Optimize the SQL query
            _output.WriteLine("Step 4: Optimizing SQL");
            var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(generationResult.Sql);
            
            Assert.NotNull(optimizationResult);
            if (optimizationResult.IsOptimized)
            {
                _output.WriteLine($"Optimized SQL: {optimizationResult.OptimizedQuery}");
                _output.WriteLine($"Explanation: {optimizationResult.Explanation}");
                _output.WriteLine($"Estimated improvement: {optimizationResult.EstimatedImprovementPercentage}%");
            }
            else
            {
                _output.WriteLine("No optimizations were necessary or possible.");
            }

            // 5. Get query complexity metrics
            _output.WriteLine("Step 5: Analyzing query complexity");
            var complexity = await _queryOptimizer.GetQueryComplexityAsync(generationResult.Sql);
            
            Assert.True(complexity >= 0);
            _output.WriteLine($"Query complexity score: {complexity}");

            // 6. Analyze query performance
            _output.WriteLine("Step 6: Analyzing query performance");
            var performanceAnalysis = await _queryOptimizer.AnalyzeQueryPerformanceAsync(generationResult.Sql);
            
            Assert.NotNull(performanceAnalysis);
            _output.WriteLine($"Performance analysis cost score: {performanceAnalysis.EstimatedCostScore}");
            foreach (var factor in performanceAnalysis.PerformanceFactors)
            {
                _output.WriteLine($"Performance factor: {factor}");
            }
            
            // 7. Full workflow completes successfully
            _output.WriteLine("Integration test complete - all components worked together successfully.");
        }

        [Fact]
        public async Task CompleteWorkflow_WithNaturalLanguageQuery_GeneratesAndValidates()
        {
            // Arrange
            string naturalLanguageQuery = "find all users with name containing 'smith' who are at least 21 years old";
            var tenantContext = CreateTestTenantContext();

            // Start with direct SQL generation from natural language
            _output.WriteLine("Step 1: Generating SQL from natural language query");
            var generationResult = await _sqlGenerator.GenerateSqlFromQueryAsync(naturalLanguageQuery, tenantContext);
            
            // Ensure generation was successful
            Assert.True(generationResult.IsSuccessful, $"SQL generation from NL failed: {generationResult.ErrorMessage}");
            Assert.NotEmpty(generationResult.Sql);
            _output.WriteLine($"Generated SQL: {generationResult.Sql}");

            // Validate the generated SQL
            _output.WriteLine("Step 2: Validating generated SQL");
            var validationResult = await _sqlValidator.ValidateSqlAsync(generationResult.Sql, generationResult.Parameters);
            
            // Output any validation issues
            foreach (var issue in validationResult.Issues)
            {
                _output.WriteLine($"Validation Issue: {issue.Description} (Severity: {issue.Severity}, Category: {issue.Category})");
            }

            // Check if critical security issues exist
            bool hasSecurityIssues = validationResult.HasSecurityIssues();
            Assert.False(hasSecurityIssues, "Generated SQL has critical security issues");

            // Optimize the SQL query
            _output.WriteLine("Step 3: Optimizing generated SQL");
            var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(generationResult.Sql);
            
            Assert.NotNull(optimizationResult);
            if (optimizationResult.IsOptimized)
            {
                _output.WriteLine($"Optimized SQL: {optimizationResult.OptimizedQuery}");
                _output.WriteLine($"Explanation: {optimizationResult.Explanation}");
                _output.WriteLine($"Estimated improvement: {optimizationResult.EstimatedImprovementPercentage}%");
            }
            else
            {
                _output.WriteLine("No optimizations were necessary or possible.");
            }

            // Full workflow completes successfully
            _output.WriteLine("NL query integration test complete - all components worked together successfully.");
        }
    }
} 