using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.AI.Models
{
    /// <summary>
    /// Categories of safety rules for AI output verification
    /// </summary>
    public enum SafetyRuleCategory
    {
        /// <summary>
        /// Rules related to content appropriateness
        /// </summary>
        ContentAppropriate,

        /// <summary>
        /// Rules related to personally identifiable information
        /// </summary>
        PrivacyProtection,

        /// <summary>
        /// Rules related to data compliance (GDPR, HIPAA, etc.)
        /// </summary>
        Compliance,

        /// <summary>
        /// Rules related to security vulnerabilities
        /// </summary>
        Security,

        /// <summary>
        /// Rules related to intellectual property
        /// </summary>
        IntellectualProperty,

        /// <summary>
        /// Rules related to fairness and bias
        /// </summary>
        Fairness,

        /// <summary>
        /// Rules related to factual accuracy
        /// </summary>
        Accuracy
    }

    /// <summary>
    /// Severity levels for safety rule violations
    /// </summary>
    public enum SafetyRuleSeverity
    {
        /// <summary>
        /// Informational issues that don't require action
        /// </summary>
        Info,
        
        /// <summary>
        /// Warning issues that should be reviewed
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error issues that require attention
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical issues that block output delivery
        /// </summary>
        Critical
    }

    /// <summary>
    /// Definition of a safety rule for AI output verification
    /// </summary>
    public class SafetyRuleDefinition
    {
        /// <summary>
        /// Unique identifier for the rule
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the rule
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Description of the rule
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Category of the rule
        /// </summary>
        public SafetyRuleCategory Category { get; set; }

        /// <summary>
        /// Default severity of issues found by this rule
        /// </summary>
        public SafetyRuleSeverity DefaultSeverity { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Default recommendation to include with issues
        /// </summary>
        public string? DefaultRecommendation { get; set; }

        /// <summary>
        /// Tenant ID this rule belongs to (null for global rules)
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Function to check output and return any issues
        /// </summary>
        public Func<string, Dictionary<string, object>?, Task<List<SafetyRuleViolation>>>? ValidationFunction { get; set; }

        /// <summary>
        /// Function to check output and return any issues (with cancellation support)
        /// </summary>
        public Func<string, Dictionary<string, object>?, CancellationToken, Task<List<SafetyRuleViolation>>>? ValidationFunctionWithCancellation { get; set; }
    }

    /// <summary>
    /// Represents a violation of a safety rule in AI output
    /// </summary>
    public class SafetyRuleViolation
    {
        /// <summary>
        /// Description of the violation
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Category of the violated rule
        /// </summary>
        public SafetyRuleCategory Category { get; set; }

        /// <summary>
        /// Severity of the violation
        /// </summary>
        public SafetyRuleSeverity Severity { get; set; }

        /// <summary>
        /// The content that triggered the violation (if available)
        /// </summary>
        public string? ViolatingContent { get; set; }

        /// <summary>
        /// Position in the output where the violation occurred (if applicable)
        /// </summary>
        public int? Position { get; set; }

        /// <summary>
        /// Recommendation for fixing the issue
        /// </summary>
        public string? Recommendation { get; set; }

        /// <summary>
        /// Rule ID that triggered this violation
        /// </summary>
        public string RuleId { get; set; } = null!;
    }

    /// <summary>
    /// Result of AI output verification against safety rules
    /// </summary>
    public class OutputVerificationResult
    {
        /// <summary>
        /// Whether the output is safe according to the rules
        /// </summary>
        public bool IsSafe { get; set; } = true;

        /// <summary>
        /// List of safety rule violations found
        /// </summary>
        public List<SafetyRuleViolation> Violations { get; set; } = new List<SafetyRuleViolation>();

        /// <summary>
        /// The original output before verification/filtering
        /// </summary>
        public string OriginalOutput { get; set; } = null!;

        /// <summary>
        /// The filtered/sanitized output if modifications were made
        /// </summary>
        public string? FilteredOutput { get; set; }

        /// <summary>
        /// Whether the output was modified during verification
        /// </summary>
        public bool WasModified => FilteredOutput != null && FilteredOutput != OriginalOutput;

        /// <summary>
        /// Timestamp when the verification was performed
        /// </summary>
        public DateTime VerificationTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Named set of safety rules
    /// </summary>
    public class SafetyRuleSet
    {
        /// <summary>
        /// Name of the rule set
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Description of the rule set
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// When the rule set was created
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tenant ID this rule set belongs to (null for global rule sets)
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// List of rule IDs in this set
        /// </summary>
        public List<string> RuleIds { get; set; } = new List<string>();
    }
} 