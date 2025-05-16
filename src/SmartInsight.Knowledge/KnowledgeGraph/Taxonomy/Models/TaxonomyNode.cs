using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models
{
    /// <summary>
    /// Represents a node in the hierarchical taxonomy structure
    /// </summary>
    public class TaxonomyNode
    {
        /// <summary>
        /// Unique identifier for the taxonomy node
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The tenant ID this taxonomy node belongs to
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// The name of the taxonomy node
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// A description of the taxonomy node
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The parent node ID (null for root nodes)
        /// </summary>
        public string ParentId { get; set; }
        
        /// <summary>
        /// The type of this taxonomy node (e.g., Class, Property, Relation)
        /// </summary>
        public TaxonomyNodeType NodeType { get; set; }
        
        /// <summary>
        /// The unique identifier within its domain (e.g., URI or qualified name)
        /// </summary>
        public string QualifiedName { get; set; }
        
        /// <summary>
        /// The namespace or domain this taxonomy node belongs to
        /// </summary>
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
        /// When the taxonomy node was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the taxonomy node was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Version of this taxonomy node
        /// </summary>
        public int Version { get; set; } = 1;
        
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