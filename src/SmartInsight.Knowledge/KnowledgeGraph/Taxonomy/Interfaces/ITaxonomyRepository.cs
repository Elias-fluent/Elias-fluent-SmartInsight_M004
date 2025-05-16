using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces
{
    /// <summary>
    /// Interface for taxonomy repository operations
    /// </summary>
    public interface ITaxonomyRepository
    {
        #region Nodes
        
        /// <summary>
        /// Creates a new taxonomy node
        /// </summary>
        /// <param name="node">The node to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created node</returns>
        Task<TaxonomyNode> CreateNodeAsync(TaxonomyNode node, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing taxonomy node
        /// </summary>
        /// <param name="node">The node with updated values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated node</returns>
        Task<TaxonomyNode> UpdateNodeAsync(TaxonomyNode node, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a taxonomy node
        /// </summary>
        /// <param name="nodeId">The ID of the node to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteNodeAsync(string nodeId, string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a taxonomy node by ID
        /// </summary>
        /// <param name="nodeId">The ID of the node to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The taxonomy node if found, null otherwise</returns>
        Task<TaxonomyNode> GetNodeAsync(string nodeId, string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all nodes for a tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All nodes for the tenant</returns>
        Task<IEnumerable<TaxonomyNode>> GetAllNodesAsync(string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets nodes matching a filter
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="nameFilter">Optional name filter</param>
        /// <param name="nodeType">Optional node type filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching nodes</returns>
        Task<IEnumerable<TaxonomyNode>> FindNodesAsync(
            string tenantId, 
            string nameFilter = null, 
            TaxonomyNodeType? nodeType = null, 
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Relations
        
        /// <summary>
        /// Creates a relation between taxonomy nodes
        /// </summary>
        /// <param name="relation">The relation to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created relation</returns>
        Task<TaxonomyRelation> CreateRelationAsync(TaxonomyRelation relation, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing relation
        /// </summary>
        /// <param name="relation">The relation with updated values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated relation</returns>
        Task<TaxonomyRelation> UpdateRelationAsync(TaxonomyRelation relation, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a relation
        /// </summary>
        /// <param name="relationId">The ID of the relation to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteRelationAsync(string relationId, string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a relation by ID
        /// </summary>
        /// <param name="relationId">The ID of the relation to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relation if found, null otherwise</returns>
        Task<TaxonomyRelation> GetRelationAsync(string relationId, string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets relations for a node (source or target)
        /// </summary>
        /// <param name="nodeId">The ID of the node</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="relationType">Optional relation type filter</param>
        /// <param name="isSource">If true, get relations where the node is the source, otherwise get relations where the node is the target</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relations</returns>
        Task<IEnumerable<TaxonomyRelation>> GetNodeRelationsAsync(
            string nodeId, 
            string tenantId, 
            TaxonomyRelationType? relationType = null, 
            bool isSource = true, 
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Inheritance Rules
        
        /// <summary>
        /// Creates an inheritance rule
        /// </summary>
        /// <param name="rule">The rule to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created rule</returns>
        Task<TaxonomyInheritanceRule> CreateInheritanceRuleAsync(TaxonomyInheritanceRule rule, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing inheritance rule
        /// </summary>
        /// <param name="rule">The rule with updated values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated rule</returns>
        Task<TaxonomyInheritanceRule> UpdateInheritanceRuleAsync(TaxonomyInheritanceRule rule, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an inheritance rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to delete</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteInheritanceRuleAsync(string ruleId, string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets an inheritance rule by ID
        /// </summary>
        /// <param name="ruleId">The ID of the rule to get</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The rule if found, null otherwise</returns>
        Task<TaxonomyInheritanceRule> GetInheritanceRuleAsync(string ruleId, string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all inheritance rules for a tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All inheritance rules for the tenant</returns>
        Task<IEnumerable<TaxonomyInheritanceRule>> GetAllInheritanceRulesAsync(string tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets inheritance rules for a node type
        /// </summary>
        /// <param name="nodeTypeId">The ID of the node type</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The inheritance rules</returns>
        Task<IEnumerable<TaxonomyInheritanceRule>> GetInheritanceRulesByNodeTypeAsync(
            string nodeTypeId, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        #endregion
    }
} 