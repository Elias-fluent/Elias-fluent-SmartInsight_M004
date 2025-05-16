using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces
{
    /// <summary>
    /// Defines the contract for entity extraction components
    /// </summary>
    public interface IEntityExtractor
    {
        /// <summary>
        /// Extracts entities from the provided text content
        /// </summary>
        /// <param name="content">The text content to extract entities from</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of extracted entities</returns>
        Task<IEnumerable<Entity>> ExtractEntitiesAsync(
            string content, 
            string sourceId,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Extracts entities from the provided structured data
        /// </summary>
        /// <param name="data">Dictionary containing structured data field-value pairs</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of extracted entities</returns>
        Task<IEnumerable<Entity>> ExtractEntitiesFromStructuredDataAsync(
            IDictionary<string, object> data,
            string sourceId,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the entity types supported by this extractor
        /// </summary>
        /// <returns>A collection of entity types this extractor can identify</returns>
        IEnumerable<EntityType> GetSupportedEntityTypes();
    }
} 