using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Regression
{
    /// <summary>
    /// Regression tests for SQL operations to ensure fixed issues do not reoccur
    /// </summary>
    public class SqlRegressionTests : SqlTestBase
    {
        private readonly ISqlGenerator _sqlGenerator;
        private readonly ISqlValidator _sqlValidator;
        private readonly ISqlSanitizer _sqlSanitizer;
        private readonly IParameterValidator _parameterValidator;
        private readonly IQueryOptimizer _queryOptimizer;

        public SqlRegressionTests(ITestOutputHelper output) : base(output)
        {
            _sqlGenerator = _serviceProvider.GetRequiredService<ISqlGenerator>();
            _sqlValidator = _serviceProvider.GetRequiredService<ISqlValidator>();
            _sqlSanitizer = _serviceProvider.GetRequiredService<ISqlSanitizer>();
            _parameterValidator = _serviceProvider.GetRequiredService<IParameterValidator>();
            _queryOptimizer = _serviceProvider.GetRequiredService<IQueryOptimizer>();
        }

        [Fact]
        public async Task Regression_ValidateEmptySql_ReturnsProperError()
        {
            // This test verifies a previous bug where empty SQL validation 
            // was causing NullReferenceException instead of proper validation failure

            // Arrange
            string emptySql = string.Empty;

            // Act
            var validationResult = await _sqlValidator.ValidateSqlAsync(emptySql);

            // Assert
            Assert.NotNull(validationResult);
            Assert.False(validationResult.IsValid);
            Assert.NotEmpty(validationResult.Issues);
            Assert.Contains(validationResult.Issues, issue => 
                issue.Description.Contains("empty", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Regression_TemplateWithMissingParameters_ValidationIdentifiesIssue()
        {
            // This test verifies a bug fix where template validation wasn't 
            // detecting SQL parameters that weren't defined in the template

            // Arrange
            var template = new SqlTemplate
            {
                Id = "regression-template",
                Name = "Regression Test Template",
                Description = "Template for regression testing",
                SqlTemplateText = "SELECT * FROM Users WHERE Id = @userId AND Email = @email",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "userId",
                        Type = "Integer",
                        Required = true,
                        Description = "User ID"
                    }
                    // Note: @email parameter is in the SQL but missing from parameter definitions
                },
                Created = DateTime.UtcNow,
                Version = "1.0"
            };

            // Act
            var validationResult = await _sqlValidator.ValidateTemplateAsync(template);

            // Assert
            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Issues, issue => 
                issue.Description.Contains("email", StringComparison.OrdinalIgnoreCase) &&
                issue.Description.Contains("parameter", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Regression_CaseSensitiveParameterNames_HandledCorrectly()
        {
            // This test verifies a bug fix where parameter names were being 
            // treated as case-sensitive, causing false validation failures

            // Arrange
            var template = new SqlTemplate
            {
                Id = "case-sensitive-test",
                Name = "Case Sensitivity Test",
                Description = "Template for testing case sensitivity handling",
                SqlTemplateText = "SELECT * FROM Users WHERE Id = @UserID",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "userid", // Note: Different case from SQL parameter @UserID
                        Type = "Integer",
                        Required = true,
                        Description = "User ID"
                    }
                },
                Created = DateTime.UtcNow,
                Version = "1.0"
            };
            
            var parameters = new Dictionary<string, object>
            {
                { "USERID", 1 } // Note: Different case again
            };

            // Act
            var generationResult = await _sqlGenerator.GenerateSqlAsync(template, parameters);

            // Assert
            Assert.True(generationResult.IsSuccessful, 
                $"Generation failed due to case sensitivity issue: {generationResult.ErrorMessage}");
            Assert.NotEmpty(generationResult.Sql);
        }

        [Fact]
        public async Task Regression_MultipleStatementDetection_WorksCorrectly()
        {
            // This test verifies a security bug fix where multiple SQL statements
            // (potential injection vector) weren't always being detected

            // Arrange
            var statements = new[]
            {
                "SELECT * FROM Users; DROP TABLE Logs",
                "SELECT 1\n;\nDROP TABLE Users",
                "SELECT * FROM Users WHERE Id = 1/*comment*/;DROP TABLE Logs",
                "SELECT * FROM Users\r\n;\r\nDROP TABLE Logs"
            };

            // Act & Assert
            foreach (var sql in statements)
            {
                var validationResult = await _sqlValidator.ValidateSecurityAsync(sql);
                
                Assert.False(validationResult.IsValid, 
                    $"Multiple statement not detected: {sql}");
                Assert.Contains(validationResult.Issues, issue => 
                    issue.Category == ValidationCategory.Security && 
                    issue.Severity == ValidationSeverity.Critical);
                
                _output.WriteLine($"Successfully detected multiple statements in: {sql}");
            }
        }

        [Fact]
        public async Task Regression_NestedParameterReplacement_WorksCorrectly()
        {
            // This test verifies a bug fix where parameters within parameters
            // were causing incorrect replacements

            // Arrange
            string sqlTemplate = "SELECT * FROM Products WHERE CategoryId IN (@categoryIds)";
            var parameters = new Dictionary<string, object>
            {
                { "categoryIds", "1, 2, 3" } // This should be treated as a single string parameter
            };

            // Act
            var parameterizedResult = await _sqlSanitizer.ParameterizeSqlAsync(sqlTemplate, parameters);

            // Assert
            Assert.NotNull(parameterizedResult);
            Assert.NotEmpty(parameterizedResult.ParameterizedSql);
            Assert.Equal(1, parameterizedResult.Parameters.Count); // Should only have one parameter
            Assert.Contains("@categoryIds", parameterizedResult.ParameterizedSql);
            
            _output.WriteLine($"Original SQL: {sqlTemplate}");
            _output.WriteLine($"Parameterized SQL: {parameterizedResult.ParameterizedSql}");
        }

        [Fact]
        public async Task Regression_WhitespaceInParameterNames_HandledCorrectly()
        {
            // This test verifies a bug fix where whitespace in parameter names
            // was causing validation and generation issues

            // Arrange
            var template = new SqlTemplate
            {
                Id = "whitespace-test",
                Name = "Whitespace Handling Test",
                Description = "Template for testing whitespace handling",
                SqlTemplateText = "SELECT * FROM Products WHERE Name LIKE @search term", // Note the space
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "search term", // Parameter name with space
                        Type = "String",
                        Required = true,
                        Description = "Search term"
                    }
                },
                Created = DateTime.UtcNow,
                Version = "1.0"
            };
            
            var parameters = new Dictionary<string, object>
            {
                { "search term", "%product%" } // Parameter with space in name
            };

            // Act
            var validationResult = await _sqlValidator.ValidateTemplateAsync(template);
            var generationResult = await _sqlGenerator.GenerateSqlAsync(template, parameters);

            // Assert
            Assert.True(validationResult.IsValid || 
                        !validationResult.Issues.Exists(i => i.Description.Contains("search term", StringComparison.OrdinalIgnoreCase)),
                "Whitespace in parameter name caused validation issue");
            
            Assert.True(generationResult.IsSuccessful, 
                $"Generation failed due to whitespace in parameter name: {generationResult.ErrorMessage}");
            Assert.NotEmpty(generationResult.Sql);
            
            _output.WriteLine($"Template validation: {validationResult.IsValid}");
            _output.WriteLine($"Generated SQL: {generationResult.Sql}");
        }

        [Fact]
        public async Task Regression_DateTimeParameterFormatting_WorksCorrectly()
        {
            // This test verifies a bug fix where DateTime parameters
            // weren't being formatted correctly in SQL

            // Arrange
            var template = new SqlTemplate
            {
                Id = "datetime-test",
                Name = "DateTime Test",
                Description = "Template for testing DateTime parameter handling",
                SqlTemplateText = "SELECT * FROM Orders WHERE OrderDate >= @startDate",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "startDate",
                        Type = "DateTime",
                        Required = true,
                        Description = "Start date for filtering"
                    }
                },
                Created = DateTime.UtcNow,
                Version = "1.0"
            };
            
            var testDate = new DateTime(2023, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            var parameters = new Dictionary<string, object>
            {
                { "startDate", testDate }
            };

            // Act
            var generationResult = await _sqlGenerator.GenerateSqlAsync(template, parameters);
            var parameterizedResult = await _sqlSanitizer.ParameterizeSqlAsync(generationResult.Sql, parameters);

            // Assert
            Assert.True(generationResult.IsSuccessful, 
                $"Generation failed with DateTime parameter: {generationResult.ErrorMessage}");
            
            Assert.NotNull(parameterizedResult);
            Assert.Contains("@startDate", parameterizedResult.ParameterizedSql);
            Assert.True(parameterizedResult.Parameters.ContainsKey("@startDate"));
            
            // Verify DateTime is stored correctly in parameters
            var paramValue = parameterizedResult.Parameters["@startDate"];
            Assert.IsType<DateTime>(paramValue);
            Assert.Equal(testDate, (DateTime)paramValue);
            
            _output.WriteLine($"Generated SQL: {generationResult.Sql}");
            _output.WriteLine($"Parameterized SQL: {parameterizedResult.ParameterizedSql}");
            _output.WriteLine($"Parameter Value: {paramValue}");
        }

        [Fact]
        public async Task Regression_OptimizationDoesNotAlterSemantic_PreservesIntendedBehavior()
        {
            // This test verifies a bug fix where query optimization
            // was altering the semantic meaning of the query

            // Arrange
            string originalQuery = "SELECT * FROM Users WHERE LastLoginDate IS NULL OR LastLoginDate < DATEADD(day, -30, GETDATE())";

            // Act
            var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(originalQuery);

            // Assert
            Assert.NotNull(optimizationResult);
            
            // Whether optimization happens or not, the semantics should be preserved
            if (optimizationResult.IsOptimized)
            {
                // Optimized queries should retain the IS NULL check and the date comparison
                Assert.Contains("IS NULL", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("DATEADD", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("-30", optimizationResult.OptimizedQuery);
            }
            
            _output.WriteLine($"Original query: {originalQuery}");
            _output.WriteLine($"Optimized query: {optimizationResult.OptimizedQuery}");
            _output.WriteLine($"Optimized: {optimizationResult.IsOptimized}");
            _output.WriteLine($"Explanation: {optimizationResult.Explanation}");
        }

        [Fact]
        public async Task Regression_ComplexJoinOptimization_ProducesValidSql()
        {
            // This test verifies a bug fix where optimizing complex joins
            // was producing invalid SQL syntax

            // Arrange
            string complexJoinQuery = @"
                SELECT u.Id, u.Name, o.Id AS OrderId, o.Total, 
                       p.Id AS ProductId, p.Name AS ProductName
                FROM Users u, Orders o, OrderItems oi, Products p
                WHERE u.Id = o.UserId 
                  AND o.Id = oi.OrderId 
                  AND oi.ProductId = p.Id 
                  AND u.IsActive = 1";

            // Act
            var optimizationResult = await _queryOptimizer.OptimizeQueryAsync(complexJoinQuery);
            var validationResult = await _sqlValidator.ValidateSqlAsync(
                optimizationResult.IsOptimized ? optimizationResult.OptimizedQuery : complexJoinQuery);

            // Assert
            Assert.True(validationResult.IsValid ||
                        !validationResult.Issues.Exists(i => i.Severity == ValidationSeverity.Critical), 
                "Optimized complex join query has critical validation issues");
            
            if (optimizationResult.IsOptimized)
            {
                // Optimized query should use explicit JOIN syntax
                Assert.Contains("JOIN", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
                
                // All tables should still be referenced
                Assert.Contains("Users", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Orders", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("OrderItems", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Products", optimizationResult.OptimizedQuery, StringComparison.OrdinalIgnoreCase);
            }
            
            _output.WriteLine($"Original query: {complexJoinQuery}");
            _output.WriteLine($"Optimized query: {optimizationResult.OptimizedQuery}");
            _output.WriteLine($"Optimized: {optimizationResult.IsOptimized}");
            _output.WriteLine($"Validation result: {validationResult.IsValid}");
        }

        [Fact]
        public async Task Regression_NaturalLanguageEdgeCases_HandleSuccessfully()
        {
            // This test verifies bug fixes for edge cases in natural language processing
            // that were previously causing failures

            // Arrange
            var edgeCaseQueries = new[]
            {
                "users", // Single word query
                "?", // Just a question mark
                "find users.", // Query with punctuation
                "SHOW ALL DATA FROM USERS!!!", // All caps with exclamation
                "show me   users   with   spaces" // Excessive spaces
            };

            // Act & Assert
            foreach (var query in edgeCaseQueries)
            {
                var generationResult = await _sqlGenerator.GenerateSqlFromQueryAsync(query);
                
                // We're testing that the system doesn't crash, not the quality of results
                _output.WriteLine($"Query: '{query}'");
                _output.WriteLine($"Success: {generationResult.IsSuccessful}");
                _output.WriteLine($"SQL: {generationResult.Sql}");
                _output.WriteLine($"Error: {generationResult.ErrorMessage}");
                _output.WriteLine(new string('-', 50));
                
                // No exception should be thrown, but the generation might fail gracefully
                if (!generationResult.IsSuccessful)
                {
                    Assert.NotNull(generationResult.ErrorMessage);
                    Assert.NotEmpty(generationResult.ErrorMessage);
                }
            }
        }
    }
} 