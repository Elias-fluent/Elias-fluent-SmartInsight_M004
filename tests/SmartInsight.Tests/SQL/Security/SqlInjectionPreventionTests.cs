using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Security
{
    /// <summary>
    /// Security tests for SQL injection prevention
    /// </summary>
    public class SqlInjectionPreventionTests : SqlTestBase
    {
        private readonly ISqlValidator _sqlValidator;
        private readonly ISqlSanitizer _sqlSanitizer;
        private readonly ISqlGenerator _sqlGenerator;
        private readonly IParameterValidator _parameterValidator;

        public SqlInjectionPreventionTests(ITestOutputHelper output) : base(output)
        {
            _sqlValidator = _serviceProvider.GetRequiredService<ISqlValidator>();
            _sqlSanitizer = _serviceProvider.GetRequiredService<ISqlSanitizer>();
            _sqlGenerator = _serviceProvider.GetRequiredService<ISqlGenerator>();
            _parameterValidator = _serviceProvider.GetRequiredService<IParameterValidator>();
        }

        [Theory]
        [InlineData("'; DROP TABLE Users; --")]
        [InlineData("1 OR 1=1")]
        [InlineData("admin' --")]
        [InlineData("' UNION SELECT username, password FROM Users; --")]
        [InlineData("' OR 'x'='x")]
        [InlineData("; EXEC xp_cmdshell('net user');")]
        [InlineData("1; WAITFOR DELAY '0:0:10'")]
        [InlineData("1); DROP TABLE Users; --")]
        [InlineData("' OR username IS NOT NULL; --")]
        [InlineData("'; INSERT INTO Logs (Message) VALUES ('Hacked'); --")]
        public async Task DirectSqlInjectionAttempts_AreDetected(string injectionPayload)
        {
            // Arrange
            string unsafeQuery = $"SELECT * FROM Users WHERE Username = '{injectionPayload}'";
            
            // Act
            var validationResult = await _sqlValidator.ValidateSecurityAsync(unsafeQuery);
            bool isSafe = await _sqlValidator.IsSqlSafeAsync(unsafeQuery);
            
            // Assert
            Assert.False(isSafe, $"SQL injection not detected: {injectionPayload}");
            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Issues, issue => 
                issue.Category == ValidationCategory.Security && 
                issue.Severity == ValidationSeverity.Critical);
            
            _output.WriteLine($"Injection payload: {injectionPayload}");
            foreach (var issue in validationResult.Issues.Where(i => i.Category == ValidationCategory.Security))
            {
                _output.WriteLine($"Security issue: {issue.Description} (Severity: {issue.Severity})");
            }
        }

        [Theory]
        [InlineData("test@example.com'; DROP TABLE Users; --")]
        [InlineData("admin' OR 1=1 --")]
        [InlineData("' UNION SELECT * FROM INFORMATION_SCHEMA.TABLES; --")]
        [InlineData("Smith' OR 'x'='x")]
        [InlineData("test@example.com\"; DELETE FROM Users; --")]
        public async Task ParameterizedSqlPreventsSqlInjection(string injectionPayload)
        {
            // Arrange
            var template = CreateSampleTemplate(
                "security-test",
                "Security Test Template",
                "SELECT * FROM Users WHERE Email = @email");
            
            var parameters = new Dictionary<string, object>
            {
                { "email", injectionPayload }
            };
            
            // Act
            var generationResult = await _sqlGenerator.GenerateParameterizedSqlAsync(template, parameters);
            var parameterizedResult = await _sqlSanitizer.ParameterizeSqlAsync(generationResult.Sql, parameters);
            
            // Assert
            Assert.True(generationResult.IsSuccessful);
            Assert.NotNull(parameterizedResult);
            
            // The parameterized SQL should still be using the parameter placeholder
            Assert.Contains("@email", parameterizedResult.ParameterizedSql);
            
            // But the injection payload should not appear directly in the SQL
            Assert.DoesNotContain(injectionPayload, parameterizedResult.ParameterizedSql);
            
            _output.WriteLine($"Injection payload: {injectionPayload}");
            _output.WriteLine($"Original SQL: {generationResult.Sql}");
            _output.WriteLine($"Parameterized SQL: {parameterizedResult.ParameterizedSql}");
            
            // The parameter value should be properly stored in the parameters dictionary
            Assert.True(parameterizedResult.Parameters.ContainsKey("@email"));
            Assert.Equal(injectionPayload, parameterizedResult.Parameters["@email"]);
        }

        [Fact]
        public async Task EscapeSqlValue_PreventsSqlInjection()
        {
            // Arrange
            var injectionPayloads = new[]
            {
                "'; DROP TABLE Users; --",
                "1 OR 1=1",
                "admin' --",
                "' UNION SELECT username, password FROM Users; --"
            };
            
            foreach (var payload in injectionPayloads)
            {
                // Act
                string escaped = _sqlSanitizer.EscapeSqlValue(payload);
                
                // Assert
                // Ensure single quotes are escaped
                Assert.DoesNotContain("';", escaped);
                
                // Check that the escaped value is different from the original if it contained SQL injection
                if (payload.Contains("'") || payload.Contains(";"))
                {
                    Assert.NotEqual(payload, escaped);
                }
                
                // Use the escaped value in a query and ensure it's safe
                string query = $"SELECT * FROM Users WHERE Username = '{escaped}'";
                bool isSafe = await _sqlValidator.IsSqlSafeAsync(query);
                
                // The query using the escaped value should be considered safe
                Assert.True(isSafe, $"Escaped value still vulnerable: {escaped}");
                
                _output.WriteLine($"Original: '{payload}'");
                _output.WriteLine($"Escaped: '{escaped}'");
                _output.WriteLine($"Safe query: {query}");
                _output.WriteLine(new string('-', 50));
            }
        }

        [Fact]
        public void SanitizeSqlIdentifier_PreventsSqlInjection()
        {
            // Arrange
            var maliciousIdentifiers = new[]
            {
                "Users; DROP TABLE Logs",
                "Users--",
                "Users/*comment*/",
                "Users' OR '1'='1",
                "[Users]; EXEC xp_cmdshell('format c:')"
            };
            
            foreach (var identifier in maliciousIdentifiers)
            {
                // Act
                string sanitized = _sqlSanitizer.SanitizeSqlIdentifier(identifier);
                
                // Assert
                // Ensure no SQL control characters remain
                Assert.DoesNotContain(";", sanitized);
                Assert.DoesNotContain("--", sanitized);
                Assert.DoesNotContain("/*", sanitized);
                Assert.DoesNotContain("*/", sanitized);
                Assert.DoesNotContain("'", sanitized);
                Assert.DoesNotContain("EXEC", sanitized, StringComparison.OrdinalIgnoreCase);
                
                // The sanitized identifier should be different from malicious ones
                if (identifier.Contains(";") || identifier.Contains("--") || 
                    identifier.Contains("/*") || identifier.Contains("'"))
                {
                    Assert.NotEqual(identifier, sanitized);
                }
                
                _output.WriteLine($"Original: '{identifier}'");
                _output.WriteLine($"Sanitized: '{sanitized}'");
                _output.WriteLine(new string('-', 50));
            }
        }

        [Fact]
        public async Task SanitizeSqlQuery_RemovesDangerousElements()
        {
            // Arrange
            var maliciousQueries = new[]
            {
                "SELECT * FROM Users; DROP TABLE Logs;",
                "SELECT * FROM Users UNION SELECT username, password FROM Admins",
                "SELECT * FROM Users WHERE id = 1 OR 1=1",
                "SELECT * FROM Users; EXEC xp_cmdshell('net user')",
                "SELECT * FROM Users; WAITFOR DELAY '0:0:10'"
            };
            
            foreach (var query in maliciousQueries)
            {
                // Act
                string sanitized = _sqlSanitizer.SanitizeSqlQuery(query);
                var validationResult = await _sqlValidator.ValidateSecurityAsync(sanitized);
                
                // Assert
                // The sanitized query should not contain the dangerous elements
                if (query.Contains(";"))
                {
                    Assert.DoesNotContain(";", sanitized);
                }
                if (query.Contains("UNION", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.DoesNotContain("UNION", sanitized, StringComparison.OrdinalIgnoreCase);
                }
                if (query.Contains("EXEC", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.DoesNotContain("EXEC", sanitized, StringComparison.OrdinalIgnoreCase);
                }
                if (query.Contains("xp_", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.DoesNotContain("xp_", sanitized, StringComparison.OrdinalIgnoreCase);
                }
                if (query.Contains("WAITFOR", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.DoesNotContain("WAITFOR", sanitized, StringComparison.OrdinalIgnoreCase);
                }
                
                // The sanitized query should no longer have critical security issues
                Assert.False(validationResult.HasSecurityIssues(), 
                    $"Sanitized query still has security issues: {sanitized}");
                
                _output.WriteLine($"Original: '{query}'");
                _output.WriteLine($"Sanitized: '{sanitized}'");
                _output.WriteLine(new string('-', 50));
            }
        }

        [Fact]
        public async Task ParameterValidatorDetectsInjectionAttempts()
        {
            // Arrange
            var template = CreateSampleTemplate(
                "security-test",
                "Security Test Template",
                "SELECT * FROM Users WHERE Username = @username");
            
            template.Parameters[0].Name = "username";
            template.Parameters[0].Type = "String";
            
            var injectionPayloads = new[]
            {
                "'; DROP TABLE Users; --",
                "admin' OR 1=1 --",
                "' UNION SELECT username, password FROM Users; --",
                "Smith' OR 'x'='x"
            };
            
            foreach (var payload in injectionPayloads)
            {
                var parameters = new Dictionary<string, ExtractedParameter>
                {
                    { 
                        "username", 
                        new ExtractedParameter 
                        { 
                            Name = "username", 
                            Value = payload, 
                            Type = "String", 
                            Confidence = 0.9 
                        } 
                    }
                };
                
                // Act
                var validationResult = await _parameterValidator.ValidateParametersAsync(parameters, template);
                
                // Assert
                Assert.False(validationResult.IsValid, $"Injection not detected: {payload}");
                Assert.Contains(validationResult.Issues, issue => 
                    issue.ParameterName == "username" && 
                    issue.Severity == ValidationSeverity.Critical);
                
                _output.WriteLine($"Injection payload: {payload}");
                foreach (var issue in validationResult.Issues)
                {
                    _output.WriteLine($"- {issue.ParameterName}: {issue.Issue} (Severity: {issue.Severity})");
                }
                _output.WriteLine(new string('-', 50));
            }
        }
    }
} 