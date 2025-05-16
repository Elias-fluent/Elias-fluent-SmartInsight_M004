using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs.Taxonomy
{
    /// <summary>
    /// Data transfer object for taxonomy query operations
    /// </summary>
    public class TaxonomyQueryDto
    {
        /// <summary>
        /// The tenant ID for the query
        /// </summary>
        [Required]
        public string TenantId { get; set; }
        
        /// <summary>
        /// Optional name filter for finding nodes
        /// </summary>
        public string NameFilter { get; set; }
        
        /// <summary>
        /// Optional node type filter
        /// </summary>
        public string NodeType { get; set; }
        
        /// <summary>
        /// Whether to include inactive nodes in the results
        /// </summary>
        public bool IncludeInactive { get; set; }
        
        /// <summary>
        /// Maximum number of results to return (0 for unlimited)
        /// </summary>
        public int MaxResults { get; set; }
        
        /// <summary>
        /// Page number for paginated results (1-based)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// Number of results per page for paginated results
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
} 