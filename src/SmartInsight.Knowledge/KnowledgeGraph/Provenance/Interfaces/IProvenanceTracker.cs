using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance.Interfaces
{
    /// <summary>
    /// Interface for tracking and querying provenance information in the knowledge graph
    /// </summary>
    public interface IProvenanceTracker
    {
        /// <summary>
        /// Records provenance metadata for a knowledge graph element
        /// </summary>
        /// <param name="metadata">The provenance metadata to record</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RecordProvenanceAsync(
            ProvenanceMetadata metadata,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Records provenance metadata for a triple
        /// </summary>
        /// <param name="triple">The triple to record provenance for</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The recorded provenance metadata</returns>
        Task<ProvenanceMetadata> RecordTripleProvenanceAsync(
            Triple triple,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Records provenance metadata for an entity
        /// </summary>
        /// <param name="entity">The entity to record provenance for</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The recorded provenance metadata</returns>
        Task<ProvenanceMetadata> RecordEntityProvenanceAsync(
            Entity entity,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Records provenance metadata for a relation
        /// </summary>
        /// <param name="relation">The relation to record provenance for</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The recorded provenance metadata</returns>
        Task<ProvenanceMetadata> RecordRelationProvenanceAsync(
            Relation relation,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets provenance metadata for a specific element
        /// </summary>
        /// <param name="elementId">ID of the element</param>
        /// <param name="elementType">Type of the element</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The provenance metadata</returns>
        Task<ProvenanceMetadata> GetProvenanceAsync(
            string elementId,
            ProvenanceElementType elementType,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Queries provenance metadata records based on various filters
        /// </summary>
        /// <param name="query">The provenance query parameters</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query results</returns>
        Task<ProvenanceQueryResult> QueryProvenanceAsync(
            ProvenanceQuery query,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Updates existing provenance metadata
        /// </summary>
        /// <param name="metadata">The updated provenance metadata</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateProvenanceAsync(
            ProvenanceMetadata metadata,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Deletes provenance metadata for a specific element
        /// </summary>
        /// <param name="elementId">ID of the element</param>
        /// <param name="elementType">Type of the element</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteProvenanceAsync(
            string elementId,
            ProvenanceElementType elementType,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the lineage (chain of dependencies) for a specific element
        /// </summary>
        /// <param name="elementId">ID of the element</param>
        /// <param name="elementType">Type of the element</param>
        /// <param name="maxDepth">Maximum depth to traverse dependencies</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of provenance metadata representing the lineage</returns>
        Task<List<ProvenanceMetadata>> GetProvenanceLineageAsync(
            string elementId,
            ProvenanceElementType elementType,
            int maxDepth,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets all elements derived from a specific source
        /// </summary>
        /// <param name="sourceId">ID of the source</param>
        /// <param name="sourceType">Type of the source</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of provenance metadata for elements derived from the source</returns>
        Task<List<ProvenanceMetadata>> GetElementsFromSourceAsync(
            string sourceId,
            ProvenanceSourceType sourceType,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Verifies a knowledge graph element
        /// </summary>
        /// <param name="elementId">ID of the element</param>
        /// <param name="elementType">Type of the element</param>
        /// <param name="verifiedBy">ID of the user verifying the element</param>
        /// <param name="justification">Optional justification for verification</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated provenance metadata</returns>
        Task<ProvenanceMetadata> VerifyElementAsync(
            string elementId,
            ProvenanceElementType elementType,
            string verifiedBy,
            string justification,
            string tenantId,
            CancellationToken cancellationToken = default);
    }
} 