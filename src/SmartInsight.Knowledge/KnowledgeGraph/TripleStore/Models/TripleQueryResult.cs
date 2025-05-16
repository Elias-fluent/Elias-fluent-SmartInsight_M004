using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents the result of a triple store query
    /// </summary>
    public class TripleQueryResult
    {
        /// <summary>
        /// The triples matching the query
        /// </summary>
        public List<Triple> Triples { get; set; } = new List<Triple>();
        
        /// <summary>
        /// The total number of triples matching the query (may be more than returned if limited)
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Whether the query was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if the query failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Time taken to execute the query in milliseconds
        /// </summary>
        public long QueryTimeMs { get; set; }
        
        /// <summary>
        /// The SPARQL query that was executed (if applicable)
        /// </summary>
        public string SparqlQuery { get; set; }
    }
} 