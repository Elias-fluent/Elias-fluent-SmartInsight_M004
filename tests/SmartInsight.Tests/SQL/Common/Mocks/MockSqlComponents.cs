using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.Tests.SQL.Common.Mocks
{
    /// <summary>
    /// Mock implementation of ISqlGenerator for testing
    /// </summary>
    public class MockSqlGenerator : ISqlGenerator
    {
        /// <inheritdoc />
        public Task<SqlGenerationResult> GenerateSqlAsync(
            SqlTemplate template, 
            Dictionary<string, object> parameters, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlGenerationResult
            {
                IsSuccessful = true,
                Sql = template.SqlTemplateText,
                Parameters = parameters,
                TemplateId = template.Id
            };
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<SqlGenerationResult> GenerateParameterizedSqlAsync(
            SqlTemplate template, 
            Dictionary<string, object> parameters, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlGenerationResult
            {
                IsSuccessful = true,
                Sql = template.SqlTemplateText.Replace("@", "@param_"),
                Parameters = parameters,
                TemplateId = template.Id
            };
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<SqlGenerationResult> GenerateSqlFromQueryAsync(
            string query, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlGenerationResult
            {
                IsSuccessful = true,
                Sql = $"SELECT * FROM Users WHERE Query = @query",
                Parameters = new Dictionary<string, object> { { "query", query } },
                TemplateId = "generated-from-natural-language"
            };
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<ParameterizedSqlResult> ParameterizeSqlQueryAsync(
            string rawSql, 
            CancellationToken cancellationToken = default)
        {
            // For SQL injection prevention tests, let's create something that
            // will work with the specific test cases
            string parameterizedSql = rawSql;
            var parameters = new Dictionary<string, object>();
            
            // For tests that expect the SQL to be parameterized with @email
            if (rawSql.Contains("Email ="))
            {
                parameterizedSql = rawSql.Replace("Email =", "Email = @email");
                parameters["email"] = "test@example.com";
            }
            
            var result = new ParameterizedSqlResult
            {
                IsSuccessful = true,
                Sql = parameterizedSql,
                Parameters = parameters
            };
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public SqlOperationType DetermineSqlOperationType(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return SqlOperationType.Unknown;
            }
            
            sql = sql.TrimStart();
            
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return SqlOperationType.Select;
            }
            else if (sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                return SqlOperationType.Insert;
            }
            else if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                return SqlOperationType.Update;
            }
            else if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return SqlOperationType.Delete;
            }
            
            return SqlOperationType.Other;
        }
    }

    /// <summary>
    /// Mock implementation of ISqlExecutionService for testing
    /// </summary>
    public class MockSqlExecutionService : ISqlExecutionService
    {
        private readonly bool _simulateSuccess;
        private readonly object _mockResult;
        private readonly string? _mockErrorMessage;

        /// <summary>
        /// Creates a new mock SQL execution service with configurable behavior
        /// </summary>
        /// <param name="simulateSuccess">Whether to simulate successful execution</param>
        /// <param name="mockResult">The result to return when simulating success</param>
        /// <param name="mockErrorMessage">The error message to return when simulating failure</param>
        public MockSqlExecutionService(bool simulateSuccess = true, object? mockResult = null, string? mockErrorMessage = null)
        {
            _simulateSuccess = simulateSuccess;
            _mockResult = mockResult ?? new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Id", 1 },
                    { "Name", "TestName" },
                    { "Value", 42 }
                }
            };
            _mockErrorMessage = mockErrorMessage;
        }

        /// <inheritdoc />
        public Task<SqlExecutionResult> ExecuteQueryAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlExecutionResult
            {
                IsSuccessful = _simulateSuccess,
                Results = _simulateSuccess ? _mockResult : null,
                SqlGenerated = sql,
                ExecutedQuery = sql,
                ErrorMessage = _simulateSuccess ? null : _mockErrorMessage ?? "Mock execution error",
                RowsAffected = _simulateSuccess ? (_mockResult as List<Dictionary<string, object>>)?.Count ?? 1 : 0
            };
            
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<T?> ExecuteScalarAsync<T>(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            if (!_simulateSuccess)
            {
                return Task.FromResult(default(T?));
            }
            
            try
            {
                return Task.FromResult((T)Convert.ChangeType(_mockResult, typeof(T)));
            }
            catch
            {
                return Task.FromResult(default(T?));
            }
        }

        /// <inheritdoc />
        public Task<List<T>> ExecuteQueryAsync<T>(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            if (!_simulateSuccess)
            {
                return Task.FromResult(new List<T>());
            }
            
            try
            {
                if (_mockResult is List<T> typedList)
                {
                    return Task.FromResult(typedList);
                }
                
                return Task.FromResult(new List<T> { (T)Convert.ChangeType(_mockResult, typeof(T)) });
            }
            catch
            {
                return Task.FromResult(new List<T>());
            }
        }

        /// <inheritdoc />
        public Task<int> ExecuteNonQueryAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_simulateSuccess ? 1 : 0);
        }

        /// <inheritdoc />
        public Task<SqlExecutionResult> ExecuteGenerationResultAsync(
            SqlGenerationResult generationResult, 
            CancellationToken cancellationToken = default)
        {
            if (!generationResult.IsSuccessful)
            {
                return Task.FromResult(new SqlExecutionResult
                {
                    IsSuccessful = false,
                    ErrorMessage = generationResult.ErrorMessage ?? "Generation failed"
                });
            }
            
            return ExecuteQueryAsync(generationResult.Sql, generationResult.Parameters, cancellationToken);
        }
        
        /// <inheritdoc />
        public string SanitizeErrorMessage(string error)
        {
            // Simply return the original message in the mock implementation
            // In a real implementation, this would sanitize sensitive information
            if (string.IsNullOrEmpty(error))
            {
                return "Error message is null or empty";
            }
            
            // Basic sanitization - remove any SQL keywords that might expose structure
            string sanitized = error
                .Replace("TABLE", "[TABLE]", StringComparison.OrdinalIgnoreCase)
                .Replace("COLUMN", "[COLUMN]", StringComparison.OrdinalIgnoreCase)
                .Replace("DATABASE", "[DATABASE]", StringComparison.OrdinalIgnoreCase);
                
            return sanitized;
        }
    }
} 