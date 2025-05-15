using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Implementation of IParameterValidator that validates parameters against security and business rules
    /// </summary>
    public class ParameterValidator : IParameterValidator
    {
        private readonly ILogger<ParameterValidator> _logger;
        private readonly Dictionary<string, ValidationRuleDefinition> _rules;
        private readonly Dictionary<string, Func<ExtractedParameter, SqlTemplateParameter, Task<ParameterValidationIssue?>>> _ruleHandlers;

        /// <summary>
        /// Creates a new instance of ParameterValidator
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public ParameterValidator(ILogger<ParameterValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rules = new Dictionary<string, ValidationRuleDefinition>();
            _ruleHandlers = new Dictionary<string, Func<ExtractedParameter, SqlTemplateParameter, Task<ParameterValidationIssue?>>>();
            
            RegisterDefaultRules();
        }

        /// <inheritdoc />
        public async Task<Models.ParameterValidationResult> ValidateParametersAsync(
            Dictionary<string, ExtractedParameter> parameters, 
            SqlTemplate template)
        {
            _logger.LogDebug("Validating {Count} parameters against template {TemplateId}", 
                parameters.Count, template.TemplateId);

            var result = new Models.ParameterValidationResult
            {
                IsValid = true
            };

            // Check for missing required parameters
            foreach (var param in template.Parameters.Where(p => p.Required))
            {
                if (!parameters.ContainsKey(param.Name))
                {
                    var issue = new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Required.Missing",
                        Description = $"Required parameter '{param.Name}' is missing",
                        Severity = ValidationSeverity.Critical,
                        Recommendation = $"Provide a value for the {param.Name} parameter"
                    };
                    
                    result.AddIssue(issue);
                }
            }

            // Validate each parameter against all applicable rules
            foreach (var paramEntry in parameters)
            {
                var paramName = paramEntry.Key;
                var paramValue = paramEntry.Value;
                
                // Find the template parameter definition
                var templateParam = template.Parameters.FirstOrDefault(p => p.Name == paramName);
                if (templateParam == null)
                {
                    // Parameter not defined in template
                    var issue = new ParameterValidationIssue
                    {
                        ParameterName = paramName,
                        RuleName = "Parameter.Unknown",
                        Description = $"Parameter '{paramName}' is not defined in the template",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = paramValue.Value,
                        Recommendation = "Remove this parameter or use only parameters defined in the template"
                    };
                    
                    result.AddIssue(issue);
                    continue;
                }
                
                // Apply all applicable rules
                foreach (var rule in _rules.Values.Where(r => IsRuleApplicableToParameter(r, templateParam)))
                {
                    if (!rule.IsEnabled)
                    {
                        continue;
                    }
                    
                    if (_ruleHandlers.TryGetValue(rule.Name, out var handler))
                    {
                        try
                        {
                            var issue = await handler(paramValue, templateParam);
                            if (issue != null)
                            {
                                result.AddIssue(issue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing validation rule {RuleName} for parameter {ParamName}", 
                                rule.Name, paramName);
                            
                            // Add a validation issue for the rule execution failure
                            var issue = new ParameterValidationIssue
                            {
                                ParameterName = paramName,
                                RuleName = "Rule.ExecutionError",
                                Description = $"Error executing validation rule '{rule.Name}': {ex.Message}",
                                Severity = ValidationSeverity.Warning,
                                OriginalValue = paramValue.Value
                            };
                            
                            result.AddIssue(issue);
                        }
                    }
                }
            }

            _logger.LogDebug("Parameter validation completed with {IssueCount} issues. IsValid: {IsValid}", 
                result.ValidationIssues.Count, result.IsValid);
            
            return result;
        }

        /// <inheritdoc />
        public async Task<ParameterValidationIssue?> ValidateParameterAsync(
            ExtractedParameter parameter,
            SqlTemplateParameter templateParameter,
            string ruleName)
        {
            if (!_rules.TryGetValue(ruleName, out var rule))
            {
                _logger.LogWarning("Validation rule {RuleName} not found", ruleName);
                return null;
            }
            
            if (!IsRuleApplicableToParameter(rule, templateParameter))
            {
                _logger.LogDebug("Rule {RuleName} is not applicable to parameter of type {ParamType}", 
                    ruleName, templateParameter.Type);
                return null;
            }
            
            if (!_ruleHandlers.TryGetValue(ruleName, out var handler))
            {
                _logger.LogWarning("No handler found for validation rule {RuleName}", ruleName);
                return null;
            }
            
            try
            {
                return await handler(parameter, templateParameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing validation rule {RuleName} for parameter {ParamName}", 
                    ruleName, parameter.Name);
                
                return new ParameterValidationIssue
                {
                    ParameterName = parameter.Name,
                    RuleName = "Rule.ExecutionError",
                    Description = $"Error executing validation rule '{ruleName}': {ex.Message}",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = parameter.Value
                };
            }
        }

        /// <inheritdoc />
        public void RegisterValidationRule(
            string ruleName,
            string ruleDescription,
            ValidationSeverity severity,
            IEnumerable<string>? applicableTypes = null)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                throw new ArgumentException("Rule name cannot be null or empty", nameof(ruleName));
            }
            
            if (_rules.ContainsKey(ruleName))
            {
                throw new ArgumentException($"Rule with name '{ruleName}' already exists", nameof(ruleName));
            }
            
            var rule = new ValidationRuleDefinition
            {
                Name = ruleName,
                Description = ruleDescription,
                Severity = severity,
                ApplicableTypes = applicableTypes?.ToList() ?? new List<string>(),
                IsEnabled = true
            };
            
            _rules.Add(ruleName, rule);
            _logger.LogInformation("Registered validation rule {RuleName}", ruleName);
        }

        /// <inheritdoc />
        public IReadOnlyList<ValidationRuleDefinition> GetAvailableRules()
        {
            return _rules.Values.ToList();
        }
        
        /// <summary>
        /// Registers a handler for a validation rule
        /// </summary>
        /// <param name="ruleName">The name of the rule</param>
        /// <param name="handler">The handler function that implements the rule</param>
        public void RegisterRuleHandler(
            string ruleName, 
            Func<ExtractedParameter, SqlTemplateParameter, Task<ParameterValidationIssue?>> handler)
        {
            if (!_rules.ContainsKey(ruleName))
            {
                throw new ArgumentException($"Rule with name '{ruleName}' not found", nameof(ruleName));
            }
            
            _ruleHandlers[ruleName] = handler ?? throw new ArgumentNullException(nameof(handler));
            _logger.LogInformation("Registered handler for validation rule {RuleName}", ruleName);
        }
        
        /// <summary>
        /// Enables or disables a validation rule
        /// </summary>
        /// <param name="ruleName">The name of the rule</param>
        /// <param name="isEnabled">Whether the rule should be enabled</param>
        public void SetRuleEnabled(string ruleName, bool isEnabled)
        {
            if (!_rules.TryGetValue(ruleName, out var rule))
            {
                throw new ArgumentException($"Rule with name '{ruleName}' not found", nameof(ruleName));
            }
            
            rule.IsEnabled = isEnabled;
            _logger.LogInformation("Set validation rule {RuleName} enabled: {IsEnabled}", ruleName, isEnabled);
        }
        
        private bool IsRuleApplicableToParameter(ValidationRuleDefinition rule, SqlTemplateParameter parameter)
        {
            // If the rule has no specific types, it applies to all
            if (rule.ApplicableTypes.Count == 0)
            {
                return true;
            }
            
            // Check if the parameter type is in the applicable types list
            return rule.ApplicableTypes.Contains(parameter.Type, StringComparer.OrdinalIgnoreCase);
        }
        
        private void RegisterDefaultRules()
        {
            // Required parameter check
            RegisterValidationRule(
                "Required.Missing",
                "Checks if required parameters are provided",
                ValidationSeverity.Critical);
                
            RegisterRuleHandler("Required.Missing", (param, template) => 
                Task.FromResult<ParameterValidationIssue?>(template.Required && param == null
                    ? new ParameterValidationIssue
                    {
                        ParameterName = template.Name,
                        RuleName = "Required.Missing",
                        Description = $"Required parameter '{template.Name}' is missing",
                        Severity = ValidationSeverity.Critical,
                        Recommendation = $"Provide a value for the {template.Name} parameter"
                    }
                    : null));
            
            // Type compatibility check
            RegisterValidationRule(
                "Type.Invalid",
                "Checks if parameter value is compatible with the expected type",
                ValidationSeverity.Critical);
                
            RegisterRuleHandler("Type.Invalid", (param, template) =>
            {
                if (param.Value == null)
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                bool isCompatible = IsTypeCompatible(param.Value, template.Type);
                return Task.FromResult<ParameterValidationIssue?>(!isCompatible
                    ? new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Type.Invalid",
                        Description = $"Value '{param.Value}' is not compatible with type {template.Type}",
                        Severity = ValidationSeverity.Critical,
                        OriginalValue = param.Value,
                        Recommendation = $"Provide a value of type {template.Type}"
                    }
                    : null);
            });
            
            // Low confidence check
            RegisterValidationRule(
                "Confidence.Low",
                "Checks if parameter extraction confidence is above threshold",
                ValidationSeverity.Warning);
                
            RegisterRuleHandler("Confidence.Low", (param, template) =>
            {
                const double DEFAULT_CONFIDENCE_THRESHOLD = 0.7;
                return Task.FromResult<ParameterValidationIssue?>(param.Confidence < DEFAULT_CONFIDENCE_THRESHOLD
                    ? new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Confidence.Low",
                        Description = $"Low confidence ({param.Confidence:P0}) in extracted value for parameter '{param.Name}'",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = param.Confidence,
                        Recommendation = "Verify this parameter value is correct or provide a more specific value"
                    }
                    : null);
            });
            
            // SQL Injection protection
            RegisterValidationRule(
                "Security.SqlInjection",
                "Checks for potential SQL injection patterns in string parameters",
                ValidationSeverity.Critical,
                new[] { "String" });
                
            RegisterRuleHandler("Security.SqlInjection", (param, template) =>
            {
                if (param.Value == null || !(param.Value is string strValue))
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                // Check for common SQL injection patterns
                var sqlInjectionPatterns = new[]
                {
                    @";\s*--",                          // Inline comment
                    @";\s*\/\*.*?\*\/",                 // Block comment
                    @"UNION\s+ALL\s+SELECT",            // UNION injection
                    @"OR\s+['""]?\d+['""]?\s*=\s*['""]?\d+['""]?", // OR 1=1
                    @"DROP\s+TABLE",                    // DROP TABLE
                    @"DELETE\s+FROM",                   // DELETE FROM
                    @"INSERT\s+INTO",                   // INSERT INTO
                    @"EXEC\s*\(",                       // EXEC(
                    @"EXECUTE\s*\("                     // EXECUTE(
                };
                
                foreach (var pattern in sqlInjectionPatterns)
                {
                    if (Regex.IsMatch(strValue, pattern, RegexOptions.IgnoreCase))
                    {
                        return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                        {
                            ParameterName = param.Name,
                            RuleName = "Security.SqlInjection",
                            Description = $"Potential SQL injection detected in parameter '{param.Name}'",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = strValue,
                            Recommendation = "Remove SQL syntax from parameter value"
                        });
                    }
                }
                
                return Task.FromResult<ParameterValidationIssue?>(null);
            });
            
            // Email format validation
            RegisterValidationRule(
                "Format.Email",
                "Validates email address format",
                ValidationSeverity.Warning,
                new[] { "String" });
                
            RegisterRuleHandler("Format.Email", (param, template) =>
            {
                if (param.Value == null || !(param.Value is string strValue))
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                // Use a simple regex for email validation
                // For a production application, consider a more robust solution
                string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                bool isValid = Regex.IsMatch(strValue, emailPattern);
                
                return Task.FromResult<ParameterValidationIssue?>(!isValid
                    ? new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Format.Email",
                        Description = $"Invalid email format for parameter '{param.Name}'",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = strValue,
                        Recommendation = "Provide a valid email address (e.g., user@example.com)"
                    }
                    : null);
            });
            
            // URL format validation
            RegisterValidationRule(
                "Format.Url",
                "Validates URL format",
                ValidationSeverity.Warning,
                new[] { "String" });
                
            RegisterRuleHandler("Format.Url", (param, template) =>
            {
                if (param.Value == null || !(param.Value is string strValue))
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                // Check if it's a valid URI
                bool isValid = Uri.TryCreate(strValue, UriKind.Absolute, out Uri? uriResult) 
                               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                
                return Task.FromResult<ParameterValidationIssue?>(!isValid
                    ? new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Format.Url",
                        Description = $"Invalid URL format for parameter '{param.Name}'",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = strValue,
                        Recommendation = "Provide a valid URL (e.g., https://example.com)"
                    }
                    : null);
            });
            
            // Numeric range check
            RegisterValidationRule(
                "Range.Numeric",
                "Checks if numeric parameter is within reasonable range",
                ValidationSeverity.Warning,
                new[] { "Int32", "Int64", "Double", "Decimal", "Float" });
                
            RegisterRuleHandler("Range.Numeric", (param, template) =>
            {
                if (param.Value == null)
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                bool isOutOfRange = false;
                string rangeDescription = "";
                
                if (param.Value is int intValue)
                {
                    isOutOfRange = intValue < -1000000000 || intValue > 1000000000;
                    rangeDescription = "-1,000,000,000 to 1,000,000,000";
                }
                else if (param.Value is long longValue)
                {
                    isOutOfRange = longValue < -1000000000000 || longValue > 1000000000000;
                    rangeDescription = "-1,000,000,000,000 to 1,000,000,000,000";
                }
                else if (param.Value is double doubleValue)
                {
                    isOutOfRange = doubleValue < -1.0e12 || doubleValue > 1.0e12 || double.IsNaN(doubleValue) || double.IsInfinity(doubleValue);
                    rangeDescription = "-1.0e12 to 1.0e12 (not NaN or Infinity)";
                }
                else if (param.Value is decimal decimalValue)
                {
                    isOutOfRange = decimalValue < -1.0e12m || decimalValue > 1.0e12m;
                    rangeDescription = "-1.0e12 to 1.0e12";
                }
                else if (param.Value is float floatValue)
                {
                    isOutOfRange = floatValue < -1.0e12f || floatValue > 1.0e12f || float.IsNaN(floatValue) || float.IsInfinity(floatValue);
                    rangeDescription = "-1.0e12 to 1.0e12 (not NaN or Infinity)";
                }
                
                return Task.FromResult<ParameterValidationIssue?>(isOutOfRange
                    ? new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Range.Numeric",
                        Description = $"Numeric value '{param.Value}' is outside reasonable range for parameter '{param.Name}'",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = param.Value,
                        Recommendation = $"Provide a value within range {rangeDescription}"
                    }
                    : null);
            });
            
            // Date range check
            RegisterValidationRule(
                "Range.Date",
                "Checks if date parameter is within reasonable range",
                ValidationSeverity.Warning,
                new[] { "DateTime", "DateTimeOffset" });
                
            RegisterRuleHandler("Range.Date", (param, template) =>
            {
                if (param.Value == null)
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                var minDate = new DateTime(1900, 1, 1);
                var maxDate = DateTime.UtcNow.AddYears(100);
                bool isOutOfRange = false;
                DateTime valueDate = DateTime.MinValue;
                
                if (param.Value is DateTime dateValue)
                {
                    valueDate = dateValue;
                    isOutOfRange = dateValue < minDate || dateValue > maxDate;
                }
                else if (param.Value is DateTimeOffset offsetValue)
                {
                    valueDate = offsetValue.DateTime;
                    isOutOfRange = offsetValue.DateTime < minDate || offsetValue.DateTime > maxDate;
                }
                
                return Task.FromResult<ParameterValidationIssue?>(isOutOfRange
                    ? new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Range.Date",
                        Description = $"Date value '{valueDate:yyyy-MM-dd}' is outside reasonable range for parameter '{param.Name}'",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = param.Value,
                        Recommendation = $"Provide a date between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}"
                    }
                    : null);
            });
            
            // String length check
            RegisterValidationRule(
                "Format.StringLength",
                "Checks if string parameter length is within acceptable range",
                ValidationSeverity.Warning,
                new[] { "String" });
                
            RegisterRuleHandler("Format.StringLength", (param, template) =>
            {
                if (param.Value == null || !(param.Value is string strValue))
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                const int MIN_LENGTH = 1;
                const int MAX_LENGTH = 4000; // Reasonable max for SQL parameters
                
                bool isTooShort = strValue.Length < MIN_LENGTH;
                bool isTooLong = strValue.Length > MAX_LENGTH;
                
                if (isTooShort)
                {
                    return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Format.StringLength",
                        Description = $"String parameter '{param.Name}' is empty",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = strValue,
                        Recommendation = "Provide a non-empty string value"
                    });
                }
                
                if (isTooLong)
                {
                    return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                    {
                        ParameterName = param.Name,
                        RuleName = "Format.StringLength",
                        Description = $"String parameter '{param.Name}' is too long ({strValue.Length} characters)",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = strValue,
                        Recommendation = $"Provide a string value shorter than {MAX_LENGTH} characters"
                    });
                }
                
                return Task.FromResult<ParameterValidationIssue?>(null);
            });
            
            // RegEx pattern matching
            RegisterValidationRule(
                "Format.Pattern",
                "Checks if string parameter matches a regex pattern",
                ValidationSeverity.Warning,
                new[] { "String" });
                
            // This rule is registered but not automatically applied
            // It's intended to be used with custom parameter metadata
            
            // Profanity/inappropriate content check
            RegisterValidationRule(
                "Content.Inappropriate",
                "Checks for inappropriate content in string parameters",
                ValidationSeverity.Warning,
                new[] { "String" });
                
            RegisterRuleHandler("Content.Inappropriate", (param, template) =>
            {
                if (param.Value == null || !(param.Value is string strValue))
                {
                    return Task.FromResult<ParameterValidationIssue?>(null);
                }
                
                // Simple list of inappropriate terms
                // In a production system, this would be much more comprehensive and configurable
                var inappropriateTerms = new[] 
                { 
                    "profanity1", "profanity2", "profanity3", 
                    "inappropriate1", "inappropriate2", "inappropriate3" 
                };
                
                foreach (var term in inappropriateTerms)
                {
                    if (strValue.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                        {
                            ParameterName = param.Name,
                            RuleName = "Content.Inappropriate",
                            Description = $"Parameter '{param.Name}' contains inappropriate content",
                            Severity = ValidationSeverity.Warning,
                            OriginalValue = strValue,
                            Recommendation = "Remove inappropriate content from the parameter value"
                        });
                    }
                }
                
                return Task.FromResult<ParameterValidationIssue?>(null);
            });
            
            // Business rule check for value in specific list
            RegisterValidationRule(
                "Business.AllowedValues",
                "Checks if value is in list of allowed values",
                ValidationSeverity.Warning);
                
            // This rule is registered but not automatically applied
            // It's intended to be used with custom parameter metadata
        }
        
        private bool IsTypeCompatible(object value, string type)
        {
            // Simple compatibility check based on runtime type
            switch (type)
            {
                case "String":
                    return true; // All values can be strings
                case "Int32":
                    return value is int;
                case "Int64":
                    return value is long || value is int;
                case "Double":
                    return value is double || value is float || value is int;
                case "Decimal":
                    return value is decimal || value is double || value is float || value is int;
                case "Float":
                    return value is float || value is int;
                case "Boolean":
                    return value is bool;
                case "DateTime":
                    return value is DateTime;
                case "DateTimeOffset":
                    return value is DateTimeOffset || value is DateTime;
                case "TimeSpan":
                    return value is TimeSpan;
                case "Guid":
                    return value is Guid;
                default:
                    return true; // For unknown types, assume compatibility
            }
        }
        
        /// <summary>
        /// Validates a parameter against a regex pattern
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="pattern">The regex pattern to match against</param>
        /// <param name="errorMessage">Custom error message if validation fails</param>
        /// <param name="severity">Severity of the validation issue</param>
        /// <returns>A validation issue if validation fails, null otherwise</returns>
        public Task<ParameterValidationIssue?> ValidateRegexPatternAsync(
            ExtractedParameter parameter,
            string pattern,
            string? errorMessage = null,
            ValidationSeverity severity = ValidationSeverity.Warning)
        {
            if (parameter.Value == null || !(parameter.Value is string strValue))
            {
                return Task.FromResult<ParameterValidationIssue?>(null);
            }
            
            try
            {
                bool isValid = Regex.IsMatch(strValue, pattern);
                
                if (!isValid)
                {
                    return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                    {
                        ParameterName = parameter.Name,
                        RuleName = "Format.Pattern",
                        Description = errorMessage ?? $"Parameter '{parameter.Name}' does not match required pattern",
                        Severity = severity,
                        OriginalValue = strValue,
                        Recommendation = $"Provide a value that matches the pattern: {pattern}"
                    });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid regex pattern: {Pattern}", pattern);
                return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                {
                    ParameterName = parameter.Name,
                    RuleName = "Rule.Error",
                    Description = $"Error validating parameter '{parameter.Name}': Invalid regex pattern",
                    Severity = ValidationSeverity.Warning,
                    OriginalValue = pattern
                });
            }
            
            return Task.FromResult<ParameterValidationIssue?>(null);
        }
        
        /// <summary>
        /// Validates a parameter against a list of allowed values
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="allowedValues">The list of allowed values</param>
        /// <param name="ignoreCase">Whether to ignore case when comparing string values</param>
        /// <param name="errorMessage">Custom error message if validation fails</param>
        /// <param name="severity">Severity of the validation issue</param>
        /// <returns>A validation issue if validation fails, null otherwise</returns>
        public Task<ParameterValidationIssue?> ValidateAllowedValuesAsync(
            ExtractedParameter parameter,
            IEnumerable<object> allowedValues,
            bool ignoreCase = true,
            string? errorMessage = null,
            ValidationSeverity severity = ValidationSeverity.Warning)
        {
            if (parameter.Value == null)
            {
                return Task.FromResult<ParameterValidationIssue?>(null);
            }
            
            bool isValid = false;
            
            if (parameter.Value is string strValue && ignoreCase)
            {
                isValid = allowedValues.Any(v => 
                    v is string s && string.Equals(s, strValue, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                isValid = allowedValues.Any(v => v.Equals(parameter.Value));
            }
            
            if (!isValid)
            {
                var valuesStr = string.Join(", ", allowedValues.Select(v => v.ToString()));
                return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                {
                    ParameterName = parameter.Name,
                    RuleName = "Business.AllowedValues",
                    Description = errorMessage ?? $"Value '{parameter.Value}' for parameter '{parameter.Name}' is not in the list of allowed values",
                    Severity = severity,
                    OriginalValue = parameter.Value,
                    Recommendation = $"Provide one of the allowed values: {valuesStr}"
                });
            }
            
            return Task.FromResult<ParameterValidationIssue?>(null);
        }
        
        /// <summary>
        /// Validates a parameter against a number range
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="min">The minimum allowed value</param>
        /// <param name="max">The maximum allowed value</param>
        /// <param name="errorMessage">Custom error message if validation fails</param>
        /// <param name="severity">Severity of the validation issue</param>
        /// <returns>A validation issue if validation fails, null otherwise</returns>
        public Task<ParameterValidationIssue?> ValidateNumberRangeAsync(
            ExtractedParameter parameter,
            double min,
            double max,
            string? errorMessage = null,
            ValidationSeverity severity = ValidationSeverity.Warning)
        {
            if (parameter.Value == null)
            {
                return Task.FromResult<ParameterValidationIssue?>(null);
            }
            
            double? numericValue = null;
            
            if (parameter.Value is int intValue)
            {
                numericValue = intValue;
            }
            else if (parameter.Value is long longValue)
            {
                numericValue = longValue;
            }
            else if (parameter.Value is double doubleValue)
            {
                numericValue = doubleValue;
            }
            else if (parameter.Value is decimal decimalValue)
            {
                numericValue = (double)decimalValue;
            }
            else if (parameter.Value is float floatValue)
            {
                numericValue = floatValue;
            }
            
            if (numericValue.HasValue && (numericValue < min || numericValue > max))
            {
                return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                {
                    ParameterName = parameter.Name,
                    RuleName = "Range.Numeric",
                    Description = errorMessage ?? $"Value '{parameter.Value}' for parameter '{parameter.Name}' is outside the allowed range",
                    Severity = severity,
                    OriginalValue = parameter.Value,
                    Recommendation = $"Provide a value between {min} and {max}"
                });
            }
            
            return Task.FromResult<ParameterValidationIssue?>(null);
        }
        
        /// <summary>
        /// Validates a parameter against a string length range
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="minLength">The minimum allowed length</param>
        /// <param name="maxLength">The maximum allowed length</param>
        /// <param name="errorMessage">Custom error message if validation fails</param>
        /// <param name="severity">Severity of the validation issue</param>
        /// <returns>A validation issue if validation fails, null otherwise</returns>
        public Task<ParameterValidationIssue?> ValidateStringLengthAsync(
            ExtractedParameter parameter,
            int minLength = 0,
            int maxLength = int.MaxValue,
            string? errorMessage = null,
            ValidationSeverity severity = ValidationSeverity.Warning)
        {
            if (parameter.Value == null || !(parameter.Value is string strValue))
            {
                return Task.FromResult<ParameterValidationIssue?>(null);
            }
            
            if (strValue.Length < minLength)
            {
                return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                {
                    ParameterName = parameter.Name,
                    RuleName = "Format.StringLength",
                    Description = errorMessage ?? $"String parameter '{parameter.Name}' is too short ({strValue.Length} characters)",
                    Severity = severity,
                    OriginalValue = strValue,
                    Recommendation = $"Provide a string value with at least {minLength} characters"
                });
            }
            
            if (strValue.Length > maxLength)
            {
                return Task.FromResult<ParameterValidationIssue?>(new ParameterValidationIssue
                {
                    ParameterName = parameter.Name,
                    RuleName = "Format.StringLength",
                    Description = errorMessage ?? $"String parameter '{parameter.Name}' is too long ({strValue.Length} characters)",
                    Severity = severity,
                    OriginalValue = strValue,
                    Recommendation = $"Provide a string value with at most {maxLength} characters"
                });
            }
            
            return Task.FromResult<ParameterValidationIssue?>(null);
        }
    }
} 