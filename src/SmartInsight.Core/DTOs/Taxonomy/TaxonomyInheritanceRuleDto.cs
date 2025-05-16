using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs.Taxonomy
{
    /// <summary>
    /// Data transfer object for taxonomy inheritance rule operations
    /// </summary>
    public class TaxonomyInheritanceRuleDto
    {
        /// <summary>
        /// Unique identifier for the inheritance rule
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// The source node type that this rule applies to
        /// </summary>
        [Required]
        public string SourceNodeTypeId { get; set; }
        
        /// <summary>
        /// The target node type that properties are inherited from/to
        /// </summary>
        [Required]
        public string TargetNodeTypeId { get; set; }
        
        /// <summary>
        /// The type of inheritance rule
        /// </summary>
        [Required]
        public string RuleType { get; set; }
        
        /// <summary>
        /// List of property names that should be included in inheritance
        /// (empty list means all properties are included)
        /// </summary>
        public List<string> IncludedProperties { get; set; } = new List<string>();
        
        /// <summary>
        /// List of property names that should be excluded from inheritance
        /// </summary>
        public List<string> ExcludedProperties { get; set; } = new List<string>();
        
        /// <summary>
        /// Priority of this rule (higher numbers take precedence)
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Whether property values should be merged or replaced
        /// </summary>
        public bool MergeValues { get; set; }
        
        /// <summary>
        /// Whether this rule is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// A description of the rule
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }
    }
    
    /// <summary>
    /// Available types of inheritance rules
    /// </summary>
    public enum InheritanceRuleType
    {
        /// <summary>
        /// Inherit properties from parent to child
        /// </summary>
        DownwardInheritance,
        
        /// <summary>
        /// Propagate properties from children to parent
        /// </summary>
        UpwardPropagation,
        
        /// <summary>
        /// Share properties among siblings
        /// </summary>
        SiblingSharing,
        
        /// <summary>
        /// Custom inheritance logic
        /// </summary>
        Custom
    }
} 