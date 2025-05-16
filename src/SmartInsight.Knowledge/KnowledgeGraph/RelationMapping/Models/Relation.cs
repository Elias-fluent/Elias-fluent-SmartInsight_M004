using System;
using System.Collections.Generic;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models
{
    /// <summary>
    /// Represents a relation between two entities
    /// </summary>
    public class Relation
    {
        /// <summary>
        /// Unique identifier for the relation
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The tenant ID this relation belongs to
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// The source entity (subject) of the relation
        /// </summary>
        public Entity SourceEntity { get; set; }
        
        /// <summary>
        /// The ID of the source entity
        /// </summary>
        public string SourceEntityId { get; set; }
        
        /// <summary>
        /// The target entity (object) of the relation
        /// </summary>
        public Entity TargetEntity { get; set; }
        
        /// <summary>
        /// The ID of the target entity
        /// </summary>
        public string TargetEntityId { get; set; }
        
        /// <summary>
        /// The type of relation between the entities
        /// </summary>
        public RelationType RelationType { get; set; }
        
        /// <summary>
        /// Optional custom name for the relation (particularly for DomainSpecific relations)
        /// </summary>
        public string RelationName { get; set; }
        
        /// <summary>
        /// The strength or confidence score for this relation (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; }
        
        /// <summary>
        /// The original text context where this relation was identified
        /// </summary>
        public string SourceContext { get; set; }
        
        /// <summary>
        /// ID of the document where this relation was identified
        /// </summary>
        public string SourceDocumentId { get; set; }
        
        /// <summary>
        /// Whether this relation is directional (SourceEntity -> TargetEntity)
        /// If false, the relation is bidirectional (SourceEntity <-> TargetEntity)
        /// </summary>
        public bool IsDirectional { get; set; } = true;
        
        /// <summary>
        /// When the relation was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the relation was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Method used to extract this relation
        /// </summary>
        public string ExtractionMethod { get; set; }
        
        /// <summary>
        /// Whether this relation has been manually verified
        /// </summary>
        public bool IsVerified { get; set; }
        
        /// <summary>
        /// Version of this relation for tracking changes
        /// </summary>
        public int Version { get; set; } = 1;
        
        /// <summary>
        /// Additional attributes or properties of this relation
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
} 