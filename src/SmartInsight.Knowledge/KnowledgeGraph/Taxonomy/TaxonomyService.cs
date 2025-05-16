using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy
{
    /// <summary>
    /// Implementation of the taxonomy service
    /// </summary>
    public class TaxonomyService : ITaxonomyService
    {
        private readonly ITaxonomyRepository _repository;
        private readonly ILogger<TaxonomyService> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyService"/> class
        /// </summary>
        /// <param name="repository">The taxonomy repository</param>
        /// <param name="logger">The logger</param>
        public TaxonomyService(ITaxonomyRepository repository, ILogger<TaxonomyService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Creates a new taxonomy node
        /// </summary>
        /// <param name="node">The node to create</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created node</returns>
        public async Task<TaxonomyNode> CreateNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            node.TenantId = tenantId;
            node.CreatedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Creating taxonomy node {NodeName} for tenant {TenantId}", node.Name, tenantId);
            
            return await _repository.CreateNodeAsync(node, cancellationToken);
        }
        
        /// <summary>
        /// Updates an existing taxonomy node
        /// </summary>
        /// <param name="node">The node with updated values</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated node</returns>
        public async Task<TaxonomyNode> UpdateNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Ensure the node belongs to the specified tenant
            var existingNode = await _repository.GetNodeAsync(node.Id, tenantId, cancellationToken);
            if (existingNode == null)
            {
                throw new InvalidOperationException($"Node with ID {node.Id} not found for tenant {tenantId}");
            }
            
            node.TenantId = tenantId;
            node.UpdatedAt = DateTime.UtcNow;
            node.Version++;
            
            _logger.LogInformation("Updating taxonomy node {NodeId} for tenant {TenantId}", node.Id, tenantId);
            
            return await _repository.UpdateNodeAsync(node, cancellationToken);
        }
        
        /// <summary>
        /// Deletes a taxonomy node
        /// </summary>
        /// <param name="nodeId">The ID of the node to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="recursive">Whether to delete child nodes recursively</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteNodeAsync(
            string nodeId, 
            string tenantId, 
            bool recursive = false, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            if (recursive)
            {
                // Get all child nodes
                var children = await GetChildNodesAsync(nodeId, tenantId, true, cancellationToken);
                
                // Delete relations for all nodes
                foreach (var child in children)
                {
                    var relations = await _repository.GetNodeRelationsAsync(child.Id, tenantId, null, true, cancellationToken);
                    foreach (var relation in relations)
                    {
                        await _repository.DeleteRelationAsync(relation.Id, tenantId, cancellationToken);
                    }
                    
                    relations = await _repository.GetNodeRelationsAsync(child.Id, tenantId, null, false, cancellationToken);
                    foreach (var relation in relations)
                    {
                        await _repository.DeleteRelationAsync(relation.Id, tenantId, cancellationToken);
                    }
                }
                
                // Delete child nodes in reverse (to delete leaf nodes first)
                foreach (var child in children.Reverse())
                {
                    await _repository.DeleteNodeAsync(child.Id, tenantId, cancellationToken);
                }
            }
            
            // Delete relations for this node
            var nodeRelations = await _repository.GetNodeRelationsAsync(nodeId, tenantId, null, true, cancellationToken);
            foreach (var relation in nodeRelations)
            {
                await _repository.DeleteRelationAsync(relation.Id, tenantId, cancellationToken);
            }
            
            nodeRelations = await _repository.GetNodeRelationsAsync(nodeId, tenantId, null, false, cancellationToken);
            foreach (var relation in nodeRelations)
            {
                await _repository.DeleteRelationAsync(relation.Id, tenantId, cancellationToken);
            }
            
            _logger.LogInformation("Deleting taxonomy node {NodeId} for tenant {TenantId} (recursive: {Recursive})", 
                nodeId, tenantId, recursive);
            
            return await _repository.DeleteNodeAsync(nodeId, tenantId, cancellationToken);
        }
        
        /// <summary>
        /// Gets a taxonomy node by ID
        /// </summary>
        /// <param name="nodeId">The ID of the node to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The taxonomy node if found, null otherwise</returns>
        public async Task<TaxonomyNode> GetNodeAsync(
            string nodeId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            return await _repository.GetNodeAsync(nodeId, tenantId, cancellationToken);
        }
        
        /// <summary>
        /// Gets all child nodes of a parent node
        /// </summary>
        /// <param name="parentId">The ID of the parent node</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="recursive">Whether to get all descendants recursively</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The child nodes</returns>
        public async Task<IEnumerable<TaxonomyNode>> GetChildNodesAsync(
            string parentId, 
            string tenantId, 
            bool recursive = false, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(parentId)) throw new ArgumentException("Parent ID cannot be null or empty", nameof(parentId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Get all nodes for the tenant
            var allNodes = await _repository.GetAllNodesAsync(tenantId, cancellationToken);
            
            // Filter direct children
            var directChildren = allNodes.Where(n => n.ParentId == parentId).ToList();
            
            if (!recursive)
            {
                return directChildren;
            }
            
            // If recursive, add all descendants
            var result = new List<TaxonomyNode>(directChildren);
            foreach (var child in directChildren)
            {
                var descendants = await GetChildNodesAsync(child.Id, tenantId, true, cancellationToken);
                result.AddRange(descendants);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all root nodes (nodes without a parent)
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The root nodes</returns>
        public async Task<IEnumerable<TaxonomyNode>> GetRootNodesAsync(
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Get all nodes for the tenant
            var allNodes = await _repository.GetAllNodesAsync(tenantId, cancellationToken);
            
            // Filter root nodes (nodes without a parent or with a non-existent parent)
            return allNodes.Where(n => string.IsNullOrEmpty(n.ParentId));
        }
        
        /// <summary>
        /// Creates a relation between two taxonomy nodes
        /// </summary>
        /// <param name="relation">The relation to create</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created relation</returns>
        public async Task<TaxonomyRelation> CreateRelationAsync(
            TaxonomyRelation relation, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (relation == null) throw new ArgumentNullException(nameof(relation));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Validate source and target nodes exist
            var sourceNode = await _repository.GetNodeAsync(relation.SourceNodeId, tenantId, cancellationToken);
            if (sourceNode == null)
            {
                throw new InvalidOperationException($"Source node with ID {relation.SourceNodeId} not found for tenant {tenantId}");
            }
            
            var targetNode = await _repository.GetNodeAsync(relation.TargetNodeId, tenantId, cancellationToken);
            if (targetNode == null)
            {
                throw new InvalidOperationException($"Target node with ID {relation.TargetNodeId} not found for tenant {tenantId}");
            }
            
            relation.TenantId = tenantId;
            relation.CreatedAt = DateTime.UtcNow;
            relation.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Creating relation between nodes {SourceNodeId} and {TargetNodeId} for tenant {TenantId}", 
                relation.SourceNodeId, relation.TargetNodeId, tenantId);
            
            return await _repository.CreateRelationAsync(relation, cancellationToken);
        }
        
        /// <summary>
        /// Deletes a relation between taxonomy nodes
        /// </summary>
        /// <param name="relationId">The ID of the relation to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteRelationAsync(
            string relationId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(relationId)) throw new ArgumentException("Relation ID cannot be null or empty", nameof(relationId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            _logger.LogInformation("Deleting relation {RelationId} for tenant {TenantId}", relationId, tenantId);
            
            return await _repository.DeleteRelationAsync(relationId, tenantId, cancellationToken);
        }
        
        /// <summary>
        /// Gets all relations for a node
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="relationType">Optional relation type filter</param>
        /// <param name="isOutgoing">If true, get relations where the node is the source, otherwise get relations where the node is the target</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relations</returns>
        public async Task<IEnumerable<TaxonomyRelation>> GetNodeRelationsAsync(
            string nodeId, 
            string tenantId, 
            TaxonomyRelationType? relationType = null, 
            bool isOutgoing = true, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            return await _repository.GetNodeRelationsAsync(nodeId, tenantId, relationType, isOutgoing, cancellationToken);
        }
        
        /// <summary>
        /// Creates an inheritance rule
        /// </summary>
        /// <param name="rule">The rule to create</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created rule</returns>
        public async Task<TaxonomyInheritanceRule> CreateInheritanceRuleAsync(
            TaxonomyInheritanceRule rule, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            rule.TenantId = tenantId;
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Creating inheritance rule for tenant {TenantId}", tenantId);
            
            return await _repository.CreateInheritanceRuleAsync(rule, cancellationToken);
        }
        
        /// <summary>
        /// Updates an inheritance rule
        /// </summary>
        /// <param name="rule">The rule with updated values</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated rule</returns>
        public async Task<TaxonomyInheritanceRule> UpdateInheritanceRuleAsync(
            TaxonomyInheritanceRule rule, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Ensure the rule belongs to the specified tenant
            var existingRule = await _repository.GetInheritanceRuleAsync(rule.Id, tenantId, cancellationToken);
            if (existingRule == null)
            {
                throw new InvalidOperationException($"Inheritance rule with ID {rule.Id} not found for tenant {tenantId}");
            }
            
            rule.TenantId = tenantId;
            rule.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Updating inheritance rule {RuleId} for tenant {TenantId}", rule.Id, tenantId);
            
            return await _repository.UpdateInheritanceRuleAsync(rule, cancellationToken);
        }
        
        /// <summary>
        /// Deletes an inheritance rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteInheritanceRuleAsync(
            string ruleId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ruleId)) throw new ArgumentException("Rule ID cannot be null or empty", nameof(ruleId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            _logger.LogInformation("Deleting inheritance rule {RuleId} for tenant {TenantId}", ruleId, tenantId);
            
            return await _repository.DeleteInheritanceRuleAsync(ruleId, tenantId, cancellationToken);
        }
        
        /// <summary>
        /// Gets all inheritance rules for a node type
        /// </summary>
        /// <param name="nodeTypeId">The ID of the node type</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The inheritance rules</returns>
        public async Task<IEnumerable<TaxonomyInheritanceRule>> GetInheritanceRulesAsync(
            string nodeTypeId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeTypeId)) throw new ArgumentException("Node type ID cannot be null or empty", nameof(nodeTypeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            return await _repository.GetInheritanceRulesByNodeTypeAsync(nodeTypeId, tenantId, cancellationToken);
        }
    }
} 