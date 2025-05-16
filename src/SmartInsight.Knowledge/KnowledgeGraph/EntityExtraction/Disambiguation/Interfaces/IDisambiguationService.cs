using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces
{
    /// <summary>
    /// Service for managing entity disambiguation across multiple disambiguators
    /// </summary>
    public interface IDisambiguationService
    {
        /// <summary>
        /// Processes a collection of entities through appropriate disambiguators
        /// </summary>
        /// <param name="entities">The entities to disambiguate</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The processed entities with disambiguation information</returns>
        Task<IEnumerable<Entity>> ProcessEntitiesAsync(
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Resolves coreferences within a text content and links them to entities
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">The entities already extracted from the content</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entities with coreference resolution information</returns>
        Task<IEnumerable<Entity>> ResolveCoreferencesAsync(
            string content,
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Resolves an entity against existing knowledge graph entities
        /// </summary>
        /// <param name="entity">The entity to resolve</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Potentially matching entities from the knowledge graph</returns>
        Task<IEnumerable<Entity>> ResolveAgainstKnowledgeGraphAsync(
            Entity entity,
            string tenantId,
            CancellationToken cancellationToken = default);
    }
} 