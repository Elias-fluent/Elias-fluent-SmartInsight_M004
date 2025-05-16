using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces
{
    /// <summary>
    /// Interface for triple store operations
    /// </summary>
    public interface ITripleStore
    {
        /// <summary>
        /// Adds a triple to the store
        /// </summary>
        /// <param name="triple">The triple to add</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AddTripleAsync(
            Triple triple, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds multiple triples to the store in a batch
        /// </summary>
        /// <param name="triples">The triples to add</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of triples successfully added</returns>
        Task<int> AddTriplesAsync(
            IEnumerable<Triple> triples, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a triple from a relation and adds it to the store
        /// </summary>
        /// <param name="relation">The relation to convert to a triple</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional named graph URI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created triple if successful, null otherwise</returns>
        Task<Triple> AddRelationAsTripleAsync(
            Relation relation, 
            string tenantId, 
            string graphUri = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates triples from entity attributes and adds them to the store
        /// </summary>
        /// <param name="entity">The entity to extract attribute triples from</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional named graph URI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created triples if successful</returns>
        Task<IEnumerable<Triple>> AddEntityAttributesAsTripleAsync(
            Entity entity, 
            string tenantId, 
            string graphUri = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Removes a triple from the store
        /// </summary>
        /// <param name="tripleId">The ID of the triple to remove</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RemoveTripleAsync(
            string tripleId, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing triple in the store
        /// </summary>
        /// <param name="triple">The triple with updated values</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateTripleAsync(
            Triple triple, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Queries the triple store with various filter options
        /// </summary>
        /// <param name="query">The triple query parameters</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query results</returns>
        Task<TripleQueryResult> QueryAsync(
            TripleQuery query, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a SPARQL query on the triple store
        /// </summary>
        /// <param name="sparqlQuery">The SPARQL query string</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query results</returns>
        Task<object> ExecuteSparqlQueryAsync(
            string sparqlQuery, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a named graph in the triple store
        /// </summary>
        /// <param name="graphUri">The URI of the graph to create</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CreateGraphAsync(
            string graphUri, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Removes a named graph and all its triples from the store
        /// </summary>
        /// <param name="graphUri">The URI of the graph to remove</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RemoveGraphAsync(
            string graphUri, 
            string tenantId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets statistics about the triple store
        /// </summary>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of statistics</returns>
        Task<Dictionary<string, object>> GetStoreStatisticsAsync(
            string tenantId, 
            CancellationToken cancellationToken = default);
    }
} 