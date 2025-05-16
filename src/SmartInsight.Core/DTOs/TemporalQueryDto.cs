using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs
{
    /// <summary>
    /// Data transfer object for temporal query requests
    /// </summary>
    public class TemporalQueryDto
    {
        /// <summary>
        /// Filter triples by subject ID
        /// </summary>
        public string SubjectId { get; set; }
        
        /// <summary>
        /// Filter triples by predicate URI
        /// </summary>
        public string PredicateUri { get; set; }
        
        /// <summary>
        /// Filter triples by object ID
        /// </summary>
        public string ObjectId { get; set; }
        
        /// <summary>
        /// Only include triples from this graph URI
        /// </summary>
        public string GraphUri { get; set; }
        
        /// <summary>
        /// Query as of this specific date and time
        /// </summary>
        public DateTime? AsOfDate { get; set; }
        
        /// <summary>
        /// Query as of this specific version number
        /// </summary>
        public int? VersionNumber { get; set; }
        
        /// <summary>
        /// Include triples from this date and time (inclusive)
        /// </summary>
        public DateTime? FromDate { get; set; }
        
        /// <summary>
        /// Include triples up to this date and time (inclusive)
        /// </summary>
        public DateTime? ToDate { get; set; }
        
        /// <summary>
        /// Maximum number of results to return (default: 100)
        /// </summary>
        [Range(1, 1000)]
        public int MaxResults { get; set; } = 100;
        
        /// <summary>
        /// Zero-based index of the first result to return (for pagination)
        /// </summary>
        [Range(0, int.MaxValue)]
        public int Offset { get; set; } = 0;
        
        /// <summary>
        /// Whether to include deleted triples in the results
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;
        
        /// <summary>
        /// Only include triples with these change types
        /// </summary>
        public List<string> ChangeTypes { get; set; } = new List<string>();
    }
} 