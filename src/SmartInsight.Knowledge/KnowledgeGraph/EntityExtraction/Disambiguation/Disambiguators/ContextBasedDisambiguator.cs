using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Disambiguators
{
    /// <summary>
    /// Disambiguator that uses entity context to identify and link related entities
    /// </summary>
    public class ContextBasedDisambiguator : BaseEntityDisambiguator
    {
        private readonly ILogger<ContextBasedDisambiguator> _logger;
        private readonly double _contextSimilarityThreshold;
        
        /// <summary>
        /// Initializes a new instance of the ContextBasedDisambiguator class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="contextSimilarityThreshold">Threshold for context similarity (0.0 to 1.0)</param>
        public ContextBasedDisambiguator(
            ILogger<ContextBasedDisambiguator> logger,
            double contextSimilarityThreshold = 0.6) : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextSimilarityThreshold = Math.Clamp(contextSimilarityThreshold, 0.0, 1.0);
        }
        
        /// <summary>
        /// Disambiguates a collection of entities based on their context
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
                
            LogInformation("Disambiguating {Count} entities using context-based approach for tenant {TenantId}",
                entities.Count(), tenantId);
                
            var entityList = entities.ToList();
            if (entityList.Count == 0)
            {
                LogInformation("No entities to disambiguate");
                return Task.FromResult(Enumerable.Empty<Entity>());
            }
            
            try
            {
                // Filter entities with context information (required for this disambiguator)
                var entitiesWithContext = entityList
                    .Where(e => GetSupportedEntityTypes().Contains(e.Type) && !string.IsNullOrEmpty(e.OriginalContext))
                    .ToList();
                    
                LogInformation("Found {Count} entities with context information", entitiesWithContext.Count);
                
                // Group entities by type for more efficient processing
                var entitiesByType = entitiesWithContext
                    .GroupBy(e => e.Type);
                    
                var results = new List<Entity>();
                
                foreach (var typeGroup in entitiesByType)
                {
                    var entityType = typeGroup.Key;
                    var entitiesOfType = typeGroup.ToList();
                    
                    LogInformation("Processing {Count} entities of type {EntityType} with context", 
                        entitiesOfType.Count, entityType);
                    
                    // For each entity, find others with similar context
                    foreach (var entity in entitiesOfType)
                    {
                        // Skip if already processed in another group
                        if (!string.IsNullOrEmpty(entity.DisambiguationId))
                            continue;
                            
                        // Find entities with similar context
                        var similarEntities = FindEntitiesWithSimilarContext(entity, entitiesOfType);
                        
                        if (!similarEntities.Any())
                        {
                            // No similar entities found
                            results.Add(entity);
                            continue;
                        }
                        
                        // Generate a shared disambiguation ID for this group
                        string disambiguationId = GenerateDisambiguationId();
                        
                        // Include the original entity in the group
                        var entityGroup = new List<Entity> { entity };
                        entityGroup.AddRange(similarEntities);
                        
                        // Choose the most confident entity as the primary
                        var primaryEntity = entityGroup
                            .OrderByDescending(e => e.ConfidenceScore)
                            .First();
                            
                        // Update all entities with the disambiguation ID
                        foreach (var groupEntity in entityGroup)
                        {
                            groupEntity.DisambiguationId = disambiguationId;
                            
                            // Update attributes with disambiguation info
                            groupEntity.Attributes["IsPrimaryEntity"] = groupEntity.Id == primaryEntity.Id;
                            groupEntity.Attributes["EntityGroupSize"] = entityGroup.Count;
                            groupEntity.Attributes["DisambiguationMethod"] = "Context";
                            
                            results.Add(groupEntity);
                        }
                        
                        LogInformation("Disambiguated group of {Count} entities with ID {DisambiguationId} using context",
                            entityGroup.Count, disambiguationId);
                    }
                }
                
                // Add any entities without context or of unsupported types without processing
                var remainingEntities = entityList
                    .Where(e => !results.Any(r => r.Id == e.Id));
                    
                results.AddRange(remainingEntities);
                
                LogInformation("Completed context-based disambiguation. Processed {Count} entities", results.Count);
                return Task.FromResult(results.AsEnumerable());
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during context-based entity disambiguation: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Finds entities that might be related to the specified entity based on context similarity
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
            LogInformation("Finding related entities by context is not fully supported in the basic implementation");
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
                EntityType.TechnicalTerm,
                EntityType.JobTitle,
                EntityType.DatabaseTable,
                EntityType.DatabaseColumn,
                EntityType.Api
            };
        }
        
        /// <summary>
        /// Finds entities with similar context to the specified entity
        /// </summary>
        /// <param name="entity">The entity to compare with</param>
        /// <param name="candidates">The candidate entities to check</param>
        /// <returns>Entities with similar context</returns>
        private IEnumerable<Entity> FindEntitiesWithSimilarContext(Entity entity, List<Entity> candidates)
        {
            var similarEntities = new List<Entity>();
            
            // Skip if no context available
            if (string.IsNullOrEmpty(entity.OriginalContext))
                return similarEntities;
                
            // Extract key terms from the entity's context
            var keyTerms = ExtractKeyTerms(entity.OriginalContext);
            
            foreach (var candidate in candidates)
            {
                // Skip self or already processed entities
                if (candidate.Id == entity.Id || !string.IsNullOrEmpty(candidate.DisambiguationId))
                    continue;
                    
                // Skip if no context available for candidate
                if (string.IsNullOrEmpty(candidate.OriginalContext))
                    continue;
                    
                // Extract key terms from the candidate's context
                var candidateKeyTerms = ExtractKeyTerms(candidate.OriginalContext);
                
                // Calculate context similarity based on shared key terms
                double contextSimilarity = CalculateContextSimilarity(keyTerms, candidateKeyTerms);
                
                if (contextSimilarity >= _contextSimilarityThreshold)
                {
                    candidate.Attributes["ContextSimilarityScore"] = contextSimilarity;
                    similarEntities.Add(candidate);
                }
            }
            
            return similarEntities;
        }
        
        /// <summary>
        /// Extracts key terms from text for context comparison
        /// </summary>
        /// <param name="text">The text to extract terms from</param>
        /// <returns>Set of key terms</returns>
        private HashSet<string> ExtractKeyTerms(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new HashSet<string>();
                
            // Normalize text
            text = text.ToLowerInvariant();
            
            // Remove punctuation and split into words
            var words = Regex.Replace(text, @"[^\w\s]", " ")
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2) // Skip very short words
                .ToList();
                
            // Remove common stop words
            var stopWords = new HashSet<string>
            {
                "the", "and", "this", "that", "with", "for", "from", "was", "were", "will", 
                "have", "has", "had", "are", "our", "your", "their", "she", "him", "her", "they",
                "may", "can", "could", "would", "should", "been", "being", "when", "where", "why",
                "how", "what", "who", "which", "such", "some", "very", "just"
            };
            
            var keyTerms = words
                .Where(w => !stopWords.Contains(w))
                .ToHashSet();
                
            return keyTerms;
        }
        
        /// <summary>
        /// Calculates similarity between two sets of context terms
        /// </summary>
        /// <param name="terms1">First set of terms</param>
        /// <param name="terms2">Second set of terms</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        private double CalculateContextSimilarity(HashSet<string> terms1, HashSet<string> terms2)
        {
            if (terms1.Count == 0 || terms2.Count == 0)
                return 0.0;
                
            // Use Jaccard similarity: size of intersection divided by size of union
            int intersectionCount = terms1.Intersect(terms2).Count();
            int unionCount = terms1.Union(terms2).Count();
            
            return (double)intersectionCount / unionCount;
        }
    }
} 