using System;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs.Taxonomy
{
    /// <summary>
    /// Data transfer object for taxonomy relation operations
    /// </summary>
    public class TaxonomyRelationDto
    {
        /// <summary>
        /// Unique identifier for the taxonomy relation
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// The source node ID of the relation
        /// </summary>
        [Required]
        public string SourceNodeId { get; set; }
        
        /// <summary>
        /// The target node ID of the relation
        /// </summary>
        [Required]
        public string TargetNodeId { get; set; }
        
        /// <summary>
        /// The type of relationship between the nodes
        /// </summary>
        [Required]
        public string RelationType { get; set; }
        
        /// <summary>
        /// Additional properties of this relation
        /// </summary>
        public string Properties { get; set; }
        
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
        [Range(0.0, 1.0)]
        public double Weight { get; set; } = 1.0;
    }
} 