using System;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents a temporal query for retrieving historical states of the knowledge graph
    /// </summary>
    public class TemporalQuery
    {
        /// <summary>
        /// The point in time to query (exact time point)
        /// </summary>
        public DateTime? AsOfDate { get; set; }
        
        /// <summary>
        /// The start of a time range to query
        /// </summary>
        public DateTime? FromDate { get; set; }
        
        /// <summary>
        /// The end of a time range to query
        /// </summary>
        public DateTime? ToDate { get; set; }
        
        /// <summary>
        /// Include deleted triples in the results
        /// </summary>
        public bool IncludeDeleted { get; set; }
        
        /// <summary>
        /// The version number to retrieve (if specific version is desired)
        /// </summary>
        public int? VersionNumber { get; set; }
        
        /// <summary>
        /// Return all versions of the triples in the result
        /// </summary>
        public bool IncludeAllVersions { get; set; }
        
        /// <summary>
        /// Filter by specific change types
        /// </summary>
        public ChangeType[] ChangeTypes { get; set; }
        
        /// <summary>
        /// Filter by user who made the changes
        /// </summary>
        public string ChangedByUserId { get; set; }
        
        /// <summary>
        /// The underlying triple query to filter results
        /// </summary>
        public TripleQuery TripleQuery { get; set; }
        
        /// <summary>
        /// Maximum number of versions to return per triple
        /// </summary>
        public int MaxVersionsPerTriple { get; set; } = 10;
        
        /// <summary>
        /// Return only the difference between versions
        /// </summary>
        public bool DiffOnly { get; set; }
        
        /// <summary>
        /// Creates a new temporal query with default settings
        /// </summary>
        public TemporalQuery()
        {
            TripleQuery = new TripleQuery();
            ChangeTypes = Array.Empty<ChangeType>();
        }
        
        /// <summary>
        /// Creates a temporal query to retrieve the state at a specific point in time
        /// </summary>
        /// <param name="asOfDate">The date to retrieve the state for</param>
        /// <returns>A configured temporal query</returns>
        public static TemporalQuery AsOf(DateTime asOfDate)
        {
            return new TemporalQuery
            {
                AsOfDate = asOfDate,
                IncludeDeleted = false
            };
        }
        
        /// <summary>
        /// Creates a temporal query to retrieve changes during a specific time range
        /// </summary>
        /// <param name="fromDate">The start date of the range</param>
        /// <param name="toDate">The end date of the range</param>
        /// <returns>A configured temporal query</returns>
        public static TemporalQuery BetweenDates(DateTime fromDate, DateTime toDate)
        {
            return new TemporalQuery
            {
                FromDate = fromDate,
                ToDate = toDate,
                IncludeAllVersions = true
            };
        }
        
        /// <summary>
        /// Creates a temporal query to retrieve a specific version of triples
        /// </summary>
        /// <param name="versionNumber">The version number to retrieve</param>
        /// <returns>A configured temporal query</returns>
        public static TemporalQuery ForVersion(int versionNumber)
        {
            return new TemporalQuery
            {
                VersionNumber = versionNumber
            };
        }
    }
} 