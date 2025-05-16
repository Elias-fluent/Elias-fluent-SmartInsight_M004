using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Extractors
{
    /// <summary>
    /// Base class for relation extractors providing common functionality
    /// </summary>
    public abstract class BaseRelationExtractor : IRelationExtractor
    {
        private readonly ILogger _logger;
        private readonly Dictionary<(EntityType, EntityType, RelationType), bool> _validationCache = 
            new Dictionary<(EntityType, EntityType, RelationType), bool>();
        
        /// <summary>
        /// Initializes a new instance of the BaseRelationExtractor class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        protected BaseRelationExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Extracts relations between entities from text content
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">Entities extracted from the content</param>
        /// <param name="sourceDocumentId">The source document ID</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted relations between entities</returns>
        public abstract Task<IEnumerable<Relation>> ExtractRelationsAsync(
            string content,
            IEnumerable<Entity> entities,
            string sourceDocumentId,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the relation types this extractor can identify
        /// </summary>
        /// <returns>The supported relation types</returns>
        public abstract IEnumerable<RelationType> GetSupportedRelationTypes();
        
        /// <summary>
        /// Gets extraction patterns used by this extractor
        /// </summary>
        /// <returns>The relation extraction patterns</returns>
        public abstract IEnumerable<RelationExtractionPattern> GetExtractionPatterns();
        
        /// <summary>
        /// Validates a relation between entities, checking if it's valid for the given entity types
        /// </summary>
        /// <param name="sourceEntity">The source entity</param>
        /// <param name="targetEntity">The target entity</param>
        /// <param name="relationType">The relation type</param>
        /// <returns>True if the relation is valid; otherwise, false</returns>
        public virtual bool ValidateRelation(Entity sourceEntity, Entity targetEntity, RelationType relationType)
        {
            if (sourceEntity == null || targetEntity == null)
                return false;
                
            // Check cache for previous validation result
            var cacheKey = (sourceEntity.Type, targetEntity.Type, relationType);
            if (_validationCache.TryGetValue(cacheKey, out bool result))
                return result;
                
            // Get valid relation mappings
            var validMappings = GetValidEntityTypesForRelation(relationType);
            
            // Check if this entity type combination is valid for the relation
            result = validMappings.Any(m => 
                m.SourceEntityType == sourceEntity.Type && 
                m.TargetEntityType == targetEntity.Type);
                
            // Cache the result
            _validationCache[cacheKey] = result;
            
            return result;
        }
        
        /// <summary>
        /// Creates a relation between two entities
        /// </summary>
        /// <param name="sourceEntity">The source entity</param>
        /// <param name="targetEntity">The target entity</param>
        /// <param name="relationType">The relation type</param>
        /// <param name="sourceContext">The text context where the relation was found</param>
        /// <param name="confidenceScore">The confidence score for the relation</param>
        /// <param name="sourceDocumentId">The source document ID</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="extractionMethod">The method used to extract this relation</param>
        /// <returns>The created relation</returns>
        protected virtual Relation CreateRelation(
            Entity sourceEntity,
            Entity targetEntity,
            RelationType relationType,
            string sourceContext,
            double confidenceScore,
            string sourceDocumentId,
            string tenantId,
            string extractionMethod)
        {
            return new Relation
            {
                SourceEntity = sourceEntity,
                SourceEntityId = sourceEntity.Id,
                TargetEntity = targetEntity,
                TargetEntityId = targetEntity.Id,
                RelationType = relationType,
                SourceContext = sourceContext,
                ConfidenceScore = confidenceScore,
                SourceDocumentId = sourceDocumentId,
                TenantId = tenantId,
                ExtractionMethod = extractionMethod
            };
        }
        
        /// <summary>
        /// Gets valid entity type combinations for a specific relation type
        /// </summary>
        /// <param name="relationType">The relation type</param>
        /// <returns>Valid entity type combinations</returns>
        protected virtual IEnumerable<(EntityType SourceEntityType, EntityType TargetEntityType)> GetValidEntityTypesForRelation(
            RelationType relationType)
        {
            // Default implementation provides common relation type constraints
            // Override in derived classes for more specific constraints
            switch (relationType)
            {
                case RelationType.WorksFor:
                    return new[] { (EntityType.Person, EntityType.Organization) };
                    
                case RelationType.LocatedIn:
                    return new[] 
                    { 
                        (EntityType.Person, EntityType.Location),
                        (EntityType.Organization, EntityType.Location)
                    };
                    
                case RelationType.HeadquarteredIn:
                    return new[] { (EntityType.Organization, EntityType.Location) };
                    
                case RelationType.HasTitle:
                    return new[] { (EntityType.Person, EntityType.JobTitle) };
                    
                case RelationType.HasSkill:
                    return new[] { (EntityType.Person, EntityType.Skill) };
                    
                case RelationType.Created:
                    return new[] 
                    { 
                        (EntityType.Person, EntityType.Product),
                        (EntityType.Organization, EntityType.Product),
                        (EntityType.Person, EntityType.Document),
                        (EntityType.Organization, EntityType.Document)
                    };
                    
                case RelationType.Owns:
                    return new[] 
                    { 
                        (EntityType.Person, EntityType.Organization),
                        (EntityType.Person, EntityType.Product),
                        (EntityType.Organization, EntityType.Organization),
                        (EntityType.Organization, EntityType.Product)
                    };
                    
                case RelationType.SubsidiaryOf:
                    return new[] { (EntityType.Organization, EntityType.Organization) };
                    
                case RelationType.AuthorOf:
                    return new[] { (EntityType.Person, EntityType.Document) };
                    
                case RelationType.Leads:
                    return new[] { (EntityType.Person, EntityType.Project) };
                    
                case RelationType.ParticipatesIn:
                    return new[] 
                    { 
                        (EntityType.Person, EntityType.Project),
                        (EntityType.Organization, EntityType.Project)
                    };
                    
                case RelationType.ColumnOf:
                    return new[] { (EntityType.DatabaseColumn, EntityType.DatabaseTable) };
                    
                case RelationType.TableOf:
                    return new[] { (EntityType.DatabaseTable, EntityType.DatabaseSchema) };
                    
                // For generic relations, allow any combination
                case RelationType.AssociatedWith:
                case RelationType.SimilarTo:
                case RelationType.References:
                case RelationType.DomainSpecific:
                case RelationType.Other:
                    return Enum.GetValues(typeof(EntityType))
                        .Cast<EntityType>()
                        .SelectMany(sourceType => 
                            Enum.GetValues(typeof(EntityType))
                                .Cast<EntityType>()
                                .Select(targetType => (sourceType, targetType)));
                    
                default:
                    return Enumerable.Empty<(EntityType, EntityType)>();
            }
        }
        
        /// <summary>
        /// Calculates the proximity of two entities in a text
        /// </summary>
        /// <param name="entity1">First entity</param>
        /// <param name="entity2">Second entity</param>
        /// <param name="text">The text content</param>
        /// <returns>The token distance between entities</returns>
        protected int CalculateEntityProximity(Entity entity1, Entity entity2, string text)
        {
            if (entity1 == null || entity2 == null || string.IsNullOrEmpty(text) || 
                !entity1.StartPosition.HasValue || !entity2.StartPosition.HasValue)
            {
                return int.MaxValue;
            }
            
            // Get character positions
            int start1 = entity1.StartPosition.Value;
            int end1 = entity1.EndPosition ?? (start1 + entity1.Name.Length);
            
            int start2 = entity2.StartPosition.Value;
            int end2 = entity2.EndPosition ?? (start2 + entity2.Name.Length);
            
            // Calculate the text between the entities
            int betweenStart = Math.Min(end1, end2);
            int betweenEnd = Math.Max(start1, start2);
            
            if (betweenEnd <= betweenStart)
            {
                // Entities overlap
                return 0;
            }
            
            // Extract the text between the entities
            string textBetween = text.Substring(betweenStart, betweenEnd - betweenStart);
            
            // Count tokens (simple approximation by counting words)
            return textBetween.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        
        /// <summary>
        /// Logs information message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Message format arguments</param>
        protected void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }
        
        /// <summary>
        /// Logs warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Message format arguments</param>
        protected void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }
        
        /// <summary>
        /// Logs error message
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">The message to log</param>
        /// <param name="args">Message format arguments</param>
        protected void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }
    }
} 