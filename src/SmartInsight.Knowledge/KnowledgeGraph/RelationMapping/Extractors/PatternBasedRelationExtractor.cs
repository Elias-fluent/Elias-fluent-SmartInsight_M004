using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Extractors
{
    /// <summary>
    /// Relation extractor that uses regex patterns to identify relationships between entities
    /// </summary>
    public class PatternBasedRelationExtractor : BaseRelationExtractor
    {
        private readonly ILogger<PatternBasedRelationExtractor> _logger;
        private readonly List<RelationExtractionPattern> _patterns;
        private readonly int _maxTokenDistance;
        
        /// <summary>
        /// Initializes a new instance of the PatternBasedRelationExtractor class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="maxTokenDistance">Maximum token distance between related entities</param>
        public PatternBasedRelationExtractor(
            ILogger<PatternBasedRelationExtractor> logger,
            int maxTokenDistance = 10) : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxTokenDistance = maxTokenDistance;
            _patterns = InitializePatterns();
        }
        
        /// <summary>
        /// Extracts relations between entities from text content based on patterns
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">Entities extracted from the content</param>
        /// <param name="sourceDocumentId">The source document ID</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted relations between entities</returns>
        public override async Task<IEnumerable<Relation>> ExtractRelationsAsync(
            string content,
            IEnumerable<Entity> entities,
            string sourceDocumentId,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));
                
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            LogInformation(
                "Extracting relations using pattern-based approach from content for tenant {TenantId}",
                tenantId);
                
            var entityList = entities.ToList();
            if (entityList.Count < 2)
            {
                LogInformation("Not enough entities to extract relations (minimum 2 required)");
                return Enumerable.Empty<Relation>();
            }
            
            try
            {
                var relations = new List<Relation>();
                
                // Find potential entity pairs
                var entityPairs = GetPotentialEntityPairs(entityList, content);
                LogInformation("Found {Count} potential entity pairs to check for relations", entityPairs.Count);
                
                foreach (var (sourceEntity, targetEntity) in entityPairs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    // Skip if entities are the same
                    if (sourceEntity.Id == targetEntity.Id)
                        continue;
                        
                    // Get the text segment containing both entities
                    string textSegment = ExtractTextSegment(content, sourceEntity, targetEntity);
                    
                    // Match against patterns
                    foreach (var pattern in _patterns)
                    {
                        if (!ValidateRelation(sourceEntity, targetEntity, pattern.RelationType))
                            continue;
                            
                        // Check for pattern match
                        var match = pattern.CompiledRegex?.Match(textSegment);
                        if (match != null && match.Success)
                        {
                            double confidence = CalculateConfidenceScore(
                                pattern.BaseConfidenceScore,
                                sourceEntity,
                                targetEntity,
                                match.Value);
                                
                            // Create relation
                            var relation = CreateRelation(
                                sourceEntity,
                                targetEntity,
                                pattern.RelationType,
                                match.Value,
                                confidence,
                                sourceDocumentId,
                                tenantId,
                                "PatternBased");
                                
                            relations.Add(relation);
                            
                            LogInformation(
                                "Extracted {RelationType} relation between '{SourceEntity}' and '{TargetEntity}' with confidence {Confidence}",
                                pattern.RelationType,
                                sourceEntity.Name,
                                targetEntity.Name,
                                confidence);
                                
                            // If we found a relation, no need to check other patterns for this pair
                            break;
                        }
                    }
                    
                    // Check for trigger phrases if no pattern match was found
                    if (!relations.Any(r => 
                        r.SourceEntityId == sourceEntity.Id && 
                        r.TargetEntityId == targetEntity.Id))
                    {
                        var relation = CheckTriggerPhrases(
                            sourceEntity,
                            targetEntity,
                            textSegment,
                            sourceDocumentId,
                            tenantId);
                            
                        if (relation != null)
                        {
                            relations.Add(relation);
                            
                            LogInformation(
                                "Extracted {RelationType} relation between '{SourceEntity}' and '{TargetEntity}' with confidence {Confidence} using trigger phrases",
                                relation.RelationType,
                                sourceEntity.Name,
                                targetEntity.Name,
                                relation.ConfidenceScore);
                        }
                    }
                }
                
                LogInformation(
                    "Completed pattern-based relation extraction. Found {Count} relations", 
                    relations.Count);
                    
                return relations;
            }
            catch (Exception ex)
            {
                LogError(
                    ex,
                    "Error extracting relations using pattern-based approach: {ErrorMessage}",
                    ex.Message);
                    
                throw;
            }
            
            // Return as completed task for synchronous implementations
            return await Task.FromResult(Enumerable.Empty<Relation>());
        }
        
        /// <summary>
        /// Gets the relation types this extractor can identify
        /// </summary>
        /// <returns>The supported relation types</returns>
        public override IEnumerable<RelationType> GetSupportedRelationTypes()
        {
            return _patterns.Select(p => p.RelationType).Distinct();
        }
        
        /// <summary>
        /// Gets extraction patterns used by this extractor
        /// </summary>
        /// <returns>The relation extraction patterns</returns>
        public override IEnumerable<RelationExtractionPattern> GetExtractionPatterns()
        {
            return _patterns;
        }
        
        /// <summary>
        /// Gets potential entity pairs that might have relations
        /// </summary>
        /// <param name="entities">The entities to check</param>
        /// <param name="content">The text content</param>
        /// <returns>Potential entity pairs</returns>
        private List<(Entity Source, Entity Target)> GetPotentialEntityPairs(
            List<Entity> entities, 
            string content)
        {
            var pairs = new List<(Entity, Entity)>();
            
            // Filter entities with position information
            var entitiesWithPosition = entities
                .Where(e => e.StartPosition.HasValue && e.EndPosition.HasValue)
                .ToList();
                
            // Get all possible entity pairs
            for (int i = 0; i < entitiesWithPosition.Count; i++)
            {
                for (int j = 0; j < entitiesWithPosition.Count; j++)
                {
                    if (i == j) continue;
                    
                    var sourceEntity = entitiesWithPosition[i];
                    var targetEntity = entitiesWithPosition[j];
                    
                    // Calculate token distance between entities
                    int distance = CalculateEntityProximity(sourceEntity, targetEntity, content);
                    
                    // Only add pairs within the maximum token distance
                    if (distance <= _maxTokenDistance)
                    {
                        pairs.Add((sourceEntity, targetEntity));
                    }
                }
            }
            
            return pairs;
        }
        
        /// <summary>
        /// Extracts the text segment containing both entities
        /// </summary>
        /// <param name="content">The text content</param>
        /// <param name="entity1">First entity</param>
        /// <param name="entity2">Second entity</param>
        /// <returns>The text segment containing both entities</returns>
        private string ExtractTextSegment(string content, Entity entity1, Entity entity2)
        {
            if (string.IsNullOrEmpty(content) || 
                !entity1.StartPosition.HasValue || !entity1.EndPosition.HasValue ||
                !entity2.StartPosition.HasValue || !entity2.EndPosition.HasValue)
            {
                return string.Empty;
            }
            
            int start = Math.Min(entity1.StartPosition.Value, entity2.StartPosition.Value);
            int end = Math.Max(entity1.EndPosition.Value, entity2.EndPosition.Value);
            
            // Add some context around the entities
            start = Math.Max(0, start - 50);
            end = Math.Min(content.Length, end + 50);
            
            return content.Substring(start, end - start);
        }
        
        /// <summary>
        /// Calculates the confidence score for a relation
        /// </summary>
        /// <param name="baseConfidence">Base confidence score</param>
        /// <param name="sourceEntity">Source entity</param>
        /// <param name="targetEntity">Target entity</param>
        /// <param name="matchText">The matched text</param>
        /// <returns>The calculated confidence score</returns>
        private double CalculateConfidenceScore(
            double baseConfidence, 
            Entity sourceEntity, 
            Entity targetEntity, 
            string matchText)
        {
            double confidence = baseConfidence;
            
            // Boost confidence based on entity confidence
            confidence += (sourceEntity.ConfidenceScore + targetEntity.ConfidenceScore) * 0.1;
            
            // Adjust based on match length (longer matches might be more reliable)
            if (matchText != null && matchText.Length > 30)
            {
                confidence += 0.05;
            }
            
            // Ensure confidence is within valid range
            return Math.Clamp(confidence, 0.0, 1.0);
        }
        
        /// <summary>
        /// Checks text for trigger phrases that indicate relations
        /// </summary>
        /// <param name="sourceEntity">Source entity</param>
        /// <param name="targetEntity">Target entity</param>
        /// <param name="textSegment">Text segment to check</param>
        /// <param name="sourceDocumentId">Source document ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>The relation if found; otherwise, null</returns>
        private Relation CheckTriggerPhrases(
            Entity sourceEntity,
            Entity targetEntity,
            string textSegment,
            string sourceDocumentId,
            string tenantId)
        {
            if (string.IsNullOrEmpty(textSegment))
                return null;
                
            foreach (var pattern in _patterns)
            {
                if (!ValidateRelation(sourceEntity, targetEntity, pattern.RelationType))
                    continue;
                    
                foreach (var trigger in pattern.TriggerPhrases)
                {
                    if (textSegment.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                    {
                        double confidence = pattern.BaseConfidenceScore * 0.9; // Slightly lower confidence for trigger-based matches
                        
                        return CreateRelation(
                            sourceEntity,
                            targetEntity,
                            pattern.RelationType,
                            textSegment,
                            confidence,
                            sourceDocumentId,
                            tenantId,
                            "TriggerPhrase");
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Initializes the relation extraction patterns
        /// </summary>
        /// <returns>List of relation extraction patterns</returns>
        private List<RelationExtractionPattern> InitializePatterns()
        {
            var patterns = new List<RelationExtractionPattern>();
            
            // WorksFor relation patterns
            patterns.Add(new RelationExtractionPattern(@"(works|working|employed|serves|serving)\s+(at|for|with)\s+")
            {
                Id = "WorksFor_1",
                Name = "Works For Pattern",
                RelationType = RelationType.WorksFor,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.Organization,
                BaseConfidenceScore = 0.8,
                TriggerPhrases = new List<string>
                {
                    "works for", "works at", "employed by", "employee of", "staff at", "team member at"
                }
            });
            
            // LocatedIn relation patterns
            patterns.Add(new RelationExtractionPattern(@"(located|based|situated|headquartered|residing|lives?)\s+(in|at|near)\s+")
            {
                Id = "LocatedIn_1",
                Name = "Located In Pattern",
                RelationType = RelationType.LocatedIn,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.Location,
                BaseConfidenceScore = 0.7,
                TriggerPhrases = new List<string>
                {
                    "located in", "based in", "lives in", "resides in", "from"
                }
            });
            
            // HasTitle relation patterns
            patterns.Add(new RelationExtractionPattern(@"(is|as|the)\s+(a|an|the)?\s*(?!.*company)(?!.*organization)")
            {
                Id = "HasTitle_1",
                Name = "Has Title Pattern",
                RelationType = RelationType.HasTitle,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.JobTitle,
                BaseConfidenceScore = 0.7,
                TriggerPhrases = new List<string>
                {
                    "is the", "serves as", "position of", "role of", "title of"
                }
            });
            
            // HasSkill relation patterns
            patterns.Add(new RelationExtractionPattern(@"(skilled|experienced|proficient|expertise|specializes)\s+(in|with|at)\s+")
            {
                Id = "HasSkill_1",
                Name = "Has Skill Pattern",
                RelationType = RelationType.HasSkill,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.Skill,
                BaseConfidenceScore = 0.6,
                TriggerPhrases = new List<string>
                {
                    "skilled in", "experienced with", "expertise in", "proficient in", "knows", "familiar with"
                }
            });
            
            // SubsidiaryOf relation patterns
            patterns.Add(new RelationExtractionPattern(@"(subsidiary|division|branch|unit|part)\s+(of|owned by)\s+")
            {
                Id = "SubsidiaryOf_1",
                Name = "Subsidiary Of Pattern",
                RelationType = RelationType.SubsidiaryOf,
                SourceEntityType = EntityType.Organization,
                TargetEntityType = EntityType.Organization,
                BaseConfidenceScore = 0.8,
                TriggerPhrases = new List<string>
                {
                    "subsidiary of", "owned by", "division of", "part of", "belongs to"
                }
            });
            
            // AuthorOf relation patterns
            patterns.Add(new RelationExtractionPattern(@"(authored|wrote|created|published|written by)\s+")
            {
                Id = "AuthorOf_1",
                Name = "Author Of Pattern",
                RelationType = RelationType.AuthorOf,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.Document,
                BaseConfidenceScore = 0.7,
                TriggerPhrases = new List<string>
                {
                    "authored by", "written by", "created by", "author of", "wrote"
                }
            });
            
            // Leads relation patterns
            patterns.Add(new RelationExtractionPattern(@"(leads|leading|manages|directing|heads|in charge of)\s+")
            {
                Id = "Leads_1",
                Name = "Leads Pattern",
                RelationType = RelationType.Leads,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.Project,
                BaseConfidenceScore = 0.7,
                TriggerPhrases = new List<string>
                {
                    "leads", "manages", "directs", "oversees", "in charge of", "head of"
                }
            });
            
            // ParticipatesIn relation patterns
            patterns.Add(new RelationExtractionPattern(@"(participates|participating|involved|working|contributor)\s+(in|on|with)\s+")
            {
                Id = "ParticipatesIn_1",
                Name = "Participates In Pattern",
                RelationType = RelationType.ParticipatesIn,
                SourceEntityType = EntityType.Person,
                TargetEntityType = EntityType.Project,
                BaseConfidenceScore = 0.6,
                TriggerPhrases = new List<string>
                {
                    "participates in", "works on", "involved in", "contributing to", "member of"
                }
            });
            
            // AssociatedWith generic relation pattern
            patterns.Add(new RelationExtractionPattern(@"(associated|related|connected|linked)\s+(with|to)\s+")
            {
                Id = "AssociatedWith_1",
                Name = "Associated With Pattern",
                RelationType = RelationType.AssociatedWith,
                IsBidirectional = true,
                BaseConfidenceScore = 0.5,
                TriggerPhrases = new List<string>
                {
                    "associated with", "related to", "connected to", "linked to"
                }
            });
            
            return patterns;
        }
    }
} 