using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces
{
    /// <summary>
    /// Interface for relation extractors that identify relationships between entities
    /// </summary>
    public interface IRelationExtractor
    {
        /// <summary>
        /// Extracts relations between entities from text content
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">Entities extracted from the content</param>
        /// <param name="sourceDocumentId">The source document ID</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted relations between entities</returns>
        Task<IEnumerable<Relation>> ExtractRelationsAsync(
            string content,
            IEnumerable<Entity> entities,
            string sourceDocumentId,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the relation types this extractor can identify
        /// </summary>
        /// <returns>The supported relation types</returns>
        IEnumerable<RelationType> GetSupportedRelationTypes();
        
        /// <summary>
        /// Gets extraction patterns used by this extractor
        /// </summary>
        /// <returns>The relation extraction patterns</returns>
        IEnumerable<RelationExtractionPattern> GetExtractionPatterns();
        
        /// <summary>
        /// Validates a relation between entities, checking if it's valid for the given entity types
        /// </summary>
        /// <param name="sourceEntity">The source entity</param>
        /// <param name="targetEntity">The target entity</param>
        /// <param name="relationType">The relation type</param>
        /// <returns>True if the relation is valid; otherwise, false</returns>
        bool ValidateRelation(Entity sourceEntity, Entity targetEntity, RelationType relationType);
    }
} 