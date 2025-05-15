using System;
using System.Collections.Generic;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.Tests.SQL.Common.TestData
{
    /// <summary>
    /// Provides test data for SQL tests
    /// </summary>
    public static class SqlTestData
    {
        /// <summary>
        /// Gets a collection of sample SQL templates for testing
        /// </summary>
        /// <returns>Collection of SQL templates</returns>
        public static IEnumerable<SqlTemplate> GetSampleTemplates()
        {
            yield return new SqlTemplate
            {
                Id = "template-1",
                Name = "Get User By ID",
                Description = "Retrieves user details by ID",
                SqlTemplateText = "SELECT Id, Name, Email, CreatedDate FROM Users WHERE Id = @userId",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "userId",
                        Type = "Integer",
                        Required = true,
                        Description = "User ID to query"
                    }
                },
                Created = DateTime.UtcNow.AddDays(-30),
                LastModified = DateTime.UtcNow.AddDays(-5),
                Version = "1.0",
                IntentMapping = new List<string> { "QueryUser", "GetUser" },
                Tags = new List<string> { "User", "Query" }
            };
            
            yield return new SqlTemplate
            {
                Id = "template-2",
                Name = "Get Orders By User",
                Description = "Retrieves orders for a specific user",
                SqlTemplateText = "SELECT o.Id, o.OrderDate, o.Total, o.Status FROM Orders o WHERE o.UserId = @userId AND (@startDate IS NULL OR o.OrderDate >= @startDate) AND (@endDate IS NULL OR o.OrderDate <= @endDate)",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "userId",
                        Type = "Integer",
                        Required = true,
                        Description = "User ID to query orders for"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "startDate",
                        Type = "DateTime",
                        Required = false,
                        Description = "Optional start date filter"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "endDate",
                        Type = "DateTime",
                        Required = false,
                        Description = "Optional end date filter"
                    }
                },
                Created = DateTime.UtcNow.AddDays(-15),
                LastModified = DateTime.UtcNow.AddDays(-2),
                Version = "1.1",
                IntentMapping = new List<string> { "QueryUserOrders", "GetUserOrders" },
                Tags = new List<string> { "Orders", "User", "Query" }
            };

            yield return new SqlTemplate
            {
                Id = "template-3",
                Name = "Insert New User",
                Description = "Creates a new user record",
                SqlTemplateText = "INSERT INTO Users (Name, Email, Password, CreatedDate) VALUES (@name, @email, @password, @createdDate); SELECT SCOPE_IDENTITY()",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "name",
                        Type = "String",
                        Required = true,
                        Description = "User's full name"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "email",
                        Type = "String",
                        Required = true,
                        Description = "User's email address"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "password",
                        Type = "String",
                        Required = true,
                        Description = "User's hashed password"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "createdDate",
                        Type = "DateTime",
                        Required = true,
                        Description = "Account creation date"
                    }
                },
                Created = DateTime.UtcNow.AddDays(-10),
                LastModified = DateTime.UtcNow.AddDays(-1),
                Version = "1.0",
                IntentMapping = new List<string> { "CreateUser", "RegisterUser" },
                Tags = new List<string> { "User", "Insert", "Create" }
            };
        }
        
        /// <summary>
        /// Gets a collection of sample validation issues
        /// </summary>
        /// <returns>Collection of validation issues</returns>
        public static IEnumerable<SqlValidationIssue> GetSampleValidationIssues()
        {
            yield return new SqlValidationIssue
            {
                Description = "SQL injection risk detected",
                Category = ValidationCategory.Security,
                Severity = ValidationSeverity.Critical,
                LineNumber = 1,
                Position = 35,
                Recommendation = "Use parameterized queries instead of concatenating user input"
            };
            
            yield return new SqlValidationIssue
            {
                Description = "Missing index on filtered column",
                Category = ValidationCategory.Performance,
                Severity = ValidationSeverity.Warning,
                LineNumber = 1,
                Position = 40,
                Recommendation = "Add an index to the column used in the WHERE clause"
            };
            
            yield return new SqlValidationIssue
            {
                Description = "SELECT * used which can impact performance",
                Category = ValidationCategory.Performance,
                Severity = ValidationSeverity.Info,
                LineNumber = 1,
                Position = 7,
                Recommendation = "Specify only the required columns"
            };
            
            yield return new SqlValidationIssue
            {
                Description = "Query contains table join without explicit JOIN syntax",
                Category = ValidationCategory.Syntax,
                Severity = ValidationSeverity.Warning,
                LineNumber = 1,
                Position = 20,
                Recommendation = "Use explicit JOIN syntax for better readability and maintainability"
            };
        }

        /// <summary>
        /// Gets a collection of SQL validation rule definitions
        /// </summary>
        /// <returns>Collection of rule definitions</returns>
        public static IEnumerable<SqlValidationRuleDefinition> GetSampleValidationRules()
        {
            yield return new SqlValidationRuleDefinition
            {
                Name = "NoSelectStar",
                Description = "Avoid using SELECT * in queries for production code",
                Category = "Performance",
                CategoryEnum = ValidationCategory.Performance,
                DefaultSeverity = ValidationSeverity.Warning,
                IsEnabled = true,
                DefaultRecommendation = "Specify only the columns you need"
            };
            
            yield return new SqlValidationRuleDefinition
            {
                Name = "NoMultipleStatements",
                Description = "Avoid using multiple statements in a single query (SQL injection risk)",
                Category = "Security",
                CategoryEnum = ValidationCategory.Security,
                DefaultSeverity = ValidationSeverity.Critical,
                IsEnabled = true,
                DefaultRecommendation = "Use separate queries for multiple operations"
            };
            
            yield return new SqlValidationRuleDefinition
            {
                Name = "RequireWhereClause",
                Description = "DELETE and UPDATE statements should include a WHERE clause",
                Category = "Security",
                CategoryEnum = ValidationCategory.Security,
                DefaultSeverity = ValidationSeverity.Critical,
                IsEnabled = true,
                DefaultRecommendation = "Always include a WHERE clause with DELETE and UPDATE"
            };

            yield return new SqlValidationRuleDefinition
            {
                Name = "AvoidDistinct",
                Description = "Avoid using DISTINCT unnecessarily as it impacts performance",
                Category = "Performance",
                CategoryEnum = ValidationCategory.Performance,
                DefaultSeverity = ValidationSeverity.Info,
                IsEnabled = true,
                DefaultRecommendation = "Consider alternatives to DISTINCT for better performance"
            };
        }
        
        /// <summary>
        /// Gets sample query optimization suggestions
        /// </summary>
        /// <returns>Collection of optimization suggestions</returns>
        public static IEnumerable<QueryOptimizationSuggestion> GetSampleOptimizationSuggestions()
        {
            yield return new QueryOptimizationSuggestion
            {
                OriginalQuery = "SELECT * FROM Products WHERE Category = 'Electronics'",
                OptimizedQuery = "SELECT Id, Name, Price FROM Products WHERE Category = 'Electronics'",
                Description = "Reduced columns to only those needed",
                ImprovementEstimate = 15
            };
            
            yield return new QueryOptimizationSuggestion
            {
                OriginalQuery = "SELECT Orders.*, Customers.* FROM Orders, Customers WHERE Orders.CustomerId = Customers.Id",
                OptimizedQuery = "SELECT o.Id, o.OrderDate, o.Total, c.Name, c.Email FROM Orders o INNER JOIN Customers c ON o.CustomerId = c.Id",
                Description = "Changed implicit join to explicit join and reduced columns",
                ImprovementEstimate = 25
            };
            
            yield return new QueryOptimizationSuggestion
            {
                OriginalQuery = "SELECT COUNT(*) FROM Products",
                OptimizedQuery = "SELECT COUNT(1) FROM Products",
                Description = "Used COUNT(1) instead of COUNT(*)",
                ImprovementEstimate = 5
            };
        }
    }
} 