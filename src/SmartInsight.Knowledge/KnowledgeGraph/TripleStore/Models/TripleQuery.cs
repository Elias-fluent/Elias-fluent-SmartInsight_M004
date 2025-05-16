using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents a query for triples in the triple store
    /// </summary>
    public class TripleQuery
    {
        /// <summary>
        /// The tenant ID to filter by
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// Subject ID to filter by (optional)
        /// </summary>
        public string SubjectId { get; set; }
        
        /// <summary>
        /// Predicate URI to filter by (optional)
        /// </summary>
        public string PredicateUri { get; set; }
        
        /// <summary>
        /// Object ID to filter by (optional)
        /// </summary>
        public string ObjectId { get; set; }
        
        /// <summary>
        /// Graph URI to filter by (optional)
        /// </summary>
        public string GraphUri { get; set; }
        
        /// <summary>
        /// The minimum confidence score for returned triples (0.0 to 1.0)
        /// </summary>
        public double MinConfidence { get; set; } = 0.0;
        
        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int Limit { get; set; } = 100;
        
        /// <summary>
        /// Number of results to skip (for pagination)
        /// </summary>
        public int Offset { get; set; } = 0;
        
        /// <summary>
        /// Raw SPARQL query to execute (if provided, other filter parameters are ignored)
        /// </summary>
        public string SparqlQuery { get; set; }
        
        /// <summary>
        /// Whether to include verified triples only
        /// </summary>
        public bool VerifiedOnly { get; set; } = false;
        
        /// <summary>
        /// Custom filters to apply to the query
        /// </summary>
        public Dictionary<string, object> CustomFilters { get; set; } = new Dictionary<string, object>();
    }
} 