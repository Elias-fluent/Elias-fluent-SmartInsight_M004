using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.Tests.SQL.Common.Mocks
{
    /// <summary>
    /// Mock implementation of ISqlValidationRulesEngine for testing
    /// </summary>
    public class MockSqlValidationRulesEngine : ISqlValidationRulesEngine
    {
        private readonly Dictionary<string, SqlValidationRuleDefinition> _rules = new Dictionary<string, SqlValidationRuleDefinition>();

        /// <summary>
        /// Creates a new mock SQL validation rules engine for testing
        /// </summary>
        public MockSqlValidationRulesEngine()
        {
            // Add some sample rules
            AddSampleRules();
        }

        /// <inheritdoc />
        public SqlValidationRuleDefinition? GetRule(string ruleName)
        {
            return _rules.TryGetValue(ruleName, out var rule) ? rule : null;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<SqlValidationRuleDefinition> GetAllRules()
        {
            return _rules.Values;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<SqlValidationRuleDefinition> GetRulesByCategory(SmartInsight.AI.SQL.Models.ValidationCategory category)
        {
            // Convert ValidationCategory to string for comparison with rule.Category
            string categoryString = category.ToString();
            return _rules.Values.Where(r => r.Category == categoryString).ToList();
        }

        /// <inheritdoc />
        public bool AddRule(SqlValidationRuleDefinition rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Name))
            {
                return false;
            }

            // For test compatibility, if ValidationFunction is not set,
            // add a dummy function that returns no issues
            if (rule.ValidationFunction == null)
            {
                rule.ValidationFunction = (_, __) => Task.FromResult(new List<SqlValidationIssue>());
            }

            _rules[rule.Name] = rule;
            return true;
        }

        /// <inheritdoc />
        public bool RemoveRule(string ruleName)
        {
            return _rules.Remove(ruleName);
        }

        /// <inheritdoc />
        public bool SetRuleEnabled(string ruleName, bool isEnabled)
        {
            if (_rules.TryGetValue(ruleName, out var rule))
            {
                rule.IsEnabled = isEnabled;
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null, 
            IEnumerable<SmartInsight.AI.SQL.Models.ValidationCategory>? categories = null,
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true,
                Issues = new List<SqlValidationIssue>()
            };

            if (string.IsNullOrWhiteSpace(sql))
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "SQL query cannot be empty",
                    Category = SmartInsight.AI.SQL.Models.ValidationCategory.Syntax,
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical
                });
                return Task.FromResult(result);
            }

            // Mock implementation - always return valid for tests unless specific cases
            if (sql.Contains("--") || sql.Contains("DROP") || sql.Contains("DELETE"))
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "SQL query contains potentially dangerous content",
                    Category = SmartInsight.AI.SQL.Models.ValidationCategory.Security,
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "Remove dangerous statements"
                });
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateTemplateAsync(
            SqlTemplate template, 
            IEnumerable<SmartInsight.AI.SQL.Models.ValidationCategory>? categories = null,
            CancellationToken cancellationToken = default)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            // Delegate to ValidateSqlAsync with template SQL
            return ValidateSqlAsync(template.SqlTemplateText, null, categories, cancellationToken);
        }

        /// <inheritdoc />
        public SqlValidationRuleSet CreateRuleSet(string name, string description, IEnumerable<string> ruleNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Rule set name cannot be null or empty", nameof(name));
            }

            if (ruleNames == null)
            {
                throw new ArgumentNullException(nameof(ruleNames));
            }

            var ruleSet = new SqlValidationRuleSet
            {
                Name = name,
                Description = description ?? "Custom rule set",
                Created = DateTime.UtcNow
            };

            // Add existing rules to the set
            foreach (var ruleName in ruleNames)
            {
                if (_rules.ContainsKey(ruleName))
                {
                    ruleSet.RuleNames.Add(ruleName);
                }
            }

            return ruleSet;
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ApplyRuleSetAsync(
            SqlValidationRuleSet ruleSet, 
            string sql, 
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            // In the mock, just return a successful validation
            var result = new SqlValidationResult
            {
                IsValid = true,
                Issues = new List<SqlValidationIssue>()
            };

            return Task.FromResult(result);
        }

        /// <summary>
        /// Adds sample rules for testing
        /// </summary>
        private void AddSampleRules()
        {
            // Sample security rule
            AddRule(new SqlValidationRuleDefinition
            {
                Name = "Security.SqlInjection",
                Description = "Detects potential SQL injection patterns in queries",
                Category = "Security",
                DefaultSeverity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                ValidationFunction = (sql, parameters) =>
                {
                    var issues = new List<SqlValidationIssue>();
                    if (sql.Contains("'") || sql.Contains("--"))
                    {
                        issues.Add(new SqlValidationIssue
                        {
                            Description = "Potential SQL injection detected",
                            Category = SmartInsight.AI.SQL.Models.ValidationCategory.Security,
                            Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                            Recommendation = "Use parameterized queries"
                        });
                    }
                    return Task.FromResult(issues);
                }
            });

            // Sample performance rule
            AddRule(new SqlValidationRuleDefinition
            {
                Name = "Performance.SelectAll",
                Description = "Detects use of SELECT * which can impact performance",
                Category = "Performance",
                DefaultSeverity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                ValidationFunction = (sql, parameters) =>
                {
                    var issues = new List<SqlValidationIssue>();
                    if (sql.Contains("SELECT *"))
                    {
                        issues.Add(new SqlValidationIssue
                        {
                            Description = "Using SELECT * can impact performance",
                            Category = SmartInsight.AI.SQL.Models.ValidationCategory.Performance,
                            Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                            Recommendation = "Specify only the columns you need"
                        });
                    }
                    return Task.FromResult(issues);
                }
            });
        }
    }

    /// <summary>
    /// Mock implementation of ISqlValidator for testing
    /// </summary>
    public class MockSqlValidator : ISqlValidator
    {
        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSecurityAsync(
            string sql, 
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true,
                Issues = new List<SqlValidationIssue>()
            };

            // Simple security check for tests
            if (sql.Contains("'") || sql.Contains("--") || sql.Contains(";"))
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "Potential SQL injection pattern detected",
                    Category = SmartInsight.AI.SQL.Models.ValidationCategory.Security,
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "Use parameterized queries and avoid dynamic SQL"
                });
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSyntaxAsync(
            string sql,
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true,
                Issues = new List<SqlValidationIssue>()
            };

            // Basic syntax check for tests
            if (!sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                !sql.Trim().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) &&
                !sql.Trim().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) &&
                !sql.Trim().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "Invalid SQL syntax",
                    Category = SmartInsight.AI.SQL.Models.ValidationCategory.Syntax,
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "SQL must start with a valid statement type"
                });
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidatePerformanceAsync(
            string sql,
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true,
                Issues = new List<SqlValidationIssue>()
            };

            // Performance check for tests
            if (sql.Contains("SELECT *"))
            {
                result.Issues.Add(new SqlValidationIssue
                {
                    Description = "Using SELECT * can impact performance",
                    Category = SmartInsight.AI.SQL.Models.ValidationCategory.Performance,
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                    Recommendation = "Specify only the columns you need"
                });
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var result = new SqlValidationResult
            {
                IsValid = true,
                Issues = new List<SqlValidationIssue>()
            };

            // Combine all validations for tests
            var securityTask = ValidateSecurityAsync(sql, parameters, cancellationToken);
            var syntaxTask = ValidateSyntaxAsync(sql, cancellationToken);
            var performanceTask = ValidatePerformanceAsync(sql, cancellationToken);

            Task.WaitAll(securityTask, syntaxTask, performanceTask);

            result.Issues.AddRange(securityTask.Result.Issues);
            result.Issues.AddRange(syntaxTask.Result.Issues);
            result.Issues.AddRange(performanceTask.Result.Issues);

            result.IsValid = !result.Issues.Any(i => i.Severity == SmartInsight.AI.SQL.Models.ValidationSeverity.Critical);

            return Task.FromResult(result);
        }
        
        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateSqlAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            // Same as ValidateAsync for mock implementation
            return ValidateAsync(sql, parameters, cancellationToken);
        }
        
        /// <inheritdoc />
        public Task<SqlValidationResult> ValidatePerformanceAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            // Add parameters overload for mock implementation
            return ValidatePerformanceAsync(sql, cancellationToken);
        }
        
        /// <inheritdoc />
        public Task<SqlValidationResult> ValidateTemplateAsync(
            SqlTemplate template,
            CancellationToken cancellationToken = default)
        {
            // For mock, just validate the template SQL
            return ValidateSqlAsync(template.SqlTemplateText, null, cancellationToken);
        }
        
        /// <inheritdoc />
        public Task<bool> IsSqlSafeAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            // Quick check for obvious SQL injection patterns
            bool isSafe = !sql.Contains("--") && 
                          !sql.Contains(";") && 
                          !sql.Contains("'") &&
                          !sql.Contains("EXEC") &&
                          !sql.Contains("xp_") &&
                          !sql.Contains("DROP") &&
                          !sql.Contains("ALTER");
                          
            return Task.FromResult(isSafe);
        }
    }

    /// <summary>
    /// Mock implementation of IParameterValidator for testing
    /// </summary>
    public class MockParameterValidator : IParameterValidator
    {
        private readonly Dictionary<string, SmartInsight.AI.SQL.Interfaces.ValidationRuleDefinition> _rules = new Dictionary<string, SmartInsight.AI.SQL.Interfaces.ValidationRuleDefinition>();
        
        /// <summary>
        /// Creates a new mock parameter validator
        /// </summary>
        public MockParameterValidator()
        {
            // Add some sample validation rules
            RegisterValidationRule("Required.Missing", "Validates required parameters are present", SmartInsight.AI.SQL.Models.ValidationSeverity.Critical);
            RegisterValidationRule("Type.Invalid", "Validates parameter types", SmartInsight.AI.SQL.Models.ValidationSeverity.Error);
            RegisterValidationRule("Confidence.Low", "Validates parameter confidence levels", SmartInsight.AI.SQL.Models.ValidationSeverity.Warning);
            RegisterValidationRule("Business.AllowedValues", "Validates parameter allowed values", SmartInsight.AI.SQL.Models.ValidationSeverity.Error);
        }
        
        /// <inheritdoc />
        public Task<SmartInsight.AI.SQL.Models.ParameterValidationResult> ValidateParametersAsync(
            Dictionary<string, ExtractedParameter> parameters, 
            SqlTemplate template)
        {
            var result = new SmartInsight.AI.SQL.Models.ParameterValidationResult { IsValid = true };

            // Check for required parameters
            foreach (var templateParam in template.Parameters)
            {
                if (templateParam.Required && 
                    (!parameters.ContainsKey(templateParam.Name) || 
                    parameters[templateParam.Name].Value == null))
                {
                    result.IsValid = false;
                    result.MissingParameters.Add(templateParam.Name);
                    result.ValidationIssues.Add(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                    {
                        ParameterName = templateParam.Name,
                        RuleName = "Required.Missing",
                        Description = $"Required parameter '{templateParam.Name}' is missing",
                        Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                        Recommendation = "Provide a value for this required parameter"
                    });
                }
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue?> ValidateParameterAsync(
            ExtractedParameter parameter,
            SqlTemplateParameter templateParameter,
            string ruleName)
        {
            // Check for specific rule violations
            switch (ruleName)
            {
                case "Required.Missing":
                    if (parameter.Value == null && templateParameter.Required)
                    {
                        return Task.FromResult<SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue?>(new SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue
                        {
                            ParameterName = parameter.Name,
                            RuleName = ruleName,
                            Description = $"Required parameter '{parameter.Name}' is missing",
                            Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                            OriginalValue = parameter.Value,
                            Recommendation = "Provide a value for this required parameter"
                        });
                    }
                    break;
                    
                case "Type.Invalid":
                    if (parameter.Value != null && parameter.Type != templateParameter.Type)
                    {
                        return Task.FromResult<SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue?>(new SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue
                        {
                            ParameterName = parameter.Name,
                            RuleName = ruleName,
                            Description = $"Parameter '{parameter.Name}' has incorrect type (expected {templateParameter.Type})",
                            Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Error,
                            OriginalValue = parameter.Value,
                            Recommendation = $"Provide a value of type {templateParameter.Type}"
                        });
                    }
                    break;
                    
                case "Confidence.Low":
                    if (parameter.Confidence < 0.7)
                    {
                        return Task.FromResult<SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue?>(new SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue
                        {
                            ParameterName = parameter.Name,
                            RuleName = ruleName,
                            Description = $"Low confidence ({parameter.Confidence:P0}) for parameter '{parameter.Name}'",
                            Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                            OriginalValue = parameter.Confidence,
                            Recommendation = "Verify this parameter value is correct"
                        });
                    }
                    break;
            }
            
            return Task.FromResult<SmartInsight.AI.SQL.Interfaces.ParameterValidationIssue?>(null);
        }

        /// <inheritdoc />
        public void RegisterValidationRule(
            string ruleName, 
            string ruleDescription, 
            SmartInsight.AI.SQL.Models.ValidationSeverity severity, 
            IEnumerable<string>? applicableTypes = null)
        {
            _rules[ruleName] = new SmartInsight.AI.SQL.Interfaces.ValidationRuleDefinition
            {
                Name = ruleName,
                Description = ruleDescription,
                Severity = severity,
                ApplicableTypes = (applicableTypes ?? Enumerable.Empty<string>()).ToList(),
                IsEnabled = true
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<SmartInsight.AI.SQL.Interfaces.ValidationRuleDefinition> GetAvailableRules()
        {
            return _rules.Values.ToList();
        }
    }
} 