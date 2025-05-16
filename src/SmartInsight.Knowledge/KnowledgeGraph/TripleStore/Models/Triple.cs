using System;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents a triple (subject-predicate-object) in the knowledge graph
    /// </summary>
    public class Triple
    {
        /// <summary>
        /// Unique identifier for the triple
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The tenant ID this triple belongs to for multi-tenant isolation
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// The subject entity ID of the triple
        /// </summary>
        public string SubjectId { get; set; }
        
        /// <summary>
        /// The predicate (relationship type) of the triple
        /// </summary>
        public string PredicateUri { get; set; }
        
        /// <summary>
        /// The object entity ID or literal value of the triple
        /// </summary>
        public string ObjectId { get; set; }
        
        /// <summary>
        /// Indicates whether the object is a literal value rather than an entity reference
        /// </summary>
        public bool IsLiteral { get; set; }
        
        /// <summary>
        /// The datatype URI for literal values
        /// </summary>
        public string LiteralDataType { get; set; }
        
        /// <summary>
        /// The language tag for literal values (if applicable)
        /// </summary>
        public string LanguageTag { get; set; }
        
        /// <summary>
        /// The named graph this triple belongs to
        /// </summary>
        public string GraphUri { get; set; }
        
        /// <summary>
        /// Confidence score for this triple (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; } = 1.0;
        
        /// <summary>
        /// When the triple was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the triple was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Source document ID where this triple was extracted from
        /// </summary>
        public string SourceDocumentId { get; set; }
        
        /// <summary>
        /// Whether this triple has been manually verified
        /// </summary>
        public bool IsVerified { get; set; }
        
        /// <summary>
        /// Version of this triple for tracking changes
        /// </summary>
        public int Version { get; set; } = 1;
    }
} 