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
    /// Service for validating the consistency of the taxonomy structure
    /// </summary>
    public class TaxonomyValidationService
    {
        private readonly ITaxonomyService _taxonomyService;
        private readonly ILogger<TaxonomyValidationService> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyValidationService"/> class
        /// </summary>
        /// <param name="taxonomyService">The taxonomy service</param>
        /// <param name="logger">The logger</param>
        public TaxonomyValidationService(ITaxonomyService taxonomyService, ILogger<TaxonomyValidationService> logger)
        {
            _taxonomyService = taxonomyService ?? throw new ArgumentNullException(nameof(taxonomyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates the hierarchical structure of the taxonomy
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of validation issues found, empty list if no issues</returns>
        public async Task<IList<TaxonomyValidationIssue>> ValidateHierarchyAsync(
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            var issues = new List<TaxonomyValidationIssue>();
            
            try
            {
                // Get all nodes for the tenant
                var rootNodes = await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken);
                
                // Track visited nodes to detect cycles
                var visitedNodes = new HashSet<string>();
                
                // Validate each root node and its descendants
                foreach (var rootNode in rootNodes)
                {
                    await ValidateNodeHierarchyAsync(rootNode, null, visitedNodes, issues, tenantId, cancellationToken);
                }
                
                // Find orphaned nodes (nodes with a parent ID that doesn't exist)
                await FindOrphanedNodesAsync(tenantId, issues, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating taxonomy hierarchy for tenant {TenantId}", tenantId);
                issues.Add(new TaxonomyValidationIssue
                {
                    IssueType = ValidationIssueType.SystemError,
                    Message = $"System error during validation: {ex.Message}",
                    Severity = ValidationIssueSeverity.Error
                });
            }
            
            return issues;
        }
        
        /// <summary>
        /// Validates the consistency of relations in the taxonomy
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of validation issues found, empty list if no issues</returns>
        public async Task<IList<TaxonomyValidationIssue>> ValidateRelationsAsync(
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            var issues = new List<TaxonomyValidationIssue>();
            
            try
            {
                // Get all nodes for the tenant
                var rootNodes = await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken);
                var nodes = new List<TaxonomyNode>(rootNodes);
                
                // Get all descendant nodes
                foreach (var rootNode in rootNodes)
                {
                    var descendants = await _taxonomyService.GetChildNodesAsync(rootNode.Id, tenantId, true, cancellationToken);
                    nodes.AddRange(descendants);
                }
                
                // Create a dictionary of nodes by ID for quick lookup
                var nodesById = nodes.ToDictionary(n => n.Id);
                
                // Validate relations for each node
                foreach (var node in nodes)
                {
                    // Check outgoing relations
                    var outgoingRelations = await _taxonomyService.GetNodeRelationsAsync(
                        node.Id, tenantId, null, true, cancellationToken);
                    
                    foreach (var relation in outgoingRelations)
                    {
                        // Check if target node exists
                        if (!nodesById.ContainsKey(relation.TargetNodeId))
                        {
                            issues.Add(new TaxonomyValidationIssue
                            {
                                IssueType = ValidationIssueType.BrokenRelation,
                                Message = $"Relation {relation.Id} from node {node.Id} points to non-existent target node {relation.TargetNodeId}",
                                AffectedNodeId = node.Id,
                                AffectedRelationId = relation.Id,
                                Severity = ValidationIssueSeverity.Error
                            });
                        }
                    }
                    
                    // Check for bidirectional relation consistency
                    foreach (var relation in outgoingRelations.Where(r => r.IsBidirectional))
                    {
                        var incomingRelations = await _taxonomyService.GetNodeRelationsAsync(
                            relation.TargetNodeId, tenantId, relation.RelationType, true, cancellationToken);
                        
                        if (!incomingRelations.Any(r => r.TargetNodeId == node.Id))
                        {
                            issues.Add(new TaxonomyValidationIssue
                            {
                                IssueType = ValidationIssueType.InconsistentBidirectionalRelation,
                                Message = $"Bidirectional relation {relation.Id} from node {node.Id} to {relation.TargetNodeId} is missing its complement relation",
                                AffectedNodeId = node.Id,
                                AffectedRelationId = relation.Id,
                                Severity = ValidationIssueSeverity.Warning
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating taxonomy relations for tenant {TenantId}", tenantId);
                issues.Add(new TaxonomyValidationIssue
                {
                    IssueType = ValidationIssueType.SystemError,
                    Message = $"System error during validation: {ex.Message}",
                    Severity = ValidationIssueSeverity.Error
                });
            }
            
            return issues;
        }
        
        /// <summary>
        /// Validates inheritance rules for consistency
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of validation issues found, empty list if no issues</returns>
        public async Task<IList<TaxonomyValidationIssue>> ValidateInheritanceRulesAsync(
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            var issues = new List<TaxonomyValidationIssue>();
            
            try
            {
                // Get all nodes for the tenant to extract node types
                var rootNodes = await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken);
                var nodes = new List<TaxonomyNode>(rootNodes);
                
                // Get all descendant nodes
                foreach (var rootNode in rootNodes)
                {
                    var descendants = await _taxonomyService.GetChildNodesAsync(rootNode.Id, tenantId, true, cancellationToken);
                    nodes.AddRange(descendants);
                }
                
                // Extract all node types
                var nodeTypes = nodes.Select(n => n.NodeType.ToString()).Distinct().ToList();
                
                // Get all inheritance rules for all node types
                var allRules = new List<TaxonomyInheritanceRule>();
                foreach (var nodeType in nodeTypes)
                {
                    var rules = await _taxonomyService.GetInheritanceRulesAsync(nodeType, tenantId, cancellationToken);
                    allRules.AddRange(rules);
                }
                
                // Filter out duplicate rules
                allRules = allRules.GroupBy(r => r.Id).Select(g => g.First()).ToList();
                
                // Validate each rule
                foreach (var rule in allRules)
                {
                    // Check source node type exists
                    if (!nodeTypes.Contains(rule.SourceNodeTypeId))
                    {
                        issues.Add(new TaxonomyValidationIssue
                        {
                            IssueType = ValidationIssueType.InvalidInheritanceRule,
                            Message = $"Inheritance rule {rule.Id} references non-existent source node type {rule.SourceNodeTypeId}",
                            AffectedRuleId = rule.Id,
                            Severity = ValidationIssueSeverity.Warning
                        });
                    }
                    
                    // Check target node type exists
                    if (!nodeTypes.Contains(rule.TargetNodeTypeId))
                    {
                        issues.Add(new TaxonomyValidationIssue
                        {
                            IssueType = ValidationIssueType.InvalidInheritanceRule,
                            Message = $"Inheritance rule {rule.Id} references non-existent target node type {rule.TargetNodeTypeId}",
                            AffectedRuleId = rule.Id,
                            Severity = ValidationIssueSeverity.Warning
                        });
                    }
                    
                    // Check for conflicting rules with same source/target but different priorities
                    var conflictingRules = allRules.Where(r => 
                        r.Id != rule.Id && 
                        r.RuleType == rule.RuleType &&
                        r.SourceNodeTypeId == rule.SourceNodeTypeId &&
                        r.TargetNodeTypeId == rule.TargetNodeTypeId &&
                        r.IsActive && rule.IsActive).ToList();
                    
                    if (conflictingRules.Any())
                    {
                        var conflictingIds = string.Join(", ", conflictingRules.Select(r => r.Id));
                        issues.Add(new TaxonomyValidationIssue
                        {
                            IssueType = ValidationIssueType.ConflictingInheritanceRules,
                            Message = $"Inheritance rule {rule.Id} conflicts with rules: {conflictingIds}",
                            AffectedRuleId = rule.Id,
                            Severity = ValidationIssueSeverity.Warning
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating inheritance rules for tenant {TenantId}", tenantId);
                issues.Add(new TaxonomyValidationIssue
                {
                    IssueType = ValidationIssueType.SystemError,
                    Message = $"System error during validation: {ex.Message}",
                    Severity = ValidationIssueSeverity.Error
                });
            }
            
            return issues;
        }
        
        #region Private Methods
        
        private async Task ValidateNodeHierarchyAsync(
            TaxonomyNode node, 
            TaxonomyNode parentNode, 
            HashSet<string> visitedNodes, 
            List<TaxonomyValidationIssue> issues, 
            string tenantId, 
            CancellationToken cancellationToken)
        {
            // Check for cycles in the hierarchy
            if (visitedNodes.Contains(node.Id))
            {
                issues.Add(new TaxonomyValidationIssue
                {
                    IssueType = ValidationIssueType.CyclicHierarchy,
                    Message = $"Cyclic reference detected for node {node.Id}",
                    AffectedNodeId = node.Id,
                    Severity = ValidationIssueSeverity.Error
                });
                return;
            }
            
            // Validate node's parent reference
            if (parentNode != null && node.ParentId != parentNode.Id)
            {
                issues.Add(new TaxonomyValidationIssue
                {
                    IssueType = ValidationIssueType.InconsistentParentReference,
                    Message = $"Node {node.Id} has inconsistent parent reference: {node.ParentId} vs {parentNode.Id}",
                    AffectedNodeId = node.Id,
                    Severity = ValidationIssueSeverity.Error
                });
            }
            
            // Mark this node as visited
            visitedNodes.Add(node.Id);
            
            // Get and validate child nodes
            var childNodes = await _taxonomyService.GetChildNodesAsync(node.Id, tenantId, false, cancellationToken);
            foreach (var childNode in childNodes)
            {
                await ValidateNodeHierarchyAsync(childNode, node, visitedNodes, issues, tenantId, cancellationToken);
            }
            
            // Remove from visited when backtracking to allow nodes to be shared in DAG-like hierarchies
            visitedNodes.Remove(node.Id);
        }
        
        private async Task FindOrphanedNodesAsync(
            string tenantId, 
            List<TaxonomyValidationIssue> issues, 
            CancellationToken cancellationToken)
        {
            // Get all nodes for the tenant
            var rootNodes = await _taxonomyService.GetRootNodesAsync(tenantId, cancellationToken);
            var nodes = new List<TaxonomyNode>(rootNodes);
            
            // Get all descendant nodes
            foreach (var rootNode in rootNodes)
            {
                var descendants = await _taxonomyService.GetChildNodesAsync(rootNode.Id, tenantId, true, cancellationToken);
                nodes.AddRange(descendants);
            }
            
            // Create a set of all node IDs
            var nodeIds = nodes.Select(n => n.Id).ToHashSet();
            
            // Find nodes with parent IDs that don't exist in the node set
            var orphanedNodes = nodes.Where(n => 
                !string.IsNullOrEmpty(n.ParentId) && 
                !nodeIds.Contains(n.ParentId)).ToList();
            
            foreach (var orphanedNode in orphanedNodes)
            {
                issues.Add(new TaxonomyValidationIssue
                {
                    IssueType = ValidationIssueType.OrphanedNode,
                    Message = $"Node {orphanedNode.Id} has non-existent parent {orphanedNode.ParentId}",
                    AffectedNodeId = orphanedNode.Id,
                    Severity = ValidationIssueSeverity.Error
                });
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a validation issue in the taxonomy
    /// </summary>
    public class TaxonomyValidationIssue
    {
        /// <summary>
        /// The type of validation issue
        /// </summary>
        public ValidationIssueType IssueType { get; set; }
        
        /// <summary>
        /// A description of the validation issue
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// The severity of the issue
        /// </summary>
        public ValidationIssueSeverity Severity { get; set; }
        
        /// <summary>
        /// The ID of the affected node, if applicable
        /// </summary>
        public string AffectedNodeId { get; set; }
        
        /// <summary>
        /// The ID of the affected relation, if applicable
        /// </summary>
        public string AffectedRelationId { get; set; }
        
        /// <summary>
        /// The ID of the affected inheritance rule, if applicable
        /// </summary>
        public string AffectedRuleId { get; set; }
    }
    
    /// <summary>
    /// Types of validation issues that can be detected
    /// </summary>
    public enum ValidationIssueType
    {
        /// <summary>
        /// Indicates a cycle in the hierarchy
        /// </summary>
        CyclicHierarchy,
        
        /// <summary>
        /// Indicates a node with a non-existent parent
        /// </summary>
        OrphanedNode,
        
        /// <summary>
        /// Indicates a relation pointing to a non-existent node
        /// </summary>
        BrokenRelation,
        
        /// <summary>
        /// Indicates a bidirectional relation without its complementary relation
        /// </summary>
        InconsistentBidirectionalRelation,
        
        /// <summary>
        /// Indicates an inheritance rule referencing a non-existent node type
        /// </summary>
        InvalidInheritanceRule,
        
        /// <summary>
        /// Indicates conflicting inheritance rules
        /// </summary>
        ConflictingInheritanceRules,
        
        /// <summary>
        /// Indicates a node with an inconsistent parent reference
        /// </summary>
        InconsistentParentReference,
        
        /// <summary>
        /// Indicates a system error during validation
        /// </summary>
        SystemError
    }
    
    /// <summary>
    /// Severity levels for validation issues
    /// </summary>
    public enum ValidationIssueSeverity
    {
        /// <summary>
        /// An informational notice, no action required
        /// </summary>
        Info,
        
        /// <summary>
        /// A warning that should be addressed but doesn't prevent operation
        /// </summary>
        Warning,
        
        /// <summary>
        /// A critical error that should be fixed
        /// </summary>
        Error
    }
} 