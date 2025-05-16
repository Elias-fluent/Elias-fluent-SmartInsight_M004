using System;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models
{
    /// <summary>
    /// Represents a relationship between taxonomy nodes
    /// </summary>
    public class TaxonomyRelation
    {
        /// <summary>
        /// Unique identifier for the taxonomy relation
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The tenant ID this relation belongs to
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// The source node ID of the relation
        /// </summary>
        public string SourceNodeId { get; set; }
        
        /// <summary>
        /// The target node ID of the relation
        /// </summary>
        public string TargetNodeId { get; set; }
        
        /// <summary>
        /// The type of relationship between the nodes
        /// </summary>
        public TaxonomyRelationType RelationType { get; set; }
        
        /// <summary>
        /// Additional properties of this relation
        /// </summary>
        public string Properties { get; set; }
        
        /// <summary>
        /// When the relation was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the relation was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether this relation is system-defined or user-defined
        /// </summary>
        public bool IsSystemDefined { get; set; }
        
        /// <summary>
        /// Whether this relation is bidirectional
        /// </summary>
        public bool IsBidirectional { get; set; }
        
        /// <summary>
        /// The strength or weight of this relation (0.0 to 1.0)
        /// </summary>
        public double Weight { get; set; } = 1.0;
    }
} 