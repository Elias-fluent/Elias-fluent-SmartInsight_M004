using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation
{
    /// <summary>
    /// Base class for entity disambiguators providing common functionality
    /// </summary>
    public abstract class BaseEntityDisambiguator : IEntityDisambiguator
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the BaseEntityDisambiguator class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        protected BaseEntityDisambiguator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Disambiguates a collection of entities, identifying and linking related entities
        /// </summary>
        /// <param name="entities">The entities to disambiguate</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The disambiguated entities with appropriate linking</returns>
        public abstract Task<IEnumerable<Entity>> DisambiguateEntitiesAsync(
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the entity types this disambiguator supports
        /// </summary>
        /// <returns>The entity types supported by this disambiguator</returns>
        public abstract IEnumerable<EntityType> GetSupportedEntityTypes();
        
        /// <summary>
        /// Finds entities that might be related to the specified entity
        /// </summary>
        /// <param name="entity">The entity to find related entities for</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of potentially related entities</returns>
        public abstract Task<IEnumerable<Entity>> FindRelatedEntitiesAsync(
            Entity entity,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Calculates similarity between two entities
        /// </summary>
        /// <param name="entity1">The first entity</param>
        /// <param name="entity2">The second entity</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        protected virtual double CalculateSimilarity(Entity entity1, Entity entity2)
        {
            if (entity1 == null || entity2 == null)
                return 0.0;
                
            if (entity1.Type != entity2.Type)
                return 0.0;
                
            // Base similarity on names using case-insensitive comparison
            if (string.Equals(entity1.Name, entity2.Name, StringComparison.OrdinalIgnoreCase))
                return 1.0;
                
            // For names that are similar but not identical, apply fuzzy matching
            double nameSimilarity = CalculateLevenshteinSimilarity(
                entity1.Name, 
                entity2.Name);
                
            return nameSimilarity;
        }
        
        /// <summary>
        /// Groups similar entities together
        /// </summary>
        /// <param name="entities">Entities to group</param>
        /// <param name="similarityThreshold">Threshold for considering entities similar (0.0 to 1.0)</param>
        /// <returns>Groups of similar entities</returns>
        protected IEnumerable<IEnumerable<Entity>> GroupSimilarEntities(
            IEnumerable<Entity> entities,
            double similarityThreshold = 0.8)
        {
            var entityList = entities.ToList();
            var grouped = new List<List<Entity>>();
            var processed = new HashSet<string>();
            
            foreach (var entity in entityList)
            {
                if (processed.Contains(entity.Id))
                    continue;
                    
                var group = new List<Entity> { entity };
                processed.Add(entity.Id);
                
                // Find similar entities
                foreach (var other in entityList)
                {
                    if (processed.Contains(other.Id) || entity.Id == other.Id)
                        continue;
                        
                    double similarity = CalculateSimilarity(entity, other);
                    if (similarity >= similarityThreshold)
                    {
                        group.Add(other);
                        processed.Add(other.Id);
                    }
                }
                
                grouped.Add(group);
            }
            
            return grouped;
        }
        
        /// <summary>
        /// Calculates string similarity based on Levenshtein distance
        /// </summary>
        /// <param name="s1">First string</param>
        /// <param name="s2">Second string</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        protected double CalculateLevenshteinSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                return 1.0;
                
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;
                
            s1 = s1.ToLowerInvariant();
            s2 = s2.ToLowerInvariant();
            
            int distance = LevenshteinDistance(s1, s2);
            int maxLength = Math.Max(s1.Length, s2.Length);
            
            return 1.0 - ((double)distance / maxLength);
        }
        
        /// <summary>
        /// Calculates Levenshtein distance between two strings
        /// </summary>
        /// <param name="s1">First string</param>
        /// <param name="s2">Second string</param>
        /// <returns>Levenshtein distance</returns>
        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];
            
            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
                
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;
                
            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    
                    d[i, j] = Math.Min(
                        Math.Min(
                            d[i - 1, j] + 1,      // Deletion
                            d[i, j - 1] + 1),     // Insertion
                        d[i - 1, j - 1] + cost);  // Substitution
                }
            }
            
            return d[s1.Length, s2.Length];
        }
        
        /// <summary>
        /// Creates a new ID for the disambiguated entity group
        /// </summary>
        /// <returns>A new disambiguation ID</returns>
        protected string GenerateDisambiguationId()
        {
            return $"dis-{Guid.NewGuid():N}";
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