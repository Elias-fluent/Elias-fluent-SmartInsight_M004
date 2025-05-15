using System.Collections.Generic;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for validating extracted parameters against security and business rules
    /// </summary>
    public interface IParameterValidator
    {
        /// <summary>
        /// Validates a set of parameters against all applicable rules
        /// </summary>
        /// <param name="parameters">The parameters to validate</param>
        /// <param name="template">The SQL template requiring these parameters</param>
        /// <returns>A validation result containing any validation issues</returns>
        Task<Models.ParameterValidationResult> ValidateParametersAsync(
            Dictionary<string, ExtractedParameter> parameters, 
            SqlTemplate template);

        /// <summary>
        /// Validates a single parameter against a specific rule
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="templateParameter">The template parameter definition</param>
        /// <param name="ruleName">The name of the rule to validate against</param>
        /// <returns>A validation result for this specific parameter</returns>
        Task<ParameterValidationIssue?> ValidateParameterAsync(
            ExtractedParameter parameter,
            SqlTemplateParameter templateParameter,
            string ruleName);

        /// <summary>
        /// Registers a custom validation rule
        /// </summary>
        /// <param name="ruleName">The unique name of the rule</param>
        /// <param name="ruleDescription">Human-readable description of the rule</param>
        /// <param name="severity">The severity level if this rule is violated</param>
        /// <param name="applicableTypes">The parameter types this rule applies to (empty means all types)</param>
        void RegisterValidationRule(
            string ruleName,
            string ruleDescription,
            ValidationSeverity severity,
            IEnumerable<string> applicableTypes = null);

        /// <summary>
        /// Gets all available validation rules
        /// </summary>
        /// <returns>A list of validation rule definitions</returns>
        IReadOnlyList<ValidationRuleDefinition> GetAvailableRules();
    }

    /// <summary>
    /// Defines a validation rule for SQL parameters
    /// </summary>
    public class ValidationRuleDefinition
    {
        /// <summary>
        /// The unique name of the rule
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Human-readable description of the rule
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// The severity level if this rule is violated
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// The parameter types this rule applies to (empty means all types)
        /// </summary>
        public IReadOnlyList<string> ApplicableTypes { get; set; } = new List<string>();

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Represents an issue found during parameter validation
    /// </summary>
    public class ParameterValidationIssue
    {
        /// <summary>
        /// The name of the parameter with the issue
        /// </summary>
        public string ParameterName { get; set; } = null!;

        /// <summary>
        /// The name of the rule that was violated
        /// </summary>
        public string RuleName { get; set; } = null!;

        /// <summary>
        /// Description of the validation issue
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// The severity of the issue
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// The original value that caused the issue
        /// </summary>
        public object? OriginalValue { get; set; }

        /// <summary>
        /// Optional recommendation for resolving the issue
        /// </summary>
        public string? Recommendation { get; set; }
    }
} 