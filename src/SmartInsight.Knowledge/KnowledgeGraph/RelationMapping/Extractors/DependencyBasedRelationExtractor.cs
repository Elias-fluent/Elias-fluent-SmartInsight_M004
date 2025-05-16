using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Extractors
{
    /// <summary>
    /// Relation extractor that uses syntactic dependencies to identify relationships
    /// This is a more sophisticated extractor that would typically use dependency parsing
    /// For example, from "John works for Microsoft", it would identify "John" -> "works for" -> "Microsoft"
    /// </summary>
    public class DependencyBasedRelationExtractor : BaseRelationExtractor
    {
        private readonly ILogger<DependencyBasedRelationExtractor> _logger;
        private readonly Dictionary<string, List<(string Verb, string Preposition, RelationType RelationType)>> _verbToRelationMap;
        
        /// <summary>
        /// Initializes a new instance of the DependencyBasedRelationExtractor class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public DependencyBasedRelationExtractor(
            ILogger<DependencyBasedRelationExtractor> logger) : base(logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _verbToRelationMap = InitializeVerbToRelationMap();
        }
        
        /// <summary>
        /// Extracts relations between entities from text content based on syntactic dependencies
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
                "Extracting relations using dependency-based approach for tenant {TenantId}",
                tenantId);
                
            var entityList = entities.ToList();
            if (entityList.Count < 2)
            {
                LogInformation("Not enough entities to extract relations (minimum 2 required)");
                return Enumerable.Empty<Relation>();
            }
            
            try
            {
                // In a real implementation, this would use a dependency parser
                // to extract subject-verb-object structures from sentences
                
                // For this demo/prototype implementation, we'll simulate dependency parsing
                // by looking for verb phrases between entities that match our relation verbs
                var relations = new List<Relation>();
                
                // Split content into sentences for easier processing
                var sentences = SplitIntoSentences(content);
                LogInformation("Processing {Count} sentences for dependency-based relation extraction", sentences.Count);
                
                foreach (var sentence in sentences)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    // Get entities in this sentence
                    var entitiesInSentence = GetEntitiesInSentence(entityList, sentence);
                    if (entitiesInSentence.Count < 2)
                        continue;
                        
                    // Process this sentence for relations
                    var sentenceRelations = ExtractRelationsFromSentence(
                        sentence,
                        entitiesInSentence,
                        sourceDocumentId,
                        tenantId);
                        
                    relations.AddRange(sentenceRelations);
                }
                
                LogInformation(
                    "Completed dependency-based relation extraction. Found {Count} relations", 
                    relations.Count);
                    
                return relations;
            }
            catch (Exception ex)
            {
                LogError(
                    ex,
                    "Error extracting relations using dependency-based approach: {ErrorMessage}",
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
            return _verbToRelationMap.Values
                .SelectMany(values => values.Select(v => v.RelationType))
                .Distinct();
        }
        
        /// <summary>
        /// Gets extraction patterns used by this extractor
        /// </summary>
        /// <returns>The relation extraction patterns</returns>
        public override IEnumerable<RelationExtractionPattern> GetExtractionPatterns()
        {
            // This extractor doesn't use regex patterns directly but could return
            // patterns derived from verb-preposition combinations
            return Enumerable.Empty<RelationExtractionPattern>();
        }
        
        /// <summary>
        /// Splits text content into sentences
        /// </summary>
        /// <param name="content">The text content</param>
        /// <returns>The list of sentences</returns>
        private List<string> SplitIntoSentences(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<string>();
                
            // Simple sentence splitter
            // In a real implementation, this would use a more sophisticated approach
            var sentences = content.Split(
                new[] { '.', '!', '?' },
                StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
                
            return sentences;
        }
        
        /// <summary>
        /// Gets entities that appear in a given sentence
        /// </summary>
        /// <param name="entities">All entities</param>
        /// <param name="sentence">The sentence to check</param>
        /// <returns>Entities in the sentence</returns>
        private List<Entity> GetEntitiesInSentence(List<Entity> entities, string sentence)
        {
            if (string.IsNullOrEmpty(sentence) || entities == null || !entities.Any())
                return new List<Entity>();
                
            // Simple check for entity name in sentence
            // In a real implementation, this would use position information
            return entities
                .Where(e => sentence.Contains(e.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        /// <summary>
        /// Extracts relations from a sentence based on dependency structure
        /// </summary>
        /// <param name="sentence">The sentence to process</param>
        /// <param name="entities">Entities in the sentence</param>
        /// <param name="sourceDocumentId">Source document ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>The extracted relations</returns>
        private IEnumerable<Relation> ExtractRelationsFromSentence(
            string sentence,
            List<Entity> entities,
            string sourceDocumentId,
            string tenantId)
        {
            var relations = new List<Relation>();
            
            // Generate all possible entity pairs in this sentence
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = 0; j < entities.Count; j++)
                {
                    if (i == j) continue;
                    
                    var sourceEntity = entities[i];
                    var targetEntity = entities[j];
                    
                    // Look for verb patterns between these entities
                    var verbPatterns = FindVerbPatterns(sentence, sourceEntity, targetEntity);
                    
                    foreach (var (verb, preposition) in verbPatterns)
                    {
                        // Lookup relation type for this verb-preposition combination
                        var relationType = LookupRelationType(verb, preposition, sourceEntity, targetEntity);
                        if (relationType != null)
                        {
                            // Validate the relation based on entity types
                            if (ValidateRelation(sourceEntity, targetEntity, relationType.Value))
                            {
                                var relation = CreateRelation(
                                    sourceEntity,
                                    targetEntity,
                                    relationType.Value,
                                    sentence,
                                    0.7, // Base confidence score for dependency-based relations
                                    sourceDocumentId,
                                    tenantId,
                                    "DependencyBased");
                                    
                                relations.Add(relation);
                                
                                LogInformation(
                                    "Extracted {RelationType} relation between '{SourceEntity}' and '{TargetEntity}' based on verb phrase '{Verb} {Preposition}'",
                                    relationType.Value,
                                    sourceEntity.Name,
                                    targetEntity.Name,
                                    verb,
                                    preposition);
                            }
                        }
                    }
                }
            }
            
            return relations;
        }
        
        /// <summary>
        /// Finds verb patterns between two entities in a sentence
        /// </summary>
        /// <param name="sentence">The sentence to process</param>
        /// <param name="sourceEntity">Source entity</param>
        /// <param name="targetEntity">Target entity</param>
        /// <returns>List of (verb, preposition) tuples found between entities</returns>
        private List<(string Verb, string Preposition)> FindVerbPatterns(
            string sentence,
            Entity sourceEntity,
            Entity targetEntity)
        {
            var patterns = new List<(string, string)>();
            
            if (string.IsNullOrEmpty(sentence) || sourceEntity == null || targetEntity == null)
                return patterns;
                
            // Basic implementation to extract verb phrases
            // In a real system, this would use dependency parsing or POS tagging
            
            // Find entity positions in the sentence
            int sourcePos = sentence.IndexOf(sourceEntity.Name, StringComparison.OrdinalIgnoreCase);
            int targetPos = sentence.IndexOf(targetEntity.Name, StringComparison.OrdinalIgnoreCase);
            
            if (sourcePos < 0 || targetPos < 0)
                return patterns;
                
            // Determine the order of entities
            bool sourceFirst = sourcePos < targetPos;
            int firstPos = sourceFirst ? sourcePos + sourceEntity.Name.Length : targetPos + targetEntity.Name.Length;
            int secondPos = sourceFirst ? targetPos : sourcePos;
            
            // Only look at cases where source entity comes first (subject-verb-object structure)
            if (!sourceFirst)
                return patterns;
                
            // Extract the text between the entities
            if (secondPos > firstPos)
            {
                string between = sentence.Substring(firstPos, secondPos - firstPos).Trim().ToLower();
                
                // Check for verb-preposition combinations in our map
                foreach (var entry in _verbToRelationMap)
                {
                    string verb = entry.Key;
                    if (between.Contains(verb))
                    {
                        foreach (var (_, preposition, _) in entry.Value)
                        {
                            if (between.Contains($"{verb} {preposition}"))
                            {
                                patterns.Add((verb, preposition));
                            }
                        }
                    }
                }
            }
            
            return patterns;
        }
        
        /// <summary>
        /// Looks up the relation type for a verb-preposition combination
        /// </summary>
        /// <param name="verb">The verb</param>
        /// <param name="preposition">The preposition</param>
        /// <param name="sourceEntity">Source entity</param>
        /// <param name="targetEntity">Target entity</param>
        /// <returns>The relation type if found; otherwise, null</returns>
        private RelationType? LookupRelationType(
            string verb,
            string preposition,
            Entity sourceEntity,
            Entity targetEntity)
        {
            if (string.IsNullOrEmpty(verb))
                return null;
                
            if (_verbToRelationMap.TryGetValue(verb, out var relations))
            {
                // Look for exact match with preposition
                var exactMatch = relations.FirstOrDefault(r => 
                    r.Preposition.Equals(preposition, StringComparison.OrdinalIgnoreCase));
                    
                if (exactMatch != default)
                {
                    return exactMatch.RelationType;
                }
                
                // If no exact match, look for a generic relation
                return relations.FirstOrDefault().RelationType;
            }
            
            return null;
        }
        
        /// <summary>
        /// Initializes the map of verbs to relation types
        /// </summary>
        /// <returns>Dictionary mapping verbs to relation types</returns>
        private Dictionary<string, List<(string Verb, string Preposition, RelationType RelationType)>> InitializeVerbToRelationMap()
        {
            var map = new Dictionary<string, List<(string, string, RelationType)>>(StringComparer.OrdinalIgnoreCase);
            
            // Works For relations
            map["works"] = new List<(string, string, RelationType)>
            {
                ("works", "for", RelationType.WorksFor),
                ("works", "at", RelationType.WorksFor),
                ("works", "with", RelationType.WorksFor)
            };
            
            map["employed"] = new List<(string, string, RelationType)>
            {
                ("employed", "by", RelationType.WorksFor),
                ("employed", "at", RelationType.WorksFor)
            };
            
            map["serves"] = new List<(string, string, RelationType)>
            {
                ("serves", "at", RelationType.WorksFor),
                ("serves", "with", RelationType.WorksFor)
            };
            
            // Located In relations
            map["located"] = new List<(string, string, RelationType)>
            {
                ("located", "in", RelationType.LocatedIn),
                ("located", "at", RelationType.LocatedIn)
            };
            
            map["based"] = new List<(string, string, RelationType)>
            {
                ("based", "in", RelationType.LocatedIn),
                ("based", "at", RelationType.LocatedIn)
            };
            
            map["lives"] = new List<(string, string, RelationType)>
            {
                ("lives", "in", RelationType.LocatedIn),
                ("lives", "at", RelationType.LocatedIn)
            };
            
            map["headquartered"] = new List<(string, string, RelationType)>
            {
                ("headquartered", "in", RelationType.HeadquarteredIn),
                ("headquartered", "at", RelationType.HeadquarteredIn)
            };
            
            // Has Title relations
            map["is"] = new List<(string, string, RelationType)>
            {
                ("is", "the", RelationType.HasTitle),
                ("is", "a", RelationType.HasTitle)
            };
            
            map["serves"] = new List<(string, string, RelationType)>
            {
                ("serves", "as", RelationType.HasTitle)
            };
            
            // Has Skill relations
            map["skilled"] = new List<(string, string, RelationType)>
            {
                ("skilled", "in", RelationType.HasSkill),
                ("skilled", "at", RelationType.HasSkill)
            };
            
            map["experienced"] = new List<(string, string, RelationType)>
            {
                ("experienced", "in", RelationType.HasSkill),
                ("experienced", "with", RelationType.HasSkill)
            };
            
            map["specializes"] = new List<(string, string, RelationType)>
            {
                ("specializes", "in", RelationType.HasSkill)
            };
            
            // Created relations
            map["created"] = new List<(string, string, RelationType)>
            {
                ("created", "", RelationType.Created)
            };
            
            map["developed"] = new List<(string, string, RelationType)>
            {
                ("developed", "", RelationType.Created)
            };
            
            map["authored"] = new List<(string, string, RelationType)>
            {
                ("authored", "", RelationType.AuthorOf)
            };
            
            map["wrote"] = new List<(string, string, RelationType)>
            {
                ("wrote", "", RelationType.AuthorOf)
            };
            
            // Leads relations
            map["leads"] = new List<(string, string, RelationType)>
            {
                ("leads", "", RelationType.Leads)
            };
            
            map["manages"] = new List<(string, string, RelationType)>
            {
                ("manages", "", RelationType.Leads)
            };
            
            map["directs"] = new List<(string, string, RelationType)>
            {
                ("directs", "", RelationType.Leads)
            };
            
            // ParticipatesIn relations
            map["participates"] = new List<(string, string, RelationType)>
            {
                ("participates", "in", RelationType.ParticipatesIn)
            };
            
            map["works"] = new List<(string, string, RelationType)>
            {
                ("works", "on", RelationType.ParticipatesIn)
            };
            
            map["contributes"] = new List<(string, string, RelationType)>
            {
                ("contributes", "to", RelationType.ParticipatesIn)
            };
            
            return map;
        }
    }
} 