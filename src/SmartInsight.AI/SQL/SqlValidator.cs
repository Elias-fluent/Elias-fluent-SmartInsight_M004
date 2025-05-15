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
    /// Implementation of the SQL validator for security and performance checks
    /// </summary>
    public class SqlValidator : ISqlValidator
    {
        private readonly ILogger<SqlValidator> _logger;
        private readonly ISqlSanitizer _sqlSanitizer;
        
        // Performance-related patterns to check
        private static readonly Dictionary<string, string> _performancePatterns = new Dictionary<string, string>
        {
            { "SELECT *", "Using SELECT * can impact performance" },
            { @"\bCOUNT\(\*\)", "Using COUNT(*) on large tables may impact performance" },
            { @"ORDER BY\s+\d+", "Using positional ORDER BY can lead to unexpected results" },
            { @"GROUP BY\s+\d+", "Using positional GROUP BY can lead to unexpected results" },
            { @"SELECT(?=(?:.(?!WHERE))*$)", "SELECT query without a WHERE clause may return too many rows" },
            { @"FROM\s+\w+\s+a\s*,\s*\w+\s+b\s+(?!WHERE)", "JOIN without WHERE condition may result in Cartesian product" },
            { @"SELECT.+LEFT\s+JOIN.+LEFT\s+JOIN.+LEFT\s+JOIN", "Multiple LEFT JOINs may impact performance" }
        };
        
        /// <summary>
        /// Creates a new SqlValidator
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sqlSanitizer">SQL Sanitizer for injection protection</param>
        public SqlValidator(
            ILogger<SqlValidator> logger,
            ISqlSanitizer sqlSanitizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqlSanitizer = sqlSanitizer ?? throw new ArgumentNullException(nameof(sqlSanitizer));
        }
        
        /// <inheritdoc />
        public async Task<SqlValidationResult> ValidateSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true
            };
            
            // Check for empty SQL
            if (string.IsNullOrWhiteSpace(sql))
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "SQL query cannot be empty",
                    Category = ValidationCategory.Security,
                    Severity = ValidationSeverity.Critical
                });
                return result;
            }
            
            // First check security issues
            var securityResult = await ValidateSecurityAsync(sql, parameters, cancellationToken);
            if (!securityResult.IsValid)
            {
                // If security checks fail, don't bother with performance checks
                return securityResult;
            }
            
            // Then check performance issues
            var performanceResult = await ValidatePerformanceAsync(sql, parameters, cancellationToken);
            
            // Merge the results
            result.Issues.AddRange(securityResult.Issues);
            result.Issues.AddRange(performanceResult.Issues);
            result.IsValid = result.Issues.All(i => i.Severity != ValidationSeverity.Critical);
            
            return result;
        }
        
        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSecurityAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true
            };
            
            try
            {
                // Check for injection patterns in the SQL itself
                if (_sqlSanitizer.ContainsSqlInjectionPatterns(sql))
                {
                    result.IsValid = false;
                    result.Issues.Add(new SqlValidationIssue
                    {
                        Description = "SQL query contains potential injection patterns",
                        Category = ValidationCategory.Security,
                        Severity = ValidationSeverity.Critical,
                        Recommendation = "Use parameterized queries instead of string concatenation"
                    });
                }
                
                // Check for multiple statements (e.g., statement1; statement2)
                if (Regex.IsMatch(sql, @";\s*\w", RegexOptions.IgnoreCase))
                {
                    result.IsValid = false;
                    result.Issues.Add(new SqlValidationIssue
                    {
                        Description = "SQL query contains multiple statements",
                        Category = ValidationCategory.Security,
                        Severity = ValidationSeverity.Critical,
                        Recommendation = "Execute one statement at a time"
                    });
                }
                
                // Validate string parameters for injection patterns
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        if (param.Value is string strValue && !string.IsNullOrEmpty(strValue))
                        {
                            if (_sqlSanitizer.ContainsSqlInjectionPatterns(strValue))
                            {
                                result.IsValid = false;
                                result.Issues.Add(new SqlValidationIssue
                                {
                                    Description = $"Parameter '{param.Key}' contains potential SQL injection patterns",
                                    Category = ValidationCategory.Security,
                                    Severity = ValidationSeverity.Critical,
                                    Recommendation = "Remove SQL syntax from parameter value"
                                });
                            }
                        }
                    }
                }
                
                // Check for dangerous keywords
                foreach (var keyword in new[] { "DROP", "TRUNCATE", "ALTER", "CREATE", "MODIFY", "RENAME" })
                {
                    if (Regex.IsMatch(sql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
                    {
                        result.IsValid = false;
                        result.Issues.Add(new SqlValidationIssue
                        {
                            Description = $"SQL query contains potentially dangerous keyword: {keyword}",
                            Category = ValidationCategory.Security,
                            Severity = ValidationSeverity.Critical,
                            Recommendation = "DDL operations are not allowed through this interface"
                        });
                    }
                }
                
                // Check for stored procedure execution attempts
                if (Regex.IsMatch(sql, @"\bEXEC(UTE)?\b", RegexOptions.IgnoreCase))
                {
                    result.IsValid = false;
                    result.Issues.Add(new SqlValidationIssue
                    {
                        Description = "SQL query attempts to execute a stored procedure",
                        Category = ValidationCategory.Security,
                        Severity = ValidationSeverity.Critical,
                        Recommendation = "Stored procedure execution is not allowed through this interface"
                    });
                }
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL security validation: {SqlQuery}", sql);
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = $"Error during SQL security validation: {ex.Message}",
                    Category = ValidationCategory.Security,
                    Severity = ValidationSeverity.Critical,
                    Recommendation = "Review SQL syntax and parameter values"
                });
                
                return Task.FromResult(result);
            }
        }
        
        /// <inheritdoc />
        public Task<SqlValidationResult> ValidatePerformanceAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true
            };
            
            try
            {
                // Check against known performance anti-patterns
                foreach (var pattern in _performancePatterns)
                {
                    if (Regex.IsMatch(sql, pattern.Key, RegexOptions.IgnoreCase))
                    {
                        result.Issues.Add(new SqlValidationIssue
                        {
                            Description = pattern.Value,
                            Category = ValidationCategory.Performance,
                            Severity = ValidationSeverity.Warning,
                            Recommendation = "Consider optimizing your query"
                        });
                    }
                }
                
                // For SELECT queries, check if they would return a large result set
                if (IsSelectQuery(sql) && !HasRowLimitClause(sql))
                {
                    result.Issues.Add(new SqlValidationIssue
                    {
                        Description = "Query may return a large result set without a row limit",
                        Category = ValidationCategory.Performance,
                        Severity = ValidationSeverity.Warning,
                        Recommendation = "Consider adding TOP, LIMIT, or OFFSET/FETCH clauses for pagination"
                    });
                }
                
                // If no critical issues, the result is still valid even with warnings
                result.IsValid = result.Issues.All(i => i.Severity != ValidationSeverity.Critical);
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL performance validation: {SqlQuery}", sql);
                
                // For performance issues, we still consider the result valid with a warning
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = $"Error during SQL performance validation: {ex.Message}",
                    Category = ValidationCategory.Performance,
                    Severity = ValidationSeverity.Warning,
                    Recommendation = "Review SQL syntax"
                });
                
                return Task.FromResult(result);
            }
        }
        
        /// <inheritdoc />
        public async Task<SqlValidationResult> ValidateTemplateAsync(
            SqlTemplate template, 
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true
            };
            
            if (template == null)
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "SQL template cannot be null",
                    Category = ValidationCategory.Security,
                    Severity = ValidationSeverity.Critical
                });
                return result;
            }
            
            // Check template SQL text
            if (string.IsNullOrWhiteSpace(template.SqlTemplateText))
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "SQL template text cannot be empty",
                    Category = ValidationCategory.Security,
                    Severity = ValidationSeverity.Critical
                });
                return result;
            }
            
            // Validate the raw SQL for security issues
            var securityResult = await ValidateSecurityAsync(template.SqlTemplateText, null, cancellationToken);
            if (!securityResult.IsValid)
            {
                return securityResult;
            }
            
            // Perform a structural validation of the template
            // Check for required parameters in the template
            var usedParams = ExtractParametersFromSql(template.SqlTemplateText);
            var definedParams = template.Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            // Check for parameters used in SQL but not defined in template
            var undefinedParams = usedParams.Where(p => !definedParams.Contains(p)).ToList();
            if (undefinedParams.Any())
            {
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = $"SQL template uses parameters that are not defined: {string.Join(", ", undefinedParams)}",
                    Category = ValidationCategory.Security,
                    Severity = ValidationSeverity.Warning,
                    Recommendation = "Add these parameters to the template definition"
                });
            }
            
            // Check for parameters defined in template but not used in SQL
            var unusedParams = definedParams.Where(p => !usedParams.Contains(p)).ToList();
            if (unusedParams.Any())
            {
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = $"SQL template defines parameters that are not used: {string.Join(", ", unusedParams)}",
                    Category = ValidationCategory.Performance,
                    Severity = ValidationSeverity.Warning,
                    Recommendation = "Remove unused parameters from the template definition"
                });
            }
            
            // For SELECT operations, check the full table scan setting
            if (IsSelectQuery(template.SqlTemplateText) && !template.AllowFullTableScan && !HasWhereClause(template.SqlTemplateText))
            {
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "SELECT query without WHERE clause performs a full table scan, which is not allowed by template policy",
                    Category = ValidationCategory.Performance,
                    Severity = ValidationSeverity.Critical,
                    Recommendation = "Add a WHERE clause or set AllowFullTableScan to true if this is intentional"
                });
                result.IsValid = false;
            }
            
            // Validate parameters against the template's parameter definitions
            foreach (var param in template.Parameters)
            {
                if (param.Required && param.DefaultValue == null)
                {
                    result.Issues.Add(new SqlValidationIssue
                    {
                        Description = $"Required parameter '{param.Name}' does not have a default value",
                        Category = ValidationCategory.Security,
                        Severity = ValidationSeverity.Warning,
                        Recommendation = "Either provide a default value or ensure the parameter is always supplied"
                    });
                }
            }
            
            // If we have critical issues, mark the template as invalid
            if (result.Issues.Any(i => i.Severity == ValidationSeverity.Critical))
            {
                result.IsValid = false;
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public async Task<bool> IsSqlSafeAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var result = await ValidateSecurityAsync(sql, parameters, cancellationToken);
            return result.IsValid;
        }
        
        /// <summary>
        /// Extracts parameter names from SQL text (parameters in format @name or ${name})
        /// </summary>
        /// <param name="sql">The SQL to parse</param>
        /// <returns>Set of parameter names</returns>
        private HashSet<string> ExtractParametersFromSql(string sql)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Match parameters like @name
            var paramPattern1 = @"@(\w+)";
            var matches1 = Regex.Matches(sql, paramPattern1);
            foreach (Match match in matches1)
            {
                result.Add(match.Groups[1].Value);
            }
            
            // Match template parameters like ${name}
            var paramPattern2 = @"\$\{(\w+)\}";
            var matches2 = Regex.Matches(sql, paramPattern2);
            foreach (Match match in matches2)
            {
                result.Add(match.Groups[1].Value);
            }
            
            return result;
        }
        
        /// <summary>
        /// Checks if the SQL is a SELECT query
        /// </summary>
        /// <param name="sql">The SQL query</param>
        /// <returns>True if it's a SELECT query</returns>
        private bool IsSelectQuery(string sql)
        {
            return Regex.IsMatch(sql, @"^\s*SELECT", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL has a WHERE clause
        /// </summary>
        /// <param name="sql">The SQL query</param>
        /// <returns>True if it has a WHERE clause</returns>
        private bool HasWhereClause(string sql)
        {
            return Regex.IsMatch(sql, @"\bWHERE\b", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Checks if the SQL has a row limiting clause (TOP, LIMIT, FETCH)
        /// </summary>
        /// <param name="sql">The SQL query</param>
        /// <returns>True if it has a row limiting clause</returns>
        private bool HasRowLimitClause(string sql)
        {
            return Regex.IsMatch(sql, @"\bTOP\b|\bLIMIT\b|\bFETCH\s+FIRST\b", RegexOptions.IgnoreCase);
        }
    }
} 