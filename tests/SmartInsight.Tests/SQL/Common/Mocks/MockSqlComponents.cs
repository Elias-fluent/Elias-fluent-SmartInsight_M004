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
        private readonly bool _simulateSuccess;
        private readonly string _mockSql;
        private readonly string? _mockErrorMessage;

        /// <summary>
        /// Creates a new mock SQL generator with configurable behavior
        /// </summary>
        /// <param name="simulateSuccess">Whether to simulate successful generation</param>
        /// <param name="mockSql">The SQL to return when simulating success</param>
        /// <param name="mockErrorMessage">The error message to return when simulating failure</param>
        public MockSqlGenerator(bool simulateSuccess = true, string mockSql = "SELECT * FROM TestTable", string? mockErrorMessage = null)
        {
            _simulateSuccess = simulateSuccess;
            _mockSql = mockSql;
            _mockErrorMessage = mockErrorMessage;
        }

        /// <inheritdoc />
        public Task<SqlGenerationResult> GenerateSqlAsync(
            SqlTemplate template, 
            Dictionary<string, object> parameters, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlGenerationResult
            {
                IsSuccessful = _simulateSuccess,
                Sql = _simulateSuccess ? _mockSql : string.Empty,
                ErrorMessage = _simulateSuccess ? null : _mockErrorMessage ?? "Mock error",
                TemplateId = template.Id,
                Parameters = parameters
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
                IsSuccessful = _simulateSuccess,
                Sql = _simulateSuccess ? _mockSql.Replace("TestTable", "TestTable WHERE Id = @id") : string.Empty,
                ErrorMessage = _simulateSuccess ? null : _mockErrorMessage ?? "Mock error",
                TemplateId = template.Id,
                Parameters = parameters
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
                IsSuccessful = _simulateSuccess,
                Sql = _simulateSuccess ? $"-- Generated from: {query}\n{_mockSql}" : string.Empty,
                ErrorMessage = _simulateSuccess ? null : _mockErrorMessage ?? "Mock error",
                Parameters = new Dictionary<string, object>()
            };

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public SqlOperationType DetermineSqlOperationType(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return SqlOperationType.Unknown;
                
            sql = sql.TrimStart();
            
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Select;
            if (sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Insert;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Update;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                return SqlOperationType.Delete;
                
            return SqlOperationType.Unknown;
        }
    }

    /// <summary>
    /// Mock implementation of ISqlValidator for testing
    /// </summary>
    public class MockSqlValidator : ISqlValidator
    {
        private readonly bool _mockIsValid;
        private readonly List<SqlValidationIssue> _mockIssues;

        /// <summary>
        /// Creates a new mock SQL validator with configurable behavior
        /// </summary>
        /// <param name="mockIsValid">Whether to simulate valid SQL</param>
        /// <param name="mockIssues">Validation issues to return</param>
        public MockSqlValidator(bool mockIsValid = true, List<SqlValidationIssue>? mockIssues = null)
        {
            _mockIsValid = mockIsValid;
            _mockIssues = mockIssues ?? new List<SqlValidationIssue>();
            
            if (!_mockIsValid && _mockIssues.Count == 0)
            {
                _mockIssues.Add(new SqlValidationIssue
                {
                    Description = "Mock validation issue",
                    Category = ValidationCategory.Security,
                    Severity = ValidationSeverity.Critical,
                    LineNumber = 1,
                    Position = 0,
                    Recommendation = "Fix the issue"
                });
            }
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = _mockIsValid,
                Issues = new List<SqlValidationIssue>(_mockIssues)
            };
            
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSecurityAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var securityIssues = _mockIssues
                .FindAll(i => i.Category == ValidationCategory.Security)
                .ConvertAll(i => i);
                
            var result = new SqlValidationResult
            {
                IsValid = _mockIsValid || securityIssues.Count == 0,
                Issues = securityIssues
            };
            
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidatePerformanceAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var performanceIssues = _mockIssues
                .FindAll(i => i.Category == ValidationCategory.Performance)
                .ConvertAll(i => i);
                
            var result = new SqlValidationResult
            {
                IsValid = _mockIsValid || performanceIssues.Count == 0,
                Issues = performanceIssues
            };
            
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateTemplateAsync(
            SqlTemplate template, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = _mockIsValid,
                Issues = new List<SqlValidationIssue>(_mockIssues)
            };
            
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<bool> IsSqlSafeAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_mockIsValid);
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
    }
} 