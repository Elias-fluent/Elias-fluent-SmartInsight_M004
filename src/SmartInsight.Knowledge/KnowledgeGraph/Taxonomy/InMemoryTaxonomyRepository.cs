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
    /// In-memory implementation of the taxonomy repository for development and testing purposes
    /// </summary>
    public class InMemoryTaxonomyRepository : ITaxonomyRepository
    {
        private readonly ILogger<InMemoryTaxonomyRepository> _logger;
        private readonly Dictionary<string, Dictionary<string, TaxonomyNode>> _nodesPerTenant;
        private readonly Dictionary<string, Dictionary<string, TaxonomyRelation>> _relationsPerTenant;
        private readonly Dictionary<string, Dictionary<string, TaxonomyInheritanceRule>> _rulesPerTenant;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryTaxonomyRepository"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public InMemoryTaxonomyRepository(ILogger<InMemoryTaxonomyRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodesPerTenant = new Dictionary<string, Dictionary<string, TaxonomyNode>>();
            _relationsPerTenant = new Dictionary<string, Dictionary<string, TaxonomyRelation>>();
            _rulesPerTenant = new Dictionary<string, Dictionary<string, TaxonomyInheritanceRule>>();
        }
        
        #region Nodes
        
        /// <summary>
        /// Creates a new taxonomy node
        /// </summary>
        /// <param name="node">The node to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created node</returns>
        public Task<TaxonomyNode> CreateNodeAsync(TaxonomyNode node, CancellationToken cancellationToken = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrEmpty(node.TenantId)) throw new ArgumentException("Node must have a tenant ID", nameof(node));
            
            // Initialize tenant collection if needed
            if (!_nodesPerTenant.ContainsKey(node.TenantId))
            {
                _nodesPerTenant[node.TenantId] = new Dictionary<string, TaxonomyNode>();
            }
            
            // Ensure the node has an ID
            if (string.IsNullOrEmpty(node.Id))
            {
                node.Id = Guid.NewGuid().ToString();
            }
            
            // Add the node
            _nodesPerTenant[node.TenantId][node.Id] = node;
            
            _logger.LogInformation("Created taxonomy node {NodeId} for tenant {TenantId}", node.Id, node.TenantId);
            
            return Task.FromResult(node);
        }
        
        /// <summary>
        /// Updates an existing taxonomy node
        /// </summary>
        /// <param name="node">The node with updated values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated node</returns>
        public Task<TaxonomyNode> UpdateNodeAsync(TaxonomyNode node, CancellationToken cancellationToken = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrEmpty(node.TenantId)) throw new ArgumentException("Node must have a tenant ID", nameof(node));
            if (string.IsNullOrEmpty(node.Id)) throw new ArgumentException("Node must have an ID", nameof(node));
            
            // Check if the tenant exists
            if (!_nodesPerTenant.ContainsKey(node.TenantId) || !_nodesPerTenant[node.TenantId].ContainsKey(node.Id))
            {
                throw new InvalidOperationException($"Node with ID {node.Id} not found for tenant {node.TenantId}");
            }
            
            // Update the node
            _nodesPerTenant[node.TenantId][node.Id] = node;
            
            _logger.LogInformation("Updated taxonomy node {NodeId} for tenant {TenantId}", node.Id, node.TenantId);
            
            return Task.FromResult(node);
        }
        
        /// <summary>
        /// Deletes a taxonomy node
        /// </summary>
        /// <param name="nodeId">The ID of the node to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public Task<bool> DeleteNodeAsync(string nodeId, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_nodesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(false);
            }
            
            // Check if the node exists
            if (!_nodesPerTenant[tenantId].ContainsKey(nodeId))
            {
                return Task.FromResult(false);
            }
            
            // Remove the node
            bool result = _nodesPerTenant[tenantId].Remove(nodeId);
            
            if (result)
            {
                _logger.LogInformation("Deleted taxonomy node {NodeId} for tenant {TenantId}", nodeId, tenantId);
            }
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Gets a taxonomy node by ID
        /// </summary>
        /// <param name="nodeId">The ID of the node to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The taxonomy node if found, null otherwise</returns>
        public Task<TaxonomyNode> GetNodeAsync(string nodeId, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_nodesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult<TaxonomyNode>(null);
            }
            
            // Check if the node exists
            if (!_nodesPerTenant[tenantId].TryGetValue(nodeId, out var node))
            {
                return Task.FromResult<TaxonomyNode>(null);
            }
            
            return Task.FromResult(node);
        }
        
        /// <summary>
        /// Gets all nodes for a tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All nodes for the tenant</returns>
        public Task<IEnumerable<TaxonomyNode>> GetAllNodesAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_nodesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(Enumerable.Empty<TaxonomyNode>());
            }
            
            return Task.FromResult(_nodesPerTenant[tenantId].Values.AsEnumerable());
        }
        
        /// <summary>
        /// Gets nodes matching a filter
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="nameFilter">Optional name filter</param>
        /// <param name="nodeType">Optional node type filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching nodes</returns>
        public Task<IEnumerable<TaxonomyNode>> FindNodesAsync(
            string tenantId, 
            string nameFilter = null, 
            TaxonomyNodeType? nodeType = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_nodesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(Enumerable.Empty<TaxonomyNode>());
            }
            
            // Apply filters
            var query = _nodesPerTenant[tenantId].Values.AsEnumerable();
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                var filter = nameFilter.ToLowerInvariant();
                query = query.Where(n => n.Name.ToLowerInvariant().Contains(filter));
            }
            
            if (nodeType.HasValue)
            {
                query = query.Where(n => n.NodeType == nodeType.Value);
            }
            
            return Task.FromResult(query);
        }
        
        #endregion
        
        #region Relations
        
        /// <summary>
        /// Creates a relation between taxonomy nodes
        /// </summary>
        /// <param name="relation">The relation to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created relation</returns>
        public Task<TaxonomyRelation> CreateRelationAsync(TaxonomyRelation relation, CancellationToken cancellationToken = default)
        {
            if (relation == null) throw new ArgumentNullException(nameof(relation));
            if (string.IsNullOrEmpty(relation.TenantId)) throw new ArgumentException("Relation must have a tenant ID", nameof(relation));
            
            // Initialize tenant collection if needed
            if (!_relationsPerTenant.ContainsKey(relation.TenantId))
            {
                _relationsPerTenant[relation.TenantId] = new Dictionary<string, TaxonomyRelation>();
            }
            
            // Ensure the relation has an ID
            if (string.IsNullOrEmpty(relation.Id))
            {
                relation.Id = Guid.NewGuid().ToString();
            }
            
            // Add the relation
            _relationsPerTenant[relation.TenantId][relation.Id] = relation;
            
            _logger.LogInformation("Created taxonomy relation {RelationId} for tenant {TenantId}", relation.Id, relation.TenantId);
            
            return Task.FromResult(relation);
        }
        
        /// <summary>
        /// Updates an existing relation
        /// </summary>
        /// <param name="relation">The relation with updated values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated relation</returns>
        public Task<TaxonomyRelation> UpdateRelationAsync(TaxonomyRelation relation, CancellationToken cancellationToken = default)
        {
            if (relation == null) throw new ArgumentNullException(nameof(relation));
            if (string.IsNullOrEmpty(relation.TenantId)) throw new ArgumentException("Relation must have a tenant ID", nameof(relation));
            if (string.IsNullOrEmpty(relation.Id)) throw new ArgumentException("Relation must have an ID", nameof(relation));
            
            // Check if the tenant exists
            if (!_relationsPerTenant.ContainsKey(relation.TenantId) || !_relationsPerTenant[relation.TenantId].ContainsKey(relation.Id))
            {
                throw new InvalidOperationException($"Relation with ID {relation.Id} not found for tenant {relation.TenantId}");
            }
            
            // Update the relation
            _relationsPerTenant[relation.TenantId][relation.Id] = relation;
            
            _logger.LogInformation("Updated taxonomy relation {RelationId} for tenant {TenantId}", relation.Id, relation.TenantId);
            
            return Task.FromResult(relation);
        }
        
        /// <summary>
        /// Deletes a relation
        /// </summary>
        /// <param name="relationId">The ID of the relation to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public Task<bool> DeleteRelationAsync(string relationId, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(relationId)) throw new ArgumentException("Relation ID cannot be null or empty", nameof(relationId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_relationsPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(false);
            }
            
            // Check if the relation exists
            if (!_relationsPerTenant[tenantId].ContainsKey(relationId))
            {
                return Task.FromResult(false);
            }
            
            // Remove the relation
            bool result = _relationsPerTenant[tenantId].Remove(relationId);
            
            if (result)
            {
                _logger.LogInformation("Deleted taxonomy relation {RelationId} for tenant {TenantId}", relationId, tenantId);
            }
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Gets a relation by ID
        /// </summary>
        /// <param name="relationId">The ID of the relation to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relation if found, null otherwise</returns>
        public Task<TaxonomyRelation> GetRelationAsync(string relationId, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(relationId)) throw new ArgumentException("Relation ID cannot be null or empty", nameof(relationId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_relationsPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult<TaxonomyRelation>(null);
            }
            
            // Check if the relation exists
            if (!_relationsPerTenant[tenantId].TryGetValue(relationId, out var relation))
            {
                return Task.FromResult<TaxonomyRelation>(null);
            }
            
            return Task.FromResult(relation);
        }
        
        /// <summary>
        /// Gets relations for a node (source or target)
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="relationType">Optional relation type filter</param>
        /// <param name="isSource">If true, get relations where the node is the source, otherwise get relations where the node is the target</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relations</returns>
        public Task<IEnumerable<TaxonomyRelation>> GetNodeRelationsAsync(
            string nodeId, 
            string tenantId, 
            TaxonomyRelationType? relationType = null, 
            bool isSource = true, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentException("Node ID cannot be null or empty", nameof(nodeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_relationsPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(Enumerable.Empty<TaxonomyRelation>());
            }
            
            // Filter relations
            var relations = _relationsPerTenant[tenantId].Values.AsEnumerable();
            
            // Filter by node
            if (isSource)
            {
                relations = relations.Where(r => r.SourceNodeId == nodeId);
            }
            else
            {
                relations = relations.Where(r => r.TargetNodeId == nodeId);
            }
            
            // Filter by relation type
            if (relationType.HasValue)
            {
                relations = relations.Where(r => r.RelationType == relationType.Value);
            }
            
            return Task.FromResult(relations);
        }
        
        #endregion
        
        #region Inheritance Rules
        
        /// <summary>
        /// Creates an inheritance rule
        /// </summary>
        /// <param name="rule">The rule to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created rule</returns>
        public Task<TaxonomyInheritanceRule> CreateInheritanceRuleAsync(TaxonomyInheritanceRule rule, CancellationToken cancellationToken = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(rule.TenantId)) throw new ArgumentException("Rule must have a tenant ID", nameof(rule));
            
            // Initialize tenant collection if needed
            if (!_rulesPerTenant.ContainsKey(rule.TenantId))
            {
                _rulesPerTenant[rule.TenantId] = new Dictionary<string, TaxonomyInheritanceRule>();
            }
            
            // Ensure the rule has an ID
            if (string.IsNullOrEmpty(rule.Id))
            {
                rule.Id = Guid.NewGuid().ToString();
            }
            
            // Add the rule
            _rulesPerTenant[rule.TenantId][rule.Id] = rule;
            
            _logger.LogInformation("Created taxonomy inheritance rule {RuleId} for tenant {TenantId}", rule.Id, rule.TenantId);
            
            return Task.FromResult(rule);
        }
        
        /// <summary>
        /// Updates an existing inheritance rule
        /// </summary>
        /// <param name="rule">The rule with updated values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated rule</returns>
        public Task<TaxonomyInheritanceRule> UpdateInheritanceRuleAsync(TaxonomyInheritanceRule rule, CancellationToken cancellationToken = default)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(rule.TenantId)) throw new ArgumentException("Rule must have a tenant ID", nameof(rule));
            if (string.IsNullOrEmpty(rule.Id)) throw new ArgumentException("Rule must have an ID", nameof(rule));
            
            // Check if the tenant exists
            if (!_rulesPerTenant.ContainsKey(rule.TenantId) || !_rulesPerTenant[rule.TenantId].ContainsKey(rule.Id))
            {
                throw new InvalidOperationException($"Inheritance rule with ID {rule.Id} not found for tenant {rule.TenantId}");
            }
            
            // Update the rule
            _rulesPerTenant[rule.TenantId][rule.Id] = rule;
            
            _logger.LogInformation("Updated taxonomy inheritance rule {RuleId} for tenant {TenantId}", rule.Id, rule.TenantId);
            
            return Task.FromResult(rule);
        }
        
        /// <summary>
        /// Deletes an inheritance rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public Task<bool> DeleteInheritanceRuleAsync(string ruleId, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ruleId)) throw new ArgumentException("Rule ID cannot be null or empty", nameof(ruleId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_rulesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(false);
            }
            
            // Check if the rule exists
            if (!_rulesPerTenant[tenantId].ContainsKey(ruleId))
            {
                return Task.FromResult(false);
            }
            
            // Remove the rule
            bool result = _rulesPerTenant[tenantId].Remove(ruleId);
            
            if (result)
            {
                _logger.LogInformation("Deleted taxonomy inheritance rule {RuleId} for tenant {TenantId}", ruleId, tenantId);
            }
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Gets an inheritance rule by ID
        /// </summary>
        /// <param name="ruleId">The ID of the rule to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The rule if found, null otherwise</returns>
        public Task<TaxonomyInheritanceRule> GetInheritanceRuleAsync(string ruleId, string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ruleId)) throw new ArgumentException("Rule ID cannot be null or empty", nameof(ruleId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_rulesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult<TaxonomyInheritanceRule>(null);
            }
            
            // Check if the rule exists
            if (!_rulesPerTenant[tenantId].TryGetValue(ruleId, out var rule))
            {
                return Task.FromResult<TaxonomyInheritanceRule>(null);
            }
            
            return Task.FromResult(rule);
        }
        
        /// <summary>
        /// Gets all inheritance rules for a tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All inheritance rules for the tenant</returns>
        public Task<IEnumerable<TaxonomyInheritanceRule>> GetAllInheritanceRulesAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_rulesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(Enumerable.Empty<TaxonomyInheritanceRule>());
            }
            
            return Task.FromResult(_rulesPerTenant[tenantId].Values.AsEnumerable());
        }
        
        /// <summary>
        /// Gets inheritance rules for a node type
        /// </summary>
        /// <param name="nodeTypeId">The ID of the node type</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The inheritance rules</returns>
        public Task<IEnumerable<TaxonomyInheritanceRule>> GetInheritanceRulesByNodeTypeAsync(
            string nodeTypeId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(nodeTypeId)) throw new ArgumentException("Node type ID cannot be null or empty", nameof(nodeTypeId));
            if (string.IsNullOrEmpty(tenantId)) throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            
            // Check if the tenant exists
            if (!_rulesPerTenant.ContainsKey(tenantId))
            {
                return Task.FromResult(Enumerable.Empty<TaxonomyInheritanceRule>());
            }
            
            // Filter rules by node type (either source or target)
            var rules = _rulesPerTenant[tenantId].Values.Where(r => 
                r.SourceNodeTypeId == nodeTypeId || r.TargetNodeTypeId == nodeTypeId);
            
            return Task.FromResult(rules.AsEnumerable());
        }
        
        #endregion
    }
} 