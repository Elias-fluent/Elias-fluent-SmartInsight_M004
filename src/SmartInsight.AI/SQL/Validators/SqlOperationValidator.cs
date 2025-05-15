using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Validators
{
    /// <summary>
    /// Specialized validator for SQL operation types (SELECT, INSERT, UPDATE, DELETE)
    /// </summary>
    public class SqlOperationValidator
    {
        private readonly IParameterValidator _baseValidator;
        private readonly ILogger<SqlOperationValidator> _logger;
        
        /// <summary>
        /// Creates a new instance of SqlOperationValidator
        /// </summary>
        /// <param name="baseValidator">The base parameter validator</param>
        /// <param name="logger">Logger instance</param>
        public SqlOperationValidator(
            IParameterValidator baseValidator,
            ILogger<SqlOperationValidator> logger)
        {
            _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates parameters for SQL operations
        /// </summary>
        /// <param name="parameters">The parameters to validate</param>
        /// <param name="template">The SQL template</param>
        /// <param name="operationType">The type of SQL operation</param>
        /// <returns>The validation result</returns>
        public async Task<Models.ParameterValidationResult> ValidateSqlOperationParametersAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template,
            SqlOperationType operationType)
        {
            // Start with base validation
            var result = await _baseValidator.ValidateParametersAsync(parameters, template);
            
            // If already invalid, no need for additional validation
            if (!result.IsValid)
            {
                return result;
            }
            
            // Apply operation-specific validations
            switch (operationType)
            {
                case SqlOperationType.Select:
                    await ValidateSelectOperationAsync(parameters, template, result);
                    break;
                    
                case SqlOperationType.Insert:
                    await ValidateInsertOperationAsync(parameters, template, result);
                    break;
                    
                case SqlOperationType.Update:
                    await ValidateUpdateOperationAsync(parameters, template, result);
                    break;
                    
                case SqlOperationType.Delete:
                    await ValidateDeleteOperationAsync(parameters, template, result);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown SQL operation type: {OperationType}", operationType);
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates parameters for SELECT operations
        /// </summary>
        private Task<Models.ParameterValidationResult> ValidateSelectOperationAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template,
            Models.ParameterValidationResult result)
        {
            // Check for parameters that might cause performance issues
            if (parameters.TryGetValue("limit", out var limitParam) && limitParam.Value is int limitValue)
            {
                // Check for reasonable LIMIT values to prevent performance issues
                if (limitValue > 1000)
                {
                    result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                    {
                        ParameterName = "limit",
                        RuleName = "Performance.ExcessiveLimit",
                        Description = $"LIMIT value of {limitValue} is excessively high",
                        Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                        OriginalValue = limitValue,
                        Recommendation = "Use a lower limit value (≤1000) to avoid performance issues"
                    });
                }
            }
            
            // Check for filtering parameters to prevent full table scans
            bool hasFilterParam = parameters.Any(p => IsLikelyFilterParameter(p.Key));
            if (!hasFilterParam && !template.AllowFullTableScan)
            {
                result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                {
                    ParameterName = string.Empty,
                    RuleName = "Performance.NoFilter",
                    Description = "SELECT operation has no filtering parameters, which may cause full table scan",
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                    Recommendation = "Add filter parameters to limit the result set"
                });
            }

            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Validates parameters for INSERT operations
        /// </summary>
        private Task<Models.ParameterValidationResult> ValidateInsertOperationAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template,
            Models.ParameterValidationResult result)
        {
            // Check for required parameters for INSERT
            var requiredInsertFields = new[] { "data", "values", "record" };
            bool hasRequiredField = requiredInsertFields.Any(field => 
                parameters.Keys.Any(p => p.Contains(field, StringComparison.OrdinalIgnoreCase)));
                
            if (!hasRequiredField)
            {
                result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                {
                    ParameterName = string.Empty,
                    RuleName = "Validation.MissingInsertData",
                    Description = "INSERT operation is missing data parameters",
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "Provide data values for the INSERT operation"
                });
            }
            
            // Validate data integrity for INSERT operations
            foreach (var param in parameters)
            {
                if (param.Key.EndsWith("id", StringComparison.OrdinalIgnoreCase) && 
                    param.Value.Value is Guid guidValue && 
                    guidValue == Guid.Empty)
                {
                    result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                    {
                        ParameterName = param.Key,
                        RuleName = "DataIntegrity.EmptyGuid",
                        Description = $"Empty GUID value for '{param.Key}' parameter",
                        Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                        OriginalValue = guidValue,
                        Recommendation = "Provide a valid GUID value"
                    });
                }
            }

            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Validates parameters for UPDATE operations
        /// </summary>
        private Task<Models.ParameterValidationResult> ValidateUpdateOperationAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template,
            Models.ParameterValidationResult result)
        {
            // Check for WHERE clause parameters (critical for UPDATE)
            bool hasWhereParam = parameters.Any(p => IsLikelyFilterParameter(p.Key));
            if (!hasWhereParam)
            {
                result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                {
                    ParameterName = string.Empty,
                    RuleName = "Security.UnfilteredUpdate",
                    Description = "UPDATE operation has no filtering parameters, which may affect all rows",
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "Add WHERE clause parameters to limit affected rows"
                });
            }
            
            // Check for SET clause parameters
            bool hasSetParam = parameters.Any(p => !IsLikelyFilterParameter(p.Key));
            if (!hasSetParam)
            {
                result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                {
                    ParameterName = string.Empty,
                    RuleName = "Validation.MissingUpdateData",
                    Description = "UPDATE operation is missing data to update",
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "Provide values to update"
                });
            }

            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Validates parameters for DELETE operations
        /// </summary>
        private Task<Models.ParameterValidationResult> ValidateDeleteOperationAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template,
            Models.ParameterValidationResult result)
        {
            // DELETE operations should always have filter parameters
            bool hasWhereParam = parameters.Any(p => IsLikelyFilterParameter(p.Key));
            if (!hasWhereParam)
            {
                result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                {
                    ParameterName = string.Empty,
                    RuleName = "Security.UnfilteredDelete",
                    Description = "DELETE operation has no filtering parameters, which may affect all rows",
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Critical,
                    Recommendation = "Add WHERE clause parameters to limit affected rows"
                });
            }
            
            // Check if the DELETE operation is trying to delete by a non-primary key
            bool hasPrimaryKeyFilter = parameters.Any(p => 
                p.Key.EndsWith("id", StringComparison.OrdinalIgnoreCase) && 
                !p.Key.Contains("foreign", StringComparison.OrdinalIgnoreCase));
                
            if (!hasPrimaryKeyFilter && hasWhereParam)
            {
                result.AddIssue(new SmartInsight.AI.SQL.Models.ParameterValidationIssue
                {
                    ParameterName = string.Empty,
                    RuleName = "Performance.NonKeyDelete",
                    Description = "DELETE operation uses non-primary key filters",
                    Severity = SmartInsight.AI.SQL.Models.ValidationSeverity.Warning,
                    Recommendation = "Consider using primary key for DELETE operations"
                });
            }

            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Determines if a parameter is likely to be used in a WHERE clause
        /// </summary>
        private bool IsLikelyFilterParameter(string paramName)
        {
            var filterKeywords = new[] 
            { 
                "id", "key", "where", "filter", "condition", 
                "equals", "contains", "starts", "ends", "min", "max",
                "before", "after", "from", "to", "greater", "less",
                "at", "on", "between", "in"
            };
            
            return filterKeywords.Any(keyword => 
                paramName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
} 