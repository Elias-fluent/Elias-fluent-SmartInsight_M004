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
        /// <summary>
        /// Unique name of the rule
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Human-readable description of the rule's purpose
        /// </summary>
        public string Description { get; set; } = null!;
        
        /// <summary>
        /// Category of the rule
        /// </summary>
        public ValidationCategory Category { get; set; }
        
        /// <summary>
        /// Default severity level if this rule is violated
        /// </summary>
        public ValidationSeverity DefaultSeverity { get; set; }
        
        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// Optional recommendation text to display when rule is violated
        /// </summary>
        public string? DefaultRecommendation { get; set; }
        
        /// <summary>
        /// The function that implements the rule validation logic
        /// </summary>
        public Func<string, Dictionary<string, object>?, CancellationToken, Task<List<SqlValidationIssue>>> ValidationFunction { get; set; } = null!;
    }
    
    /// <summary>
    /// Represents a set of validation rules that can be applied as a group
    /// </summary>
    public class SqlValidationRuleSet
    {
        /// <summary>
        /// Unique name of the rule set
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Human-readable description of the rule set
        /// </summary>
        public string Description { get; set; } = null!;
        
        /// <summary>
        /// Collection of rule names included in this set
        /// </summary>
        public List<string> RuleNames { get; set; } = new List<string>();
        
        /// <summary>
        /// When the rule set was created
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the rule set was last modified
        /// </summary>
        public DateTime? LastModified { get; set; }
    }
    
    /// <summary>
    /// Type of SQL validation rule
    /// </summary>
    public enum SqlRuleType
    {
        /// <summary>
        /// Security-related rule
        /// </summary>
        Security,
        
        /// <summary>
        /// Performance-related rule
        /// </summary>
        Performance,
        
        /// <summary>
        /// Syntax-related rule
        /// </summary>
        Syntax,
        
        /// <summary>
        /// Best practice rule
        /// </summary>
        BestPractice,
        
        /// <summary>
        /// Business logic rule
        /// </summary>
        Business
    }
} 