using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models
{
    /// <summary>
    /// Represents query parameters for searching provenance information
    /// </summary>
    public class ProvenanceQuery
    {
        /// <summary>
        /// Filter by element ID
        /// </summary>
        public string ElementId { get; set; }
        
        /// <summary>
        /// Filter by element type
        /// </summary>
        public ProvenanceElementType? ElementType { get; set; }
        
        /// <summary>
        /// Filter by source ID
        /// </summary>
        public string SourceId { get; set; }
        
        /// <summary>
        /// Filter by source type
        /// </summary>
        public ProvenanceSourceType? SourceType { get; set; }
        
        /// <summary>
        /// Filter by connector name
        /// </summary>
        public string ConnectorName { get; set; }
        
        /// <summary>
        /// Filter by minimum confidence score
        /// </summary>
        public double? MinConfidenceScore { get; set; }
        
        /// <summary>
        /// Filter by verification status
        /// </summary>
        public bool? IsVerified { get; set; }
        
        /// <summary>
        /// Filter by the user who verified
        /// </summary>
        public string VerifiedBy { get; set; }
        
        /// <summary>
        /// Filter by extraction method
        /// </summary>
        public string ExtractionMethod { get; set; }
        
        /// <summary>
        /// Filter by creation date range (minimum)
        /// </summary>
        public DateTime? CreatedAfter { get; set; }
        
        /// <summary>
        /// Filter by creation date range (maximum)
        /// </summary>
        public DateTime? CreatedBefore { get; set; }
        
        /// <summary>
        /// Filter by update date range (minimum)
        /// </summary>
        public DateTime? UpdatedAfter { get; set; }
        
        /// <summary>
        /// Filter by update date range (maximum)
        /// </summary>
        public DateTime? UpdatedBefore { get; set; }
        
        /// <summary>
        /// Filter by element version
        /// </summary>
        public int? Version { get; set; }
        
        /// <summary>
        /// Filter by text in justification
        /// </summary>
        public string JustificationContains { get; set; }
        
        /// <summary>
        /// Filter by dependencies
        /// </summary>
        public List<string> DependencyIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Additional custom attribute filters
        /// </summary>
        public Dictionary<string, object> AttributeFilters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; } = 100;
        
        /// <summary>
        /// Skip this number of results (for pagination)
        /// </summary>
        public int Skip { get; set; } = 0;
        
        /// <summary>
        /// Property to sort by
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";
        
        /// <summary>
        /// Sort direction
        /// </summary>
        public bool SortAscending { get; set; } = false;
    }
    
    /// <summary>
    /// Represents the results of a provenance query
    /// </summary>
    public class ProvenanceQueryResult
    {
        /// <summary>
        /// The matched provenance metadata records
        /// </summary>
        public List<ProvenanceMetadata> Results { get; set; } = new List<ProvenanceMetadata>();
        
        /// <summary>
        /// Total number of matching records
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// The query that was executed
        /// </summary>
        public ProvenanceQuery Query { get; set; }
    }
} 