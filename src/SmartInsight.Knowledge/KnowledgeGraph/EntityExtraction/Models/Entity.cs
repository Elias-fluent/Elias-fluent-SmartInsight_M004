using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models
{
    /// <summary>
    /// Represents an entity extracted from content
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The name or value of the entity
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The type of entity
        /// </summary>
        public EntityType Type { get; set; }
        
        /// <summary>
        /// The tenant ID this entity belongs to
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// Confidence score for the entity extraction (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; }
        
        /// <summary>
        /// Source where this entity was extracted from
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// When the entity was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the entity was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Original text or context where the entity was found
        /// </summary>
        public string OriginalContext { get; set; }
        
        /// <summary>
        /// Start position of the entity in original text (if applicable)
        /// </summary>
        public int? StartPosition { get; set; }
        
        /// <summary>
        /// End position of the entity in original text (if applicable)
        /// </summary>
        public int? EndPosition { get; set; }
        
        /// <summary>
        /// Additional attributes or properties of this entity
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Disambiguation identifier to link same entities across documents
        /// </summary>
        public string DisambiguationId { get; set; }
        
        /// <summary>
        /// Vector representation of this entity (if available)
        /// </summary>
        public float[] VectorEmbedding { get; set; }
        
        /// <summary>
        /// Whether this entity has been verified (manually or automatically)
        /// </summary>
        public bool IsVerified { get; set; }
        
        /// <summary>
        /// Version of this entity for tracking changes
        /// </summary>
        public int Version { get; set; } = 1;
    }
} 