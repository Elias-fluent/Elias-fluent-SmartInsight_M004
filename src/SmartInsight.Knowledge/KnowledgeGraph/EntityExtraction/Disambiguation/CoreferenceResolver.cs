using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation
{
    /// <summary>
    /// Service for resolving coreferences in text
    /// </summary>
    public class CoreferenceResolver
    {
        private readonly ILogger<CoreferenceResolver> _logger;
        
        /// <summary>
        /// Initializes a new instance of the CoreferenceResolver class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public CoreferenceResolver(ILogger<CoreferenceResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Resolves coreferences in text by identifying mentions that refer to the same entity
        /// </summary>
        /// <param name="text">The text content to analyze</param>
        /// <param name="entities">Entities already extracted from the text</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entities with coreference information</returns>
        public Task<IEnumerable<Entity>> ResolveCoreferencesAsync(
            string text,
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));
                
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            _logger.LogInformation("Resolving coreferences in text for tenant {TenantId}", tenantId);
            
            var entityList = entities.ToList();
            if (entityList.Count == 0)
            {
                _logger.LogInformation("No entities provided for coreference resolution");
                return Task.FromResult(Enumerable.Empty<Entity>());
            }
            
            try
            {
                var personEntities = entityList
                    .Where(e => e.Type == EntityType.Person && 
                           e.StartPosition.HasValue && 
                           e.EndPosition.HasValue)
                    .ToList();
                    
                var organizationEntities = entityList
                    .Where(e => e.Type == EntityType.Organization && 
                           e.StartPosition.HasValue && 
                           e.EndPosition.HasValue)
                    .ToList();
                    
                var resolvedEntities = new List<Entity>(entityList);
                
                // Resolve person pronouns
                if (personEntities.Any())
                {
                    var personPronouns = FindPersonPronouns(text);
                    foreach (var pronoun in personPronouns)
                    {
                        var bestMatch = FindBestEntityForPronoun(pronoun, personEntities, text);
                        if (bestMatch != null)
                        {
                            var pronounEntity = CreatePronounEntity(pronoun, bestMatch, text, tenantId);
                            if (pronounEntity != null)
                            {
                                // Link to the original entity
                                pronounEntity.DisambiguationId = bestMatch.DisambiguationId ?? 
                                    $"dis-{Guid.NewGuid():N}";
                                    
                                if (string.IsNullOrEmpty(bestMatch.DisambiguationId))
                                    bestMatch.DisambiguationId = pronounEntity.DisambiguationId;
                                    
                                pronounEntity.Attributes["ReferenceType"] = "Pronoun";
                                pronounEntity.Attributes["ReferenceTarget"] = bestMatch.Id;
                                
                                resolvedEntities.Add(pronounEntity);
                            }
                        }
                    }
                }
                
                // Resolve organization references
                if (organizationEntities.Any())
                {
                    var orgReferences = FindOrganizationReferences(text, organizationEntities);
                    foreach (var orgRef in orgReferences)
                    {
                        var bestMatch = FindBestEntityForOrgReference(orgRef, organizationEntities, text);
                        if (bestMatch != null)
                        {
                            var refEntity = CreateReferenceEntity(orgRef, bestMatch, text, tenantId);
                            if (refEntity != null)
                            {
                                // Link to the original entity
                                refEntity.DisambiguationId = bestMatch.DisambiguationId ?? 
                                    $"dis-{Guid.NewGuid():N}";
                                    
                                if (string.IsNullOrEmpty(bestMatch.DisambiguationId))
                                    bestMatch.DisambiguationId = refEntity.DisambiguationId;
                                    
                                refEntity.Attributes["ReferenceType"] = "OrganizationReference";
                                refEntity.Attributes["ReferenceTarget"] = bestMatch.Id;
                                
                                resolvedEntities.Add(refEntity);
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Completed coreference resolution. Found {Count} references", 
                    resolvedEntities.Count - entityList.Count);
                    
                return Task.FromResult(resolvedEntities.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving coreferences: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Finds person pronouns in text
        /// </summary>
        /// <param name="text">The text to analyze</param>
        /// <returns>List of pronoun positions and values</returns>
        private List<(int Position, string Value, PronounType Type)> FindPersonPronouns(string text)
        {
            var pronouns = new List<(int, string, PronounType)>();
            
            // Male pronouns
            var malePattern = new Regex(@"\b(he|him|his)\b", RegexOptions.IgnoreCase);
            foreach (Match match in malePattern.Matches(text))
            {
                pronouns.Add((match.Index, match.Value, PronounType.Male));
            }
            
            // Female pronouns
            var femalePattern = new Regex(@"\b(she|her|hers)\b", RegexOptions.IgnoreCase);
            foreach (Match match in femalePattern.Matches(text))
            {
                pronouns.Add((match.Index, match.Value, PronounType.Female));
            }
            
            // Gender-neutral pronouns
            var neutralPattern = new Regex(@"\b(they|them|their|theirs)\b", RegexOptions.IgnoreCase);
            foreach (Match match in neutralPattern.Matches(text))
            {
                pronouns.Add((match.Index, match.Value, PronounType.Neutral));
            }
            
            return pronouns;
        }
        
        /// <summary>
        /// Finds organization references like "the company", "the firm", etc.
        /// </summary>
        /// <param name="text">The text to analyze</param>
        /// <param name="organizations">Known organizations in the text</param>
        /// <returns>List of reference positions and values</returns>
        private List<(int Position, string Value)> FindOrganizationReferences(
            string text, 
            List<Entity> organizations)
        {
            var references = new List<(int, string)>();
            
            // Common references to organizations
            var refPattern = new Regex(@"\b(the company|the organization|the firm|the corporation|the business)\b", 
                RegexOptions.IgnoreCase);
                
            foreach (Match match in refPattern.Matches(text))
            {
                // Only consider references that appear after at least one organization is mentioned
                if (organizations.Any(org => 
                    org.StartPosition.HasValue && 
                    org.StartPosition.Value < match.Index))
                {
                    references.Add((match.Index, match.Value));
                }
            }
            
            return references;
        }
        
        /// <summary>
        /// Finds the best entity match for a pronoun
        /// </summary>
        /// <param name="pronoun">The pronoun information</param>
        /// <param name="entities">Person entities to check</param>
        /// <param name="text">The original text</param>
        /// <returns>The best matching entity</returns>
        private Entity FindBestEntityForPronoun(
            (int Position, string Value, PronounType Type) pronoun,
            List<Entity> entities,
            string text)
        {
            // Only consider entities that appear before the pronoun
            var candidates = entities
                .Where(e => e.StartPosition.HasValue && e.StartPosition.Value < pronoun.Position)
                .ToList();
                
            if (!candidates.Any())
                return null;
                
            // Start with the closest entity before the pronoun
            return candidates
                .OrderByDescending(e => e.StartPosition)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Finds the best entity match for an organization reference
        /// </summary>
        /// <param name="reference">The reference information</param>
        /// <param name="entities">Organization entities to check</param>
        /// <param name="text">The original text</param>
        /// <returns>The best matching entity</returns>
        private Entity FindBestEntityForOrgReference(
            (int Position, string Value) reference,
            List<Entity> entities,
            string text)
        {
            // Only consider entities that appear before the reference
            var candidates = entities
                .Where(e => e.StartPosition.HasValue && e.StartPosition.Value < reference.Position)
                .ToList();
                
            if (!candidates.Any())
                return null;
                
            // Start with the closest entity before the reference
            return candidates
                .OrderByDescending(e => e.StartPosition)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Creates an entity for a pronoun reference
        /// </summary>
        /// <param name="pronoun">The pronoun information</param>
        /// <param name="referent">The entity being referenced</param>
        /// <param name="text">The original text</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>A new entity representing the pronoun</returns>
        private Entity CreatePronounEntity(
            (int Position, string Value, PronounType Type) pronoun,
            Entity referent,
            string text,
            string tenantId)
        {
            try
            {
                // Get context around the pronoun
                int contextStart = Math.Max(0, pronoun.Position - 75);
                int contextLength = Math.Min(text.Length - contextStart, pronoun.Value.Length + 150);
                
                var entity = new Entity
                {
                    Name = pronoun.Value,
                    Type = EntityType.Person,
                    TenantId = tenantId,
                    SourceId = referent.SourceId,
                    ConfidenceScore = 0.7,
                    StartPosition = pronoun.Position,
                    EndPosition = pronoun.Position + pronoun.Value.Length,
                    OriginalContext = text.Substring(contextStart, contextLength)
                };
                
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pronoun entity: {ErrorMessage}", ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// Creates an entity for an organization reference
        /// </summary>
        /// <param name="reference">The reference information</param>
        /// <param name="referent">The entity being referenced</param>
        /// <param name="text">The original text</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>A new entity representing the reference</returns>
        private Entity CreateReferenceEntity(
            (int Position, string Value) reference,
            Entity referent,
            string text,
            string tenantId)
        {
            try
            {
                // Get context around the reference
                int contextStart = Math.Max(0, reference.Position - 75);
                int contextLength = Math.Min(text.Length - contextStart, reference.Value.Length + 150);
                
                var entity = new Entity
                {
                    Name = reference.Value,
                    Type = EntityType.Organization,
                    TenantId = tenantId,
                    SourceId = referent.SourceId,
                    ConfidenceScore = 0.7,
                    StartPosition = reference.Position,
                    EndPosition = reference.Position + reference.Value.Length,
                    OriginalContext = text.Substring(contextStart, contextLength)
                };
                
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reference entity: {ErrorMessage}", ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// Types of pronouns
        /// </summary>
        private enum PronounType
        {
            Male,
            Female,
            Neutral
        }
    }
} 