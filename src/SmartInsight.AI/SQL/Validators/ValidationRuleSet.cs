using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Validators
{
    /// <summary>
    /// Represents a set of related validation rules that can be applied as a group
    /// </summary>
    public class ValidationRuleSet
    {
        private readonly IParameterValidator _validator;
        private readonly ILogger _logger;
        private readonly List<string> _ruleNames = new List<string>();
        private readonly string _name;
        private readonly string _description;
        
        /// <summary>
        /// Gets the name of this rule set
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Gets the description of this rule set
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// Gets the list of rule names in this set
        /// </summary>
        public IReadOnlyList<string> RuleNames => _ruleNames;
        
        /// <summary>
        /// Creates a new ValidationRuleSet
        /// </summary>
        /// <param name="validator">The parameter validator to use</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="name">Name of the rule set</param>
        /// <param name="description">Description of the rule set</param>
        public ValidationRuleSet(
            IParameterValidator validator,
            ILogger logger,
            string name,
            string description)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _description = description ?? throw new ArgumentNullException(nameof(description));
        }
        
        /// <summary>
        /// Adds a rule to the rule set
        /// </summary>
        /// <param name="ruleName">The name of the rule to add</param>
        /// <returns>This rule set for method chaining</returns>
        public ValidationRuleSet AddRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                throw new ArgumentException("Rule name cannot be null or empty", nameof(ruleName));
            }
            
            _ruleNames.Add(ruleName);
            return this;
        }
        
        /// <summary>
        /// Applies all rules in this set to a parameter
        /// </summary>
        /// <param name="parameter">The parameter to validate</param>
        /// <param name="templateParameter">The template parameter definition</param>
        /// <returns>A list of validation issues found</returns>
        public async Task<List<ParameterValidationIssue>> ApplyAsync(
            ExtractedParameter parameter,
            SqlTemplateParameter templateParameter)
        {
            var issues = new List<ParameterValidationIssue>();
            
            foreach (var ruleName in _ruleNames)
            {
                try
                {
                    var issue = await _validator.ValidateParameterAsync(parameter, templateParameter, ruleName);
                    if (issue != null)
                    {
                        issues.Add(issue);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying rule {RuleName} to parameter {ParamName}", 
                        ruleName, parameter.Name);
                }
            }
            
            return issues;
        }
        
        /// <summary>
        /// Applies all rules in this set to a set of parameters
        /// </summary>
        /// <param name="parameters">The parameters to validate</param>
        /// <param name="template">The SQL template</param>
        /// <returns>A validation result with all issues found</returns>
        public async Task<Models.ParameterValidationResult> ApplyAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template)
        {
            var result = new Models.ParameterValidationResult
            {
                IsValid = true
            };
            
            foreach (var parameter in parameters)
            {
                // Find the template parameter definition
                var templateParam = template.Parameters.Find(p => p.Name == parameter.Key);
                if (templateParam == null)
                {
                    // Parameter not defined in template, skip validation
                    continue;
                }
                
                var issues = await ApplyAsync(parameter.Value, templateParam);
                foreach (var issue in issues)
                {
                    result.AddIssue(issue);
                }
            }
            
            return result;
        }
    }
} 