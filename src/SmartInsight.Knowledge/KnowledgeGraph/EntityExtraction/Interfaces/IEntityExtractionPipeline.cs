using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces
{
    /// <summary>
    /// Interface for a pipeline that orchestrates multiple entity extractors
    /// </summary>
    public interface IEntityExtractionPipeline
    {
        /// <summary>
        /// Process text content through the entity extraction pipeline
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use. If null, all registered extractors are used.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The processed entities</returns>
        Task<IEnumerable<Entity>> ProcessAsync(
            string content,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Process structured data through the entity extraction pipeline
        /// </summary>
        /// <param name="data">Dictionary containing structured data field-value pairs</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use. If null, all registered extractors are used.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The processed entities</returns>
        Task<IEnumerable<Entity>> ProcessStructuredDataAsync(
            IDictionary<string, object> data,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets all registered entity extractors in the pipeline
        /// </summary>
        /// <returns>Collection of registered entity extractors</returns>
        IEnumerable<IEntityExtractor> GetRegisteredExtractors();
        
        /// <summary>
        /// Registers an entity extractor in the pipeline
        /// </summary>
        /// <param name="extractor">The entity extractor to register</param>
        void RegisterExtractor(IEntityExtractor extractor);
    }
} 