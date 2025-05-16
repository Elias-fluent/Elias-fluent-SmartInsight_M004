using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces
{
    /// <summary>
    /// Interface for taxonomy management operations
    /// </summary>
    public interface ITaxonomyService
    {
        /// <summary>
        /// Creates a new taxonomy node
        /// </summary>
        /// <param name="node">The node to create</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created node</returns>
        Task<TaxonomyNode> CreateNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing taxonomy node
        /// </summary>
        /// <param name="node">The node with updated values</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated node</returns>
        Task<TaxonomyNode> UpdateNodeAsync(
            TaxonomyNode node, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a taxonomy node
        /// </summary>
        /// <param name="nodeId">The ID of the node to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="recursive">Whether to delete child nodes recursively</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteNodeAsync(
            string nodeId, 
            string tenantId, 
            bool recursive = false, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a taxonomy node by ID
        /// </summary>
        /// <param name="nodeId">The ID of the node to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The taxonomy node if found, null otherwise</returns>
        Task<TaxonomyNode> GetNodeAsync(
            string nodeId, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all child nodes of a parent node
        /// </summary>
        /// <param name="parentId">The ID of the parent node</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="recursive">Whether to get all descendants recursively</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The child nodes</returns>
        Task<IEnumerable<TaxonomyNode>> GetChildNodesAsync(
            string parentId, 
            string tenantId, 
            bool recursive = false, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all root nodes (nodes without a parent)
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The root nodes</returns>
        Task<IEnumerable<TaxonomyNode>> GetRootNodesAsync(
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a relation between two taxonomy nodes
        /// </summary>
        /// <param name="relation">The relation to create</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created relation</returns>
        Task<TaxonomyRelation> CreateRelationAsync(
            TaxonomyRelation relation, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a relation between taxonomy nodes
        /// </summary>
        /// <param name="relationId">The ID of the relation to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteRelationAsync(
            string relationId, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all relations for a node
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="relationType">Optional relation type filter</param>
        /// <param name="isOutgoing">If true, get relations where the node is the source, otherwise get relations where the node is the target</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relations</returns>
        Task<IEnumerable<TaxonomyRelation>> GetNodeRelationsAsync(
            string nodeId, 
            string tenantId, 
            TaxonomyRelationType? relationType = null, 
            bool isOutgoing = true, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates an inheritance rule
        /// </summary>
        /// <param name="rule">The rule to create</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created rule</returns>
        Task<TaxonomyInheritanceRule> CreateInheritanceRuleAsync(
            TaxonomyInheritanceRule rule, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an inheritance rule
        /// </summary>
        /// <param name="rule">The rule with updated values</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated rule</returns>
        Task<TaxonomyInheritanceRule> UpdateInheritanceRuleAsync(
            TaxonomyInheritanceRule rule, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an inheritance rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteInheritanceRuleAsync(
            string ruleId, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all inheritance rules for a node type
        /// </summary>
        /// <param name="nodeTypeId">The ID of the node type</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The inheritance rules</returns>
        Task<IEnumerable<TaxonomyInheritanceRule>> GetInheritanceRulesAsync(
            string nodeTypeId, 
            string tenantId, 
            CancellationToken cancellationToken = default);
    }
} 