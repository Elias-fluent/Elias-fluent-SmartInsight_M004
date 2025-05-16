using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy
{
    /// <summary>
    /// Implementation of the taxonomy visualizer
    /// </summary>
    public class TaxonomyVisualizer : ITaxonomyVisualizer
    {
        private readonly ITaxonomyService _taxonomyService;
        private readonly ILogger<TaxonomyVisualizer> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyVisualizer"/> class
        /// </summary>
        /// <param name="taxonomyService">The taxonomy service</param>
        /// <param name="logger">The logger</param>
        public TaxonomyVisualizer(ITaxonomyService taxonomyService, ILogger<TaxonomyVisualizer> logger)
        {
            _taxonomyService = taxonomyService ?? throw new ArgumentNullException(nameof(taxonomyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Generates a hierarchical visualization of the taxonomy
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="rootNodeId">Optional root node ID to start from (if null, visualizes from all root nodes)</param>
        /// <param name="format">The visualization format (e.g., "json", "xml", "dot")</param>
        /// <param name="maxDepth">Maximum depth to visualize (0 for unlimited)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The visualization data in the specified format</returns>
        public async Task<string> GenerateHierarchyVisualizationAsync(
            string tenantId, 
            string rootNodeId = null, 
            string format = "json", 
            int maxDepth = 0, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            _logger.LogInformation("Generating hierarchy visualization for tenant {TenantId}, format {Format}", tenantId, format);
            
            List<TaxonomyNode> rootNodes;
            
            if (!string.IsNullOrEmpty(rootNodeId))
            {
                // Start from a specific node
                var rootNode = await _taxonomyService.GetNodeAsync(rootNodeId, tenantId, cancellationToken);
                if (rootNode == null)
                    throw new InvalidOperationException($"Root node with ID {rootNodeId} not found for tenant {tenantId}");
                
                rootNodes = new List<TaxonomyNode> { rootNode };
            }
            else
            {
                // Start from all root nodes
                rootNodes = (await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken)).ToList();
            }
            
            // Build the hierarchy for each root node
            var hierarchyNodes = new List<HierarchyNode>();
            
            foreach (var rootNode in rootNodes)
            {
                var hierarchyNode = await BuildHierarchyNodeAsync(rootNode, tenantId, 1, maxDepth, cancellationToken);
                hierarchyNodes.Add(hierarchyNode);
            }
            
            // Format the output according to the specified format
            return format.ToLowerInvariant() switch
            {
                "json" => JsonConvert.SerializeObject(hierarchyNodes, Formatting.Indented),
                "xml" => throw new NotImplementedException("XML format not yet implemented"),
                "dot" => GenerateDotGraph(hierarchyNodes, "Taxonomy Hierarchy"),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };
        }
        
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
        public async Task<string> GenerateGraphVisualizationAsync(
            string tenantId, 
            string centralNodeId = null, 
            string format = "json", 
            string includeRelationTypes = "", 
            int maxDistance = 0, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            _logger.LogInformation("Generating graph visualization for tenant {TenantId}, format {Format}", tenantId, format);
            
            // Parse relation types to include
            TaxonomyRelationType[] relationTypes = null;
            if (!string.IsNullOrEmpty(includeRelationTypes))
            {
                var typeNames = includeRelationTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (typeNames.Length > 0)
                {
                    var typesList = new List<TaxonomyRelationType>();
                    foreach (var typeName in typeNames)
                    {
                        if (Enum.TryParse<TaxonomyRelationType>(typeName.Trim(), true, out var relationType))
                            typesList.Add(relationType);
                    }
                    
                    if (typesList.Count > 0)
                        relationTypes = typesList.ToArray();
                }
            }
            
            // Start building the graph from the central node (or all nodes if no central node specified)
            var graph = new GraphVisualization
            {
                Nodes = new List<GraphNode>(),
                Edges = new List<GraphEdge>()
            };
            
            var processedNodeIds = new HashSet<string>();
            
            if (!string.IsNullOrEmpty(centralNodeId))
            {
                // Start from a specific node
                var centralNode = await _taxonomyService.GetNodeAsync(centralNodeId, tenantId, cancellationToken);
                if (centralNode == null)
                    throw new InvalidOperationException($"Central node with ID {centralNodeId} not found for tenant {tenantId}");
                
                await BuildGraphFromNodeAsync(centralNode, tenantId, graph, processedNodeIds, relationTypes, 1, maxDistance, cancellationToken);
            }
            else
            {
                // Start from all nodes
                var allNodes = await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken);
                foreach (var node in allNodes)
                {
                    await BuildGraphFromNodeAsync(node, tenantId, graph, processedNodeIds, relationTypes, 1, maxDistance, cancellationToken);
                }
            }
            
            // Format the output according to the specified format
            return format.ToLowerInvariant() switch
            {
                "json" => JsonConvert.SerializeObject(graph, Formatting.Indented),
                "xml" => throw new NotImplementedException("XML format not yet implemented"),
                "dot" => GenerateDotGraph(graph, "Taxonomy Graph"),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };
        }
        
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
        public async Task<string> GenerateTreeMapVisualizationAsync(
            string tenantId, 
            string rootNodeId = null, 
            string sizeProperty = "count", 
            string colorProperty = "level", 
            string format = "json", 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            _logger.LogInformation("Generating tree map visualization for tenant {TenantId}, format {Format}", tenantId, format);
            
            // Fetch the root nodes
            List<TaxonomyNode> rootNodes;
            
            if (!string.IsNullOrEmpty(rootNodeId))
            {
                // Start from a specific node
                var rootNode = await _taxonomyService.GetNodeAsync(rootNodeId, tenantId, cancellationToken);
                if (rootNode == null)
                    throw new InvalidOperationException($"Root node with ID {rootNodeId} not found for tenant {tenantId}");
                
                rootNodes = new List<TaxonomyNode> { rootNode };
            }
            else
            {
                // Start from all root nodes
                rootNodes = (await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken)).ToList();
            }
            
            // Build the tree map nodes
            var treeMapNodes = new List<TreeMapNode>();
            
            foreach (var rootNode in rootNodes)
            {
                var node = await BuildTreeMapNodeAsync(rootNode, tenantId, sizeProperty, colorProperty, 0, cancellationToken);
                treeMapNodes.Add(node);
            }
            
            // Format the output according to the specified format
            return format.ToLowerInvariant() switch
            {
                "json" => JsonConvert.SerializeObject(treeMapNodes, Formatting.Indented),
                "svg" => throw new NotImplementedException("SVG format not yet implemented"),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };
        }
        
        #region Helper Methods
        
        private async Task<HierarchyNode> BuildHierarchyNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            int currentDepth, 
            int maxDepth, 
            CancellationToken cancellationToken)
        {
            var result = new HierarchyNode
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                NodeType = node.NodeType.ToString(),
                Level = currentDepth,
                Children = new List<HierarchyNode>()
            };
            
            // If max depth is reached, don't fetch children
            if (maxDepth > 0 && currentDepth >= maxDepth)
                return result;
            
            // Fetch and add children
            var children = await _taxonomyService.GetChildNodesAsync(node.Id, tenantId, false, cancellationToken);
            
            foreach (var child in children)
            {
                var childNode = await BuildHierarchyNodeAsync(child, tenantId, currentDepth + 1, maxDepth, cancellationToken);
                result.Children.Add(childNode);
            }
            
            return result;
        }
        
        private async Task BuildGraphFromNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            GraphVisualization graph, 
            HashSet<string> processedNodeIds, 
            TaxonomyRelationType[] relationTypes, 
            int currentDistance, 
            int maxDistance, 
            CancellationToken cancellationToken)
        {
            if (processedNodeIds.Contains(node.Id))
                return;
            
            // Add this node to the graph
            processedNodeIds.Add(node.Id);
            graph.Nodes.Add(new GraphNode
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                NodeType = node.NodeType.ToString()
            });
            
            // If max distance is reached, don't process further
            if (maxDistance > 0 && currentDistance >= maxDistance)
                return;
            
            // Get all relations from this node
            var outgoingRelations = await _taxonomyService.GetNodeRelationsAsync(node.Id, tenantId, null, true, cancellationToken);
            var incomingRelations = await _taxonomyService.GetNodeRelationsAsync(node.Id, tenantId, null, false, cancellationToken);
            
            var relations = outgoingRelations.Concat(incomingRelations);
            
            // Filter by relation type if specified
            if (relationTypes != null && relationTypes.Length > 0)
                relations = relations.Where(r => relationTypes.Contains(r.RelationType));
            
            foreach (var relation in relations)
            {
                // Add the relation to the graph
                var edgeExists = graph.Edges.Any(e =>
                    (e.SourceId == relation.SourceNodeId && e.TargetId == relation.TargetNodeId) ||
                    (relation.IsBidirectional && e.SourceId == relation.TargetNodeId && e.TargetId == relation.SourceNodeId));
                
                if (!edgeExists)
                {
                    graph.Edges.Add(new GraphEdge
                    {
                        Id = relation.Id,
                        SourceId = relation.SourceNodeId,
                        TargetId = relation.TargetNodeId,
                        Type = relation.RelationType.ToString(),
                        Bidirectional = relation.IsBidirectional
                    });
                }
                
                // Process the related node
                string relatedNodeId = relation.SourceNodeId == node.Id ? relation.TargetNodeId : relation.SourceNodeId;
                
                if (!processedNodeIds.Contains(relatedNodeId))
                {
                    var relatedNode = await _taxonomyService.GetNodeAsync(relatedNodeId, tenantId, cancellationToken);
                    if (relatedNode != null)
                        await BuildGraphFromNodeAsync(relatedNode, tenantId, graph, processedNodeIds, relationTypes, currentDistance + 1, maxDistance, cancellationToken);
                }
            }
        }
        
        private async Task<TreeMapNode> BuildTreeMapNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            string sizeProperty, 
            string colorProperty, 
            int level, 
            CancellationToken cancellationToken)
        {
            var result = new TreeMapNode
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                NodeType = node.NodeType.ToString(),
                Level = level,
                Children = new List<TreeMapNode>()
            };
            
            // Assign size property value
            if (sizeProperty == "count")
            {
                // Count will be calculated later after all children are added
                result.Size = 1;
            }
            else if (node.Properties.TryGetValue(sizeProperty, out var sizeValue))
            {
                if (sizeValue is int intValue)
                    result.Size = intValue;
                else if (sizeValue is double doubleValue)
                    result.Size = (int)doubleValue;
                else if (int.TryParse(sizeValue.ToString(), out int parsedValue))
                    result.Size = parsedValue;
                else
                    result.Size = 1; // Default size
            }
            else
            {
                result.Size = 1; // Default size
            }
            
            // Assign color property value
            if (colorProperty == "level")
            {
                result.Color = level.ToString();
            }
            else if (node.Properties.TryGetValue(colorProperty, out var colorValue))
            {
                result.Color = colorValue.ToString();
            }
            else
            {
                result.Color = node.NodeType.ToString(); // Default color
            }
            
            // Fetch and add children
            var children = await _taxonomyService.GetChildNodesAsync(node.Id, tenantId, false, cancellationToken);
            
            foreach (var child in children)
            {
                var childNode = await BuildTreeMapNodeAsync(child, tenantId, sizeProperty, colorProperty, level + 1, cancellationToken);
                result.Children.Add(childNode);
            }
            
            // If size property is "count", calculate the total count
            if (sizeProperty == "count")
            {
                result.Size = 1 + result.Children.Sum(c => c.Size);
            }
            
            return result;
        }
        
        private string GenerateDotGraph(List<HierarchyNode> nodes, string graphName)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"digraph \"{graphName}\" {{")
              .AppendLine("  node [shape=box, style=filled, color=lightblue];")
              .AppendLine("  edge [color=gray];");
            
            foreach (var node in nodes)
            {
                AppendHierarchyNodeToDot(sb, node, null);
            }
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        private void AppendHierarchyNodeToDot(StringBuilder sb, HierarchyNode node, string parentId)
        {
            // Add node
            sb.AppendLine($"  \"{node.Id}\" [label=\"{node.Name}\n{node.NodeType}\"];");
            
            // Add edge from parent if it exists
            if (parentId != null)
            {
                sb.AppendLine($"  \"{parentId}\" -> \"{node.Id}\";");
            }
            
            // Process children
            foreach (var child in node.Children)
            {
                AppendHierarchyNodeToDot(sb, child, node.Id);
            }
        }
        
        private string GenerateDotGraph(GraphVisualization graph, string graphName)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"digraph \"{graphName}\" {{")
              .AppendLine("  node [shape=box, style=filled, color=lightblue];")
              .AppendLine("  edge [color=gray];");
            
            // Add nodes
            foreach (var node in graph.Nodes)
            {
                sb.AppendLine($"  \"{node.Id}\" [label=\"{node.Name}\n{node.NodeType}\"];");
            }
            
            // Add edges
            foreach (var edge in graph.Edges)
            {
                string edgeStyle = edge.Bidirectional ? "dir=both" : "";
                sb.AppendLine($"  \"{edge.SourceId}\" -> \"{edge.TargetId}\" [label=\"{edge.Type}\" {edgeStyle}];");
            }
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        #endregion
        
        #region Visualization Models
        
        private class HierarchyNode
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string NodeType { get; set; }
            public int Level { get; set; }
            public List<HierarchyNode> Children { get; set; }
        }
        
        private class GraphVisualization
        {
            public List<GraphNode> Nodes { get; set; }
            public List<GraphEdge> Edges { get; set; }
        }
        
        private class GraphNode
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string NodeType { get; set; }
        }
        
        private class GraphEdge
        {
            public string Id { get; set; }
            public string SourceId { get; set; }
            public string TargetId { get; set; }
            public string Type { get; set; }
            public bool Bidirectional { get; set; }
        }
        
        private class TreeMapNode
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string NodeType { get; set; }
            public int Level { get; set; }
            public int Size { get; set; }
            public string Color { get; set; }
            public List<TreeMapNode> Children { get; set; }
        }
        
        #endregion
    }
} 