using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces
{
    /// <summary>
    /// Interface for entity disambiguation services that resolve and link related entities
    /// </summary>
    public interface IEntityDisambiguator
    {
        /// <summary>
        /// Disambiguates a collection of entities, identifying and linking related entities
        /// </summary>
        /// <param name="entities">The entities to disambiguate</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The disambiguated entities with appropriate linking</returns>
        Task<IEnumerable<Entity>> DisambiguateEntitiesAsync(
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the entity types this disambiguator supports
        /// </summary>
        /// <returns>The entity types supported by this disambiguator</returns>
        IEnumerable<EntityType> GetSupportedEntityTypes();
        
        /// <summary>
        /// Finds entities that might be related to the specified entity
        /// </summary>
        /// <param name="entity">The entity to find related entities for</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of potentially related entities</returns>
        Task<IEnumerable<Entity>> FindRelatedEntitiesAsync(
            Entity entity,
            string tenantId,
            CancellationToken cancellationToken = default);
    }
} 