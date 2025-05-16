using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models
{
    /// <summary>
    /// Contains standardized metadata for tracking the provenance (origin and lineage) 
    /// of knowledge graph elements
    /// </summary>
    public class ProvenanceMetadata
    {
        /// <summary>
        /// Unique identifier for this provenance record
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The tenant ID this provenance record belongs to for multi-tenant isolation
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// ID of the knowledge element (Triple, Entity, etc.) this provenance applies to
        /// </summary>
        public string ElementId { get; set; }
        
        /// <summary>
        /// Type of knowledge element this provenance applies to (Triple, Entity, Relation, etc.)
        /// </summary>
        public string ElementType { get; set; }
        
        /// <summary>
        /// Source data where this knowledge was extracted from
        /// </summary>
        public SourceReference Source { get; set; } = new SourceReference();
        
        /// <summary>
        /// Confidence score for this knowledge element (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; } = 1.0;
        
        /// <summary>
        /// When this knowledge element was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this knowledge element was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Method or technique used to extract/create this knowledge element
        /// </summary>
        public string ExtractionMethod { get; set; }
        
        /// <summary>
        /// Whether this knowledge element has been manually verified
        /// </summary>
        public bool IsVerified { get; set; }
        
        /// <summary>
        /// User ID responsible for verification (if applicable)
        /// </summary>
        public string VerifiedBy { get; set; }
        
        /// <summary>
        /// When this knowledge element was verified
        /// </summary>
        public DateTime? VerifiedAt { get; set; }
        
        /// <summary>
        /// Current version of this knowledge element
        /// </summary>
        public int Version { get; set; } = 1;
        
        /// <summary>
        /// The reasoning or justification for this knowledge element's existence
        /// </summary>
        public string Justification { get; set; }
        
        /// <summary>
        /// References to other knowledge elements that this element depends on or is derived from
        /// </summary>
        public List<DependencyReference> Dependencies { get; set; } = new List<DependencyReference>();
        
        /// <summary>
        /// Additional custom attributes for extensibility
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Represents a reference to the source data
    /// </summary>
    public class SourceReference
    {
        /// <summary>
        /// ID of the source document or data source
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// Type of source (Document, Database, API, etc.)
        /// </summary>
        public string SourceType { get; set; }
        
        /// <summary>
        /// Name of the data source connector that provided this data
        /// </summary>
        public string ConnectorName { get; set; }
        
        /// <summary>
        /// When this source was accessed or ingested
        /// </summary>
        public DateTime IngestionTimestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Original text context where this knowledge was extracted (if applicable)
        /// </summary>
        public string TextContext { get; set; }
        
        /// <summary>
        /// Start position in text (if applicable)
        /// </summary>
        public int? StartPosition { get; set; }
        
        /// <summary>
        /// End position in text (if applicable)
        /// </summary>
        public int? EndPosition { get; set; }
        
        /// <summary>
        /// Additional source-specific attributes
        /// </summary>
        public Dictionary<string, object> SourceAttributes { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Represents a dependency on another knowledge element
    /// </summary>
    public class DependencyReference
    {
        /// <summary>
        /// ID of the dependent knowledge element
        /// </summary>
        public string DependencyId { get; set; }
        
        /// <summary>
        /// Type of the dependent knowledge element
        /// </summary>
        public string DependencyType { get; set; }
        
        /// <summary>
        /// Nature of the dependency relationship
        /// </summary>
        public string RelationshipType { get; set; }
        
        /// <summary>
        /// Confidence in this dependency relationship (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; } = 1.0;
    }
} 