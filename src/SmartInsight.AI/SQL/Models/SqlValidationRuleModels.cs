using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.AI.SQL.Models
{
    /// <summary>
    /// Definition of a SQL validation rule
    /// </summary>
    public class SqlValidationRuleDefinition
    {
        private string _category = string.Empty;
        
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
        public string Category 
        { 
            get => _category;
            set
            {
                _category = value;
                // Update CategoryEnum when string Category is set
                if (Enum.TryParse<ValidationCategory>(value, true, out var categoryEnum))
                {
                    CategoryEnum = categoryEnum;
                }
            }
        }
        
        /// <summary>
        /// Category of the rule as enum
        /// </summary>
        public ValidationCategory CategoryEnum { get; set; }

        /// <summary>
        /// Default severity of any issues found by this rule
        /// </summary>
        public ValidationSeverity DefaultSeverity { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Default recommendation to include with issues
        /// </summary>
        public string? DefaultRecommendation { get; set; }

        /// <summary>
        /// Function to check SQL and return any issues
        /// </summary>
        public Func<string, Dictionary<string, object>?, Task<List<SqlValidationIssue>>>? ValidationFunction { get; set; }
        
        /// <summary>
        /// Function to check SQL and return any issues (with cancellation support)
        /// </summary>
        public Func<string, Dictionary<string, object>?, CancellationToken, Task<List<SqlValidationIssue>>>? ValidationFunctionWithCancellation { get; set; }
    }

    /// <summary>
    /// Result of SQL validation
    /// </summary>
    public class SqlValidationResult
    {
        /// <summary>
        /// Whether the SQL is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of issues found
        /// </summary>
        public List<SqlValidationIssue> Issues { get; set; } = new List<SqlValidationIssue>();
    }

    /// <summary>
    /// Named set of validation rules
    /// </summary>
    public class SqlValidationRuleSet
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
        /// List of rule names in this set
        /// </summary>
        public List<string> RuleNames { get; set; } = new List<string>();
    }
} 