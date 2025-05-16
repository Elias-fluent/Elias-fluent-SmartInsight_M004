using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy
{
    /// <summary>
    /// Resolves inheritance in the taxonomy structure according to inheritance rules
    /// </summary>
    public class TaxonomyInheritanceResolver
    {
        private readonly ITaxonomyService _taxonomyService;
        private readonly ILogger<TaxonomyInheritanceResolver> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyInheritanceResolver"/> class
        /// </summary>
        /// <param name="taxonomyService">The taxonomy service</param>
        /// <param name="logger">The logger</param>
        public TaxonomyInheritanceResolver(ITaxonomyService taxonomyService, ILogger<TaxonomyInheritanceResolver> logger)
        {
            _taxonomyService = taxonomyService ?? throw new ArgumentNullException(nameof(taxonomyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Applies inheritance rules to a node and its descendants
        /// </summary>
        /// <param name="nodeId">The ID of the node to apply inheritance to</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="recursive">Whether to apply inheritance recursively to descendants</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ApplyInheritanceAsync(
            string nodeId, 
            string tenantId, 
            bool recursive = true, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            try
            {
                var node = await _taxonomyService.GetNodeAsync(nodeId, tenantId, cancellationToken);
                if (node == null)
                {
                    _logger.LogWarning("Node {NodeId} not found for tenant {TenantId}", nodeId, tenantId);
                    return false;
                }
                
                // Get the parent node if it exists
                TaxonomyNode parentNode = null;
                if (!string.IsNullOrEmpty(node.ParentId))
                {
                    parentNode = await _taxonomyService.GetNodeAsync(node.ParentId, tenantId, cancellationToken);
                }
                
                // Apply downward inheritance from parent to this node
                if (parentNode != null)
                {
                    await ApplyDownwardInheritanceAsync(parentNode, node, tenantId, cancellationToken);
                }
                
                // Get children if recursive
                if (recursive)
                {
                    var children = await _taxonomyService.GetChildNodesAsync(nodeId, tenantId, false, cancellationToken);
                    foreach (var child in children)
                    {
                        // Apply inheritance to each child
                        await ApplyInheritanceAsync(child.Id, tenantId, true, cancellationToken);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying inheritance to node {NodeId} for tenant {TenantId}", nodeId, tenantId);
                return false;
            }
        }
        
        /// <summary>
        /// Applies upward inheritance rules from a node to its ancestors
        /// </summary>
        /// <param name="nodeId">The ID of the node to start from</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ApplyUpwardInheritanceAsync(
            string nodeId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            try
            {
                var node = await _taxonomyService.GetNodeAsync(nodeId, tenantId, cancellationToken);
                if (node == null)
                {
                    _logger.LogWarning("Node {NodeId} not found for tenant {TenantId}", nodeId, tenantId);
                    return false;
                }
                
                // If the node has no parent, there's nothing to do
                if (string.IsNullOrEmpty(node.ParentId))
                {
                    return true;
                }
                
                var parentNode = await _taxonomyService.GetNodeAsync(node.ParentId, tenantId, cancellationToken);
                if (parentNode == null)
                {
                    _logger.LogWarning("Parent node {ParentId} not found for node {NodeId}, tenant {TenantId}", 
                        node.ParentId, nodeId, tenantId);
                    return false;
                }
                
                // Apply upward inheritance from this node to its parent
                await ApplyUpwardInheritanceAsync(node, parentNode, tenantId, cancellationToken);
                
                // Continue up the hierarchy
                await ApplyUpwardInheritanceAsync(parentNode.Id, tenantId, cancellationToken);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying upward inheritance from node {NodeId} for tenant {TenantId}", 
                    nodeId, tenantId);
                return false;
            }
        }
        
        /// <summary>
        /// Applies sibling sharing rules between nodes with the same parent
        /// </summary>
        /// <param name="nodeId">The ID of one of the sibling nodes</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ApplySiblingSharingAsync(
            string nodeId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            try
            {
                var node = await _taxonomyService.GetNodeAsync(nodeId, tenantId, cancellationToken);
                if (node == null)
                {
                    _logger.LogWarning("Node {NodeId} not found for tenant {TenantId}", nodeId, tenantId);
                    return false;
                }
                
                // If the node has no parent, there are no siblings
                if (string.IsNullOrEmpty(node.ParentId))
                {
                    return true;
                }
                
                // Get all siblings (nodes with the same parent)
                var siblings = await _taxonomyService.GetChildNodesAsync(node.ParentId, tenantId, false, cancellationToken);
                
                // Filter out the node itself
                siblings = siblings.Where(s => s.Id != nodeId).ToList();
                
                // Apply sibling sharing rules
                foreach (var sibling in siblings)
                {
                    await ApplySiblingSharingAsync(node, sibling, tenantId, cancellationToken);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying sibling sharing for node {NodeId} for tenant {TenantId}", 
                    nodeId, tenantId);
                return false;
            }
        }
        
        #region Private Helper Methods
        
        private async Task ApplyDownwardInheritanceAsync(
            TaxonomyNode parentNode, 
            TaxonomyNode childNode, 
            string tenantId, 
            CancellationToken cancellationToken)
        {
            // Get inheritance rules for the parent node type
            var rules = await _taxonomyService.GetInheritanceRulesAsync(parentNode.NodeType.ToString(), tenantId, cancellationToken);
            
            // Filter for downward inheritance rules that match the parent and child node types
            var downwardRules = rules
                .Where(r => r.RuleType == InheritanceRuleType.DownwardInheritance &&
                           r.SourceNodeTypeId == parentNode.NodeType.ToString() &&
                           r.TargetNodeTypeId == childNode.NodeType.ToString() &&
                           r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToList();
            
            if (!downwardRules.Any())
            {
                _logger.LogDebug("No downward inheritance rules found for parent {ParentId} to child {ChildId}", 
                    parentNode.Id, childNode.Id);
                return;
            }
            
            // Apply each rule in priority order
            foreach (var rule in downwardRules)
            {
                ApplyInheritanceRule(parentNode, childNode, rule);
            }
            
            // Update the child node
            await _taxonomyService.UpdateNodeAsync(childNode, tenantId, cancellationToken);
        }
        
        private async Task ApplyUpwardInheritanceAsync(
            TaxonomyNode childNode, 
            TaxonomyNode parentNode, 
            string tenantId, 
            CancellationToken cancellationToken)
        {
            // Get inheritance rules for the child node type
            var rules = await _taxonomyService.GetInheritanceRulesAsync(childNode.NodeType.ToString(), tenantId, cancellationToken);
            
            // Filter for upward inheritance rules that match the child and parent node types
            var upwardRules = rules
                .Where(r => r.RuleType == InheritanceRuleType.UpwardPropagation &&
                           r.SourceNodeTypeId == childNode.NodeType.ToString() &&
                           r.TargetNodeTypeId == parentNode.NodeType.ToString() &&
                           r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToList();
            
            if (!upwardRules.Any())
            {
                _logger.LogDebug("No upward inheritance rules found for child {ChildId} to parent {ParentId}", 
                    childNode.Id, parentNode.Id);
                return;
            }
            
            // Apply each rule in priority order
            foreach (var rule in upwardRules)
            {
                ApplyInheritanceRule(childNode, parentNode, rule);
            }
            
            // Update the parent node
            await _taxonomyService.UpdateNodeAsync(parentNode, tenantId, cancellationToken);
        }
        
        private async Task ApplySiblingSharingAsync(
            TaxonomyNode sourceNode, 
            TaxonomyNode targetNode, 
            string tenantId, 
            CancellationToken cancellationToken)
        {
            // Get inheritance rules for the source node type
            var rules = await _taxonomyService.GetInheritanceRulesAsync(sourceNode.NodeType.ToString(), tenantId, cancellationToken);
            
            // Filter for sibling sharing rules that match the source and target node types
            var siblingRules = rules
                .Where(r => r.RuleType == InheritanceRuleType.SiblingSharing &&
                           r.SourceNodeTypeId == sourceNode.NodeType.ToString() &&
                           r.TargetNodeTypeId == targetNode.NodeType.ToString() &&
                           r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToList();
            
            if (!siblingRules.Any())
            {
                _logger.LogDebug("No sibling sharing rules found for source {SourceId} to target {TargetId}", 
                    sourceNode.Id, targetNode.Id);
                return;
            }
            
            // Apply each rule in priority order
            foreach (var rule in siblingRules)
            {
                ApplyInheritanceRule(sourceNode, targetNode, rule);
            }
            
            // Update the target node
            await _taxonomyService.UpdateNodeAsync(targetNode, tenantId, cancellationToken);
        }
        
        private void ApplyInheritanceRule(TaxonomyNode sourceNode, TaxonomyNode targetNode, TaxonomyInheritanceRule rule)
        {
            _logger.LogDebug("Applying inheritance rule {RuleId} from {SourceId} to {TargetId}", 
                rule.Id, sourceNode.Id, targetNode.Id);
            
            // Get the properties to include (all if empty)
            var propertiesToInclude = rule.IncludedProperties.Count > 0 
                ? rule.IncludedProperties 
                : sourceNode.Properties.Keys.ToList();
            
            // Apply property inheritance
            foreach (var propertyName in propertiesToInclude)
            {
                // Skip excluded properties
                if (rule.ExcludedProperties.Contains(propertyName))
                {
                    continue;
                }
                
                // Skip if the source doesn't have the property
                if (!sourceNode.Properties.TryGetValue(propertyName, out var propertyValue))
                {
                    continue;
                }
                
                // Handle merging or replacing
                if (rule.MergeValues && targetNode.Properties.TryGetValue(propertyName, out var existingValue))
                {
                    // For simple collections, merge values
                    if (propertyValue is IEnumerable<object> sourceCollection && existingValue is IEnumerable<object> targetCollection)
                    {
                        // This is a simple merge that doesn't handle complex merging scenarios
                        // For a real system, this would need to be more sophisticated
                        targetNode.Properties[propertyName] = targetCollection.Union(sourceCollection).ToList();
                    }
                    else
                    {
                        // For other types, just replace
                        targetNode.Properties[propertyName] = propertyValue;
                    }
                }
                else
                {
                    // Replace the property value
                    targetNode.Properties[propertyName] = propertyValue;
                }
            }
            
            // Update the target node's UpdatedAt timestamp
            targetNode.UpdatedAt = DateTime.UtcNow;
        }
        
        #endregion
    }
} 