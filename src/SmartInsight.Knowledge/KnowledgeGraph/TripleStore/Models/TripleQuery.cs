using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents a query for the triple store with various filter criteria
    /// </summary>
    public class TripleQuery
    {
        /// <summary>
        /// The tenant ID for multi-tenant isolation
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// Filter by subject ID
        /// </summary>
        public string SubjectId { get; set; }
        
        /// <summary>
        /// Filter by predicate URI
        /// </summary>
        public string PredicateUri { get; set; }
        
        /// <summary>
        /// Filter by object ID
        /// </summary>
        public string ObjectId { get; set; }
        
        /// <summary>
        /// Filter by named graph URI
        /// </summary>
        public string GraphUri { get; set; }
        
        /// <summary>
        /// Filter triples created after this date
        /// </summary>
        public DateTime? CreatedAfter { get; set; }
        
        /// <summary>
        /// Filter triples created before this date
        /// </summary>
        public DateTime? CreatedBefore { get; set; }
        
        /// <summary>
        /// Filter by minimum confidence score
        /// </summary>
        public double? MinConfidenceScore { get; set; }
        
        /// <summary>
        /// Filter by verification status
        /// </summary>
        public bool? IsVerified { get; set; }
        
        /// <summary>
        /// Filter by source document ID
        /// </summary>
        public string SourceDocumentId { get; set; }
        
        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int Limit { get; set; } = 100;
        
        /// <summary>
        /// Number of results to skip (for pagination)
        /// </summary>
        public int Offset { get; set; } = 0;
        
        /// <summary>
        /// Sort results by this property
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";
        
        /// <summary>
        /// Sort direction (ascending/descending)
        /// </summary>
        public bool SortAscending { get; set; } = false;
        
        /// <summary>
        /// Additional custom filter properties
        /// </summary>
        public Dictionary<string, object> CustomFilters { get; set; } = new Dictionary<string, object>();
    }
} 