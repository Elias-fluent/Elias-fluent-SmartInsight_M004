using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs.Taxonomy
{
    /// <summary>
    /// Data transfer object for taxonomy node operations
    /// </summary>
    public class TaxonomyNodeDto
    {
        /// <summary>
        /// Unique identifier for the taxonomy node
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// The name of the taxonomy node
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; }
        
        /// <summary>
        /// A description of the taxonomy node
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// The parent node ID (null for root nodes)
        /// </summary>
        public string ParentId { get; set; }
        
        /// <summary>
        /// The type of this taxonomy node (e.g., Class, Property, Relation)
        /// </summary>
        [Required]
        public string NodeType { get; set; }
        
        /// <summary>
        /// The unique identifier within its domain (e.g., URI or qualified name)
        /// </summary>
        [StringLength(200)]
        public string QualifiedName { get; set; }
        
        /// <summary>
        /// The namespace or domain this taxonomy node belongs to
        /// </summary>
        [StringLength(100)]
        public string Namespace { get; set; }
        
        /// <summary>
        /// Aliases or alternative names for this taxonomy node
        /// </summary>
        public List<string> Aliases { get; set; } = new List<string>();
        
        /// <summary>
        /// Properties associated with this taxonomy node
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Whether this node is a system-defined node or user-defined
        /// </summary>
        public bool IsSystemDefined { get; set; }
        
        /// <summary>
        /// Whether this node is active and should be used in operations
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
} 