using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for verifying AI outputs against safety rules and filtering/sanitizing as needed
    /// </summary>
    public interface IOutputVerifier
    {
        /// <summary>
        /// Verifies the AI output against all safety rules
        /// </summary>
        /// <param name="output">The AI output to verify</param>
        /// <param name="context">Optional context about the query/prompt that generated the output</param>
        /// <param name="tenantId">Optional tenant ID for tenant-specific rules</param>
        /// <param name="categories">Optional categories to filter rules by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verification result with any violations and filtered content if needed</returns>
        Task<OutputVerificationResult> VerifyOutputAsync(
            string output,
            string? context = null,
            Guid? tenantId = null,
            IEnumerable<SafetyRuleCategory>? categories = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a safety rule by its ID
        /// </summary>
        /// <param name="ruleId">ID of the rule to retrieve</param>
        /// <returns>The rule definition, or null if not found</returns>
        SafetyRuleDefinition? GetRule(string ruleId);

        /// <summary>
        /// Gets all safety rules
        /// </summary>
        /// <param name="includeDisabled">Whether to include disabled rules</param>
        /// <param name="tenantId">Optional tenant ID to filter rules</param>
        /// <returns>Collection of all available safety rules</returns>
        IReadOnlyCollection<SafetyRuleDefinition> GetAllRules(bool includeDisabled = false, Guid? tenantId = null);

        /// <summary>
        /// Gets all safety rules in a specific category
        /// </summary>
        /// <param name="category">Category to filter by</param>
        /// <param name="includeDisabled">Whether to include disabled rules</param>
        /// <param name="tenantId">Optional tenant ID to filter rules</param>
        /// <returns>Collection of rules in the specified category</returns>
        IReadOnlyCollection<SafetyRuleDefinition> GetRulesByCategory(
            SafetyRuleCategory category, 
            bool includeDisabled = false, 
            Guid? tenantId = null);

        /// <summary>
        /// Adds a new safety rule
        /// </summary>
        /// <param name="rule">The rule definition to add</param>
        /// <returns>True if added successfully, false if a rule with the same ID already exists</returns>
        bool AddRule(SafetyRuleDefinition rule);

        /// <summary>
        /// Removes a safety rule
        /// </summary>
        /// <param name="ruleId">ID of the rule to remove</param>
        /// <returns>True if removed successfully, false if rule doesn't exist</returns>
        bool RemoveRule(string ruleId);

        /// <summary>
        /// Enables or disables a safety rule
        /// </summary>
        /// <param name="ruleId">ID of the rule to update</param>
        /// <param name="isEnabled">Whether the rule should be enabled</param>
        /// <returns>True if updated successfully, false if rule doesn't exist</returns>
        bool SetRuleEnabled(string ruleId, bool isEnabled);

        /// <summary>
        /// Creates a named set of safety rules
        /// </summary>
        /// <param name="name">Name for the rule set</param>
        /// <param name="description">Description of the rule set</param>
        /// <param name="ruleIds">IDs of rules to include in the set</param>
        /// <param name="tenantId">Optional tenant ID this rule set belongs to</param>
        /// <returns>The created rule set</returns>
        SafetyRuleSet CreateRuleSet(string name, string description, IEnumerable<string> ruleIds, Guid? tenantId = null);

        /// <summary>
        /// Applies a rule set to verify an output
        /// </summary>
        /// <param name="ruleSet">The rule set to apply</param>
        /// <param name="output">The AI output to verify</param>
        /// <param name="context">Optional context about the query/prompt that generated the output</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verification result with any violations and filtered content if needed</returns>
        Task<OutputVerificationResult> ApplyRuleSetAsync(
            SafetyRuleSet ruleSet,
            string output,
            string? context = null,
            CancellationToken cancellationToken = default);
    }
} 