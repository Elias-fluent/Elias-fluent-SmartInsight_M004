using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Disambiguators
{
    /// <summary>
    /// Disambiguator that uses entity names to identify and link related entities
    /// </summary>
    public class NameBasedDisambiguator : BaseEntityDisambiguator
    {
        private readonly ILogger<NameBasedDisambiguator> _logger;
        private readonly double _similarityThreshold;
        
        /// <summary>
        /// Initializes a new instance of the NameBasedDisambiguator class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="similarityThreshold">Threshold for name similarity (0.0 to 1.0)</param>
        public NameBasedDisambiguator(
            ILogger<NameBasedDisambiguator> logger,
            double similarityThreshold = 0.8) : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _similarityThreshold = Math.Clamp(similarityThreshold, 0.0, 1.0);
        }
        
        /// <summary>
        /// Disambiguates a collection of entities based on name similarity
        /// </summary>
        /// <param name="entities">The entities to disambiguate</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The disambiguated entities with appropriate linking</returns>
        public override Task<IEnumerable<Entity>> DisambiguateEntitiesAsync(
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            LogInformation("Disambiguating {Count} entities using name-based approach for tenant {TenantId}",
                entities.Count(), tenantId);
                
            var entityList = entities.ToList();
            if (entityList.Count == 0)
            {
                LogInformation("No entities to disambiguate");
                return Task.FromResult(Enumerable.Empty<Entity>());
            }
            
            try
            {
                // Group entities by type for more efficient processing
                var entitiesByType = entityList
                    .Where(e => GetSupportedEntityTypes().Contains(e.Type))
                    .GroupBy(e => e.Type);
                    
                var results = new List<Entity>();
                
                foreach (var typeGroup in entitiesByType)
                {
                    var entityType = typeGroup.Key;
                    var entitiesOfType = typeGroup.ToList();
                    
                    LogInformation("Processing {Count} entities of type {EntityType}", 
                        entitiesOfType.Count, entityType);
                    
                    // Group similar entities together
                    var entityGroups = GroupSimilarEntities(entitiesOfType, _similarityThreshold);
                    
                    // Process each group
                    foreach (var group in entityGroups)
                    {
                        var entityGroup = group.ToList();
                        
                        // Skip single-entity groups (no disambiguation needed)
                        if (entityGroup.Count <= 1)
                        {
                            results.AddRange(entityGroup);
                            continue;
                        }
                        
                        // Generate a shared disambiguation ID for this group
                        string disambiguationId = GenerateDisambiguationId();
                        
                        // Choose the most confident entity as the primary
                        var primaryEntity = entityGroup
                            .OrderByDescending(e => e.ConfidenceScore)
                            .First();
                            
                        // Update all entities with the disambiguation ID
                        foreach (var entity in entityGroup)
                        {
                            entity.DisambiguationId = disambiguationId;
                            
                            // Update attributes with disambiguation info
                            entity.Attributes["IsPrimaryEntity"] = entity.Id == primaryEntity.Id;
                            entity.Attributes["EntityGroupSize"] = entityGroup.Count;
                            
                            results.Add(entity);
                        }
                        
                        LogInformation("Disambiguated group of {Count} entities with ID {DisambiguationId}",
                            entityGroup.Count, disambiguationId);
                    }
                }
                
                // Add any entities of unsupported types without processing
                var unsupportedEntities = entityList
                    .Where(e => !GetSupportedEntityTypes().Contains(e.Type));
                    
                results.AddRange(unsupportedEntities);
                
                LogInformation("Completed disambiguation. Processed {Count} entities", results.Count);
                return Task.FromResult(results.AsEnumerable());
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during entity disambiguation: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Finds entities that might be related to the specified entity based on name similarity
        /// </summary>
        /// <param name="entity">The entity to find related entities for</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of potentially related entities</returns>
        public override Task<IEnumerable<Entity>> FindRelatedEntitiesAsync(
            Entity entity,
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            // This is a simple implementation that doesn't rely on external storage
            // In a real implementation, this would query a database of known entities
            
            LogInformation("Finding related entities is not supported in the basic implementation");
            return Task.FromResult(Enumerable.Empty<Entity>());
        }
        
        /// <summary>
        /// Gets the entity types this disambiguator supports
        /// </summary>
        /// <returns>The entity types supported by this disambiguator</returns>
        public override IEnumerable<EntityType> GetSupportedEntityTypes()
        {
            return new[]
            {
                EntityType.Person,
                EntityType.Organization,
                EntityType.Location,
                EntityType.Product,
                EntityType.Project,
                EntityType.TechnicalTerm,
                EntityType.JobTitle
            };
        }
        
        /// <summary>
        /// Calculates similarity between two entities with additional name-specific logic
        /// </summary>
        /// <param name="entity1">The first entity</param>
        /// <param name="entity2">The second entity</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        protected override double CalculateSimilarity(Entity entity1, Entity entity2)
        {
            double baseSimilarity = base.CalculateSimilarity(entity1, entity2);
            
            // Additional name-specific logic for certain entity types
            if (entity1.Type == EntityType.Person && entity2.Type == EntityType.Person)
            {
                // For people, check if last names match even if first names don't
                string[] parts1 = entity1.Name.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
                string[] parts2 = entity2.Name.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts1.Length > 1 && parts2.Length > 1)
                {
                    string lastName1 = parts1[parts1.Length - 1];
                    string lastName2 = parts2[parts2.Length - 1];
                    
                    if (string.Equals(lastName1, lastName2, StringComparison.OrdinalIgnoreCase))
                    {
                        // Boost similarity if last names match
                        baseSimilarity = Math.Max(baseSimilarity, 0.7);
                    }
                }
            }
            
            return baseSimilarity;
        }
    }
} 