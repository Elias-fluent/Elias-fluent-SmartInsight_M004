using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.Options;

namespace SmartInsight.AI.Docs
{
    /// <summary>
    /// Example demonstrating how to use the OutputVerifier in a real application scenario.
    /// This is for documentation purposes only and is not meant to be instantiated.
    /// </summary>
    public class OutputVerifierUsageExample
    {
        private readonly IOutputVerifier _outputVerifier;
        private readonly ILogger<OutputVerifierUsageExample> _logger;
        private readonly OutputVerificationOptions _options;

        /// <summary>
        /// Constructor that shows proper dependency injection
        /// </summary>
        public OutputVerifierUsageExample(
            IOutputVerifier outputVerifier,
            IOptions<OutputVerificationOptions> options,
            ILogger<OutputVerifierUsageExample> logger)
        {
            _outputVerifier = outputVerifier ?? throw new ArgumentNullException(nameof(outputVerifier));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Example method showing how to verify AI-generated output before returning it to the user
        /// </summary>
        public async Task<string> GenerateSafeResponseAsync(string userQuery, Guid? tenantId = null)
        {
            // Generate AI response (simulated for this example)
            string aiGeneratedOutput = SimulateAIResponse(userQuery);
            
            // Verify AI output against safety rules
            var verificationResult = await _outputVerifier.VerifyOutputAsync(
                aiGeneratedOutput,
                context: userQuery,
                tenantId: tenantId,
                categories: _options.DefaultCategories);
            
            // Log verification results
            LogVerificationResults(verificationResult);
            
            // Return appropriate response based on verification results
            if (!verificationResult.IsSafe)
            {
                switch (_options.DefaultAction)
                {
                    case "Filter":
                        if (_options.IncludeViolationDetails)
                        {
                            // Include violation details for transparency
                            return AppendViolationDetails(verificationResult.FilteredOutput!, verificationResult);
                        }
                        return verificationResult.FilteredOutput!;
                    
                    case "Block":
                        // Return error message instead of filtered content
                        return "Sorry, the generated content contained safety violations and could not be provided.";
                    
                    case "Log":
                        // Allow but log violations (already done above)
                        return aiGeneratedOutput;
                    
                    default:
                        return verificationResult.FilteredOutput!;
                }
            }
            
            // Content is safe, return original output
            return aiGeneratedOutput;
        }
        
        /// <summary>
        /// Example showing how to create a tenant-specific rule
        /// </summary>
        public void AddTenantSpecificRule(Guid tenantId, string prohibitedWord)
        {
            // Create a tenant-specific rule for a prohibited word
            var rule = new SafetyRuleDefinition
            {
                Name = $"TenantRule.ProhibitedWord-{prohibitedWord}",
                Description = $"Tenant-specific rule prohibiting the word '{prohibitedWord}'",
                Category = SafetyRuleCategory.ContentAppropriate,
                DefaultSeverity = SafetyRuleSeverity.Critical,
                TenantId = tenantId,
                ValidationFunction = (output, _) =>
                {
                    var violations = new List<SafetyRuleViolation>();
                    
                    if (output.Contains(prohibitedWord, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add(new SafetyRuleViolation
                        {
                            Description = $"Output contains tenant-prohibited word: {prohibitedWord}",
                            Category = SafetyRuleCategory.ContentAppropriate,
                            Severity = SafetyRuleSeverity.Critical,
                            ViolatingContent = prohibitedWord,
                            Recommendation = "Remove this tenant-prohibited word",
                            RuleId = $"TenantRule.ProhibitedWord-{prohibitedWord}"
                        });
                    }
                    
                    return Task.FromResult(violations);
                }
            };
            
            _outputVerifier.AddRule(rule);
            _logger.LogInformation("Added tenant-specific prohibited word rule for tenant {TenantId}: {Word}", 
                tenantId, prohibitedWord);
        }
        
        /// <summary>
        /// Example showing how to create a rule set for different use cases
        /// </summary>
        public SafetyRuleSet CreateHighSecurityRuleSet(Guid? tenantId = null)
        {
            // Create a rule set with stricter security rules for sensitive operations
            var securityRules = _outputVerifier.GetRulesByCategory(SafetyRuleCategory.Security)
                .Select(r => r.Id)
                .ToList();
            
            var privacyRules = _outputVerifier.GetRulesByCategory(SafetyRuleCategory.PrivacyProtection)
                .Select(r => r.Id)
                .ToList();
            
            var ruleSet = _outputVerifier.CreateRuleSet(
                "HighSecurityRuleSet",
                "Strict rule set for sensitive operations",
                securityRules.Concat(privacyRules),
                tenantId);
            
            return ruleSet;
        }
        
        // Simulates an AI response - in a real application, this would be a call to an AI service
        private string SimulateAIResponse(string userQuery)
        {
            // This is just a placeholder - in a real app, this would be a real AI response
            return $"Here is an answer to your question about {userQuery}...";
        }
        
        // Logs verification results for monitoring and analytics
        private void LogVerificationResults(OutputVerificationResult result)
        {
            if (result.IsSafe)
            {
                _logger.LogInformation("AI output passed safety verification");
                return;
            }
            
            foreach (var violation in result.Violations)
            {
                _logger.LogWarning(
                    "AI output safety violation: {Rule} ({Category}) - {Description}",
                    violation.RuleId,
                    violation.Category,
                    violation.Description);
            }
        }
        
        // Appends violation details to the filtered output for transparency
        private string AppendViolationDetails(string filteredOutput, OutputVerificationResult result)
        {
            var details = new System.Text.StringBuilder(filteredOutput);
            
            details.AppendLine();
            details.AppendLine();
            details.AppendLine("Note: The original response contained content that was filtered due to:");
            
            foreach (var violation in result.Violations.Where(v => v.Severity == SafetyRuleSeverity.Critical))
            {
                details.AppendLine($"- {violation.Description} ({violation.Category})");
            }
            
            return details.ToString();
        }
    }
} 