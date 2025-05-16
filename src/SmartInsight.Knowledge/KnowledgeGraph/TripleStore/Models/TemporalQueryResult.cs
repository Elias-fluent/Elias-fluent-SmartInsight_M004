using System;
using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents the results of a temporal query on the triple store
    /// </summary>
    public class TemporalQueryResult
    {
        /// <summary>
        /// The triple versions matching the query
        /// </summary>
        public List<TripleVersion> TripleVersions { get; set; } = new List<TripleVersion>();
        
        /// <summary>
        /// The triples at the requested point in time
        /// </summary>
        public List<Triple> Triples { get; set; } = new List<Triple>();
        
        /// <summary>
        /// Total number of version records matching the query (before pagination)
        /// </summary>
        public int TotalVersionCount { get; set; }
        
        /// <summary>
        /// Total number of distinct triples matching the query
        /// </summary>
        public int TotalTripleCount { get; set; }
        
        /// <summary>
        /// Whether there are more results available
        /// </summary>
        public bool HasMore { get; set; }
        
        /// <summary>
        /// The temporal query that was executed
        /// </summary>
        public TemporalQuery Query { get; set; }
        
        /// <summary>
        /// Additional metadata about the query results
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// The point in time for which this result represents the state
        /// </summary>
        public DateTime? StateTimestamp { get; set; }
        
        /// <summary>
        /// For diff queries, the differences between versions
        /// </summary>
        public List<TripleDiff> Diffs { get; set; } = new List<TripleDiff>();
        
        /// <summary>
        /// Gets triple versions grouped by triple ID
        /// </summary>
        /// <returns>A dictionary of triple versions grouped by triple ID</returns>
        public Dictionary<string, List<TripleVersion>> GetVersionsByTripleId()
        {
            var result = new Dictionary<string, List<TripleVersion>>();
            
            foreach (var version in TripleVersions)
            {
                if (!result.ContainsKey(version.TripleId))
                {
                    result[version.TripleId] = new List<TripleVersion>();
                }
                
                result[version.TripleId].Add(version);
            }
            
            return result;
        }
    }
} 