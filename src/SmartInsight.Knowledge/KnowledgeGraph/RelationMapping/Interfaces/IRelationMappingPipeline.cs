using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces
{
    /// <summary>
    /// Interface for the relation mapping pipeline that coordinates relation extraction
    /// </summary>
    public interface IRelationMappingPipeline
    {
        /// <summary>
        /// Processes entities extracted from text content to identify relations between them
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">Entities extracted from the content</param>
        /// <param name="sourceDocumentId">The source document ID</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="extractorTypes">Optional specific extractor types to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted relations between entities</returns>
        Task<IEnumerable<Relation>> ProcessAsync(
            string content,
            IEnumerable<Entity> entities,
            string sourceDocumentId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets all registered relation extractors
        /// </summary>
        /// <returns>The registered relation extractors</returns>
        IEnumerable<IRelationExtractor> GetRegisteredExtractors();
        
        /// <summary>
        /// Gets a specific relation extractor by type name
        /// </summary>
        /// <param name="typeName">The relation extractor type name</param>
        /// <returns>The relation extractor instance, or null if not found</returns>
        IRelationExtractor GetExtractor(string typeName);
        
        /// <summary>
        /// Gets the relation types supported across all extractors
        /// </summary>
        /// <returns>The supported relation types</returns>
        IEnumerable<RelationType> GetSupportedRelationTypes();
        
        /// <summary>
        /// Validates all extracted relations
        /// </summary>
        /// <param name="relations">The relations to validate</param>
        /// <param name="minConfidenceThreshold">Minimum confidence threshold</param>
        /// <returns>The validated relations</returns>
        IEnumerable<Relation> ValidateRelations(
            IEnumerable<Relation> relations,
            double minConfidenceThreshold = 0.5);
    }
} 