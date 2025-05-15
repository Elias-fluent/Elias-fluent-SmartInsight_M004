using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Extension methods for parameter validation
    /// </summary>
    public static class ParameterValidatorExtensions
    {
        /// <summary>
        /// Adds a validation issue to the result if the issue is not null
        /// </summary>
        /// <param name="result">The validation result to add the issue to</param>
        /// <param name="issue">The validation issue to add</param>
        /// <returns>The same validation result for method chaining</returns>
        public static Models.ParameterValidationResult AddIssueIfPresent(
            this Models.ParameterValidationResult result, 
            ParameterValidationIssue? issue)
        {
            if (issue != null)
            {
                result.AddIssue(issue);
            }
            
            return result;
        }

        /// <summary>
        /// Adds multiple validation issues to the result
        /// </summary>
        /// <param name="result">The validation result to add the issues to</param>
        /// <param name="issues">The validation issues to add</param>
        /// <returns>The same validation result for method chaining</returns>
        public static Models.ParameterValidationResult AddIssues(
            this Models.ParameterValidationResult result, 
            IEnumerable<ParameterValidationIssue> issues)
        {
            foreach (var issue in issues)
            {
                result.AddIssue(issue);
            }
            
            return result;
        }
        
        /// <summary>
        /// Runs custom validation logic and adds any resulting issues to the validation result
        /// </summary>
        /// <param name="result">The validation result to add issues to</param>
        /// <param name="validationFunc">The validation function that returns validation issues</param>
        /// <returns>The same validation result for method chaining</returns>
        public static async Task<Models.ParameterValidationResult> WithCustomValidationAsync(
            this Models.ParameterValidationResult result,
            Func<Task<IEnumerable<ParameterValidationIssue>>> validationFunc)
        {
            var issues = await validationFunc();
            return result.AddIssues(issues);
        }
        
        /// <summary>
        /// Validates a parameter against all default rules
        /// </summary>
        /// <param name="validator">The parameter validator</param>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="templateParameter">The template parameter definition</param>
        /// <param name="result">The validation result to add issues to</param>
        /// <returns>The validation result with any issues added</returns>
        public static async Task<Models.ParameterValidationResult> ValidateAllDefaultRulesAsync(
            this IParameterValidator validator,
            ExtractedParameter parameter,
            SqlTemplateParameter templateParameter,
            Models.ParameterValidationResult result)
        {
            // Common default validation rules
            string[] defaultRules = new[]
            {
                "Type.Invalid",
                "Confidence.Low",
                "Range.Numeric",
                "Range.Date",
                "Format.StringLength"
            };
            
            // Security rules for strings
            if (templateParameter.Type == "String")
            {
                defaultRules = defaultRules.Concat(new[] 
                {
                    "Security.SqlInjection",
                    "Content.Inappropriate"
                }).ToArray();
            }
            
            // Validate against all default rules
            foreach (var ruleName in defaultRules)
            {
                var issue = await validator.ValidateParameterAsync(parameter, templateParameter, ruleName);
                result.AddIssueIfPresent(issue);
            }
            
            return result;
        }
    }
} 