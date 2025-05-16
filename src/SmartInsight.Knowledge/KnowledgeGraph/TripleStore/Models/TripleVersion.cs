using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents a specific version of a triple in the knowledge graph
    /// </summary>
    public class TripleVersion
    {
        /// <summary>
        /// Unique identifier for this version record
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The ID of the triple this version belongs to
        /// </summary>
        public string TripleId { get; set; }
        
        /// <summary>
        /// The tenant ID this version belongs to for multi-tenant isolation
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// The version number of this triple (1-based)
        /// </summary>
        public int VersionNumber { get; set; }
        
        /// <summary>
        /// When this version was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// User ID who made this change (if applicable)
        /// </summary>
        public string ChangedByUserId { get; set; }
        
        /// <summary>
        /// Type of change made in this version
        /// </summary>
        public ChangeType ChangeType { get; set; }
        
        /// <summary>
        /// The subject entity ID of the triple at this version
        /// </summary>
        public string SubjectId { get; set; }
        
        /// <summary>
        /// The predicate (relationship type) of the triple at this version
        /// </summary>
        public string PredicateUri { get; set; }
        
        /// <summary>
        /// The object entity ID or literal value of the triple at this version
        /// </summary>
        public string ObjectId { get; set; }
        
        /// <summary>
        /// Indicates whether the object is a literal value rather than an entity reference
        /// </summary>
        public bool IsLiteral { get; set; }
        
        /// <summary>
        /// The datatype URI for literal values at this version
        /// </summary>
        public string LiteralDataType { get; set; }
        
        /// <summary>
        /// The language tag for literal values at this version (if applicable)
        /// </summary>
        public string LanguageTag { get; set; }
        
        /// <summary>
        /// The named graph this triple belongs to at this version
        /// </summary>
        public string GraphUri { get; set; }
        
        /// <summary>
        /// Confidence score for this triple at this version (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; }
        
        /// <summary>
        /// Source document ID where this triple was extracted from at this version
        /// </summary>
        public string SourceDocumentId { get; set; }
        
        /// <summary>
        /// Whether this triple was manually verified at this version
        /// </summary>
        public bool IsVerified { get; set; }
        
        /// <summary>
        /// Additional provenance information about this triple at this version
        /// </summary>
        public Dictionary<string, object> ProvenanceInfo { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Comment or reason for the change
        /// </summary>
        public string ChangeComment { get; set; }
    }
} 