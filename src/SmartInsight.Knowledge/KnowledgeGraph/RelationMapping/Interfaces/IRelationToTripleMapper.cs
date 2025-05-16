using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces
{
    /// <summary>
    /// Interface for mapping relations to triples for storage in the triple store
    /// </summary>
    public interface IRelationToTripleMapper
    {
        /// <summary>
        /// Maps a relation to a triple and stores it in the triple store
        /// </summary>
        /// <param name="relation">The relation to map</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional graph URI for the triple</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> MapAndStoreAsync(
            Relation relation, 
            string tenantId, 
            string graphUri = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Maps multiple relations to triples and stores them in the triple store
        /// </summary>
        /// <param name="relations">The relations to map</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional graph URI for the triples</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of triples successfully stored</returns>
        Task<int> MapAndStoreBatchAsync(
            IEnumerable<Relation> relations, 
            string tenantId, 
            string graphUri = null, 
            CancellationToken cancellationToken = default);
    }
} 