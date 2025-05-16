using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces
{
    /// <summary>
    /// Interface for taxonomy visualization operations
    /// </summary>
    public interface ITaxonomyVisualizer
    {
        /// <summary>
        /// Generates a hierarchical visualization of the taxonomy
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="rootNodeId">Optional root node ID to start from (if null, visualizes from all root nodes)</param>
        /// <param name="format">The visualization format (e.g., "json", "xml", "dot")</param>
        /// <param name="maxDepth">Maximum depth to visualize (0 for unlimited)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The visualization data in the specified format</returns>
        Task<string> GenerateHierarchyVisualizationAsync(
            string tenantId, 
            string rootNodeId = null, 
            string format = "json", 
            int maxDepth = 0, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates a graph visualization of the taxonomy with relationships
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="centralNodeId">Optional central node ID to build the graph around</param>
        /// <param name="format">The visualization format (e.g., "json", "xml", "dot")</param>
        /// <param name="includeRelationTypes">Comma-separated list of relation types to include (empty for all)</param>
        /// <param name="maxDistance">Maximum relation distance from central node (0 for unlimited)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The visualization data in the specified format</returns>
        Task<string> GenerateGraphVisualizationAsync(
            string tenantId, 
            string centralNodeId = null, 
            string format = "json", 
            string includeRelationTypes = "", 
            int maxDistance = 0, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates a tree map visualization of the taxonomy
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="rootNodeId">Optional root node ID to start from (if null, visualizes from all root nodes)</param>
        /// <param name="sizeProperty">Property name to use for determining rectangle size</param>
        /// <param name="colorProperty">Property name to use for determining rectangle color</param>
        /// <param name="format">The visualization format (e.g., "json", "svg")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The visualization data in the specified format</returns>
        Task<string> GenerateTreeMapVisualizationAsync(
            string tenantId, 
            string rootNodeId = null, 
            string sizeProperty = "count", 
            string colorProperty = "level", 
            string format = "json", 
            CancellationToken cancellationToken = default);
    }
} 