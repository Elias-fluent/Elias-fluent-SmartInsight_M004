using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for a rule engine that validates SQL queries against configurable rules
    /// </summary>
    public interface ISqlValidationRulesEngine
    {
        /// <summary>
        /// Gets a validation rule by its name
        /// </summary>
        /// <param name="ruleName">Name of the rule to retrieve</param>
        /// <returns>The rule definition, or null if not found</returns>
        SqlValidationRuleDefinition? GetRule(string ruleName);
        
        /// <summary>
        /// Gets all available validation rules
        /// </summary>
        /// <returns>Collection of all validation rule definitions</returns>
        IReadOnlyCollection<SqlValidationRuleDefinition> GetAllRules();
        
        /// <summary>
        /// Gets all validation rules in a specific category
        /// </summary>
        /// <param name="category">The validation category to filter by</param>
        /// <returns>Collection of rule definitions in the specified category</returns>
        IReadOnlyCollection<SqlValidationRuleDefinition> GetRulesByCategory(ValidationCategory category);
        
        /// <summary>
        /// Adds a new validation rule to the engine
        /// </summary>
        /// <param name="rule">The rule definition to add</param>
        /// <returns>True if added successfully, false if a rule with the same name already exists</returns>
        bool AddRule(SqlValidationRuleDefinition rule);
        
        /// <summary>
        /// Removes a validation rule from the engine
        /// </summary>
        /// <param name="ruleName">Name of the rule to remove</param>
        /// <returns>True if removed successfully, false if rule doesn't exist</returns>
        bool RemoveRule(string ruleName);
        
        /// <summary>
        /// Enables or disables a validation rule
        /// </summary>
        /// <param name="ruleName">Name of the rule to update</param>
        /// <param name="isEnabled">Whether the rule should be enabled</param>
        /// <returns>True if updated successfully, false if rule doesn't exist</returns>
        bool SetRuleEnabled(string ruleName, bool isEnabled);
        
        /// <summary>
        /// Validates SQL query against all enabled rules
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="parameters">Parameters for the query</param>
        /// <param name="categories">Optional categories to filter rules by (if null, all categories are used)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with all detected issues</returns>
        Task<SqlValidationResult> ValidateSqlAsync(
            string sql, 
            Dictionary<string, object>? parameters = null,
            IEnumerable<ValidationCategory>? categories = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates SQL template against all enabled rules
        /// </summary>
        /// <param name="template">The SQL template to validate</param>
        /// <param name="categories">Optional categories to filter rules by (if null, all categories are used)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with all detected issues</returns>
        Task<SqlValidationResult> ValidateTemplateAsync(
            SqlTemplate template,
            IEnumerable<ValidationCategory>? categories = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a rule set from specified rule names
        /// </summary>
        /// <param name="name">Name for the rule set</param>
        /// <param name="description">Description of the rule set</param>
        /// <param name="ruleNames">Names of rules to include in the set</param>
        /// <returns>The created rule set</returns>
        SqlValidationRuleSet CreateRuleSet(string name, string description, IEnumerable<string> ruleNames);
        
        /// <summary>
        /// Applies a rule set to a SQL query
        /// </summary>
        /// <param name="ruleSet">The rule set to apply</param>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="parameters">Parameters for the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with all detected issues</returns>
        Task<SqlValidationResult> ApplyRuleSetAsync(
            SqlValidationRuleSet ruleSet,
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);
    }
} 