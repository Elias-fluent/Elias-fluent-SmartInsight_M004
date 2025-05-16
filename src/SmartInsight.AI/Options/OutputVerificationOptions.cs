using System.Collections.Generic;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Options
{
    /// <summary>
    /// Options for configuring the output verification system
    /// </summary>
    public class OutputVerificationOptions
    {
        /// <summary>
        /// Whether output verification is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Default action when safety rule is violated
        /// "Filter" - Replace violating content
        /// "Block" - Return error message
        /// "Log" - Allow but log violation
        /// </summary>
        public string DefaultAction { get; set; } = "Filter";
        
        /// <summary>
        /// Categories to check by default
        /// </summary>
        public List<SafetyRuleCategory> DefaultCategories { get; set; } = new List<SafetyRuleCategory>
        {
            SafetyRuleCategory.ContentAppropriate,
            SafetyRuleCategory.PrivacyProtection,
            SafetyRuleCategory.Security
        };
        
        /// <summary>
        /// Whether to enforce tenant-specific rules
        /// </summary>
        public bool EnforceTenantRules { get; set; } = true;
        
        /// <summary>
        /// Text to use when replacing violating content
        /// </summary>
        public string ReplacementText { get; set; } = "[CONTENT REMOVED DUE TO SAFETY CONCERNS]";
        
        /// <summary>
        /// Whether to add detailed violation information to the filtered output
        /// </summary>
        public bool IncludeViolationDetails { get; set; } = false;
        
        /// <summary>
        /// Minimum severity level that triggers filtering/blocking
        /// </summary>
        public SafetyRuleSeverity BlockingThreshold { get; set; } = SafetyRuleSeverity.Critical;
    }
} 