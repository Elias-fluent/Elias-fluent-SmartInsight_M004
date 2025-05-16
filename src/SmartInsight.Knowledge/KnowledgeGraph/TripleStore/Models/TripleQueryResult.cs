using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents the results of a query to the triple store
    /// </summary>
    public class TripleQueryResult
    {
        /// <summary>
        /// The triples that match the query
        /// </summary>
        public List<Triple> Triples { get; set; } = new List<Triple>();
        
        /// <summary>
        /// Total number of triples matching the query (before pagination)
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Whether there are more results available
        /// </summary>
        public bool HasMore { get; set; }
        
        /// <summary>
        /// The query that was executed
        /// </summary>
        public TripleQuery Query { get; set; }
        
        /// <summary>
        /// Additional metadata about the query execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
} 