using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping
{
    /// <summary>
    /// Advanced pipeline for coordinating relation extraction and triple store integration
    /// </summary>
    public class AdvancedRelationMappingPipeline : IRelationMappingPipeline
    {
        private readonly ILogger<AdvancedRelationMappingPipeline> _logger;
        private readonly IRelationExtractorFactory _extractorFactory;
        private readonly IRelationToTripleMapper _tripleMapper;
        private readonly ITripleStore _tripleStore;
        private readonly List<IRelationExtractor> _extractors;
        private readonly RelationMappingOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the AdvancedRelationMappingPipeline class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="extractorFactory">The relation extractor factory</param>
        /// <param name="tripleMapper">The relation to triple mapper</param>
        /// <param name="tripleStore">The triple store</param>
        /// <param name="options">The relation mapping options</param>
        public AdvancedRelationMappingPipeline(
            ILogger<AdvancedRelationMappingPipeline> logger,
            IRelationExtractorFactory extractorFactory,
            IRelationToTripleMapper tripleMapper,
            ITripleStore tripleStore,
            IOptions<RelationMappingOptions> options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractorFactory = extractorFactory ?? throw new ArgumentNullException(nameof(extractorFactory));
            _tripleMapper = tripleMapper ?? throw new ArgumentNullException(nameof(tripleMapper));
            _tripleStore = tripleStore ?? throw new ArgumentNullException(nameof(tripleStore));
            _options = options?.Value ?? new RelationMappingOptions();
            _extractors = _extractorFactory.CreateAllExtractors().ToList();
            
            _logger.LogInformation(
                "Initialized AdvancedRelationMappingPipeline with {Count} extractors",
                _extractors.Count);
        }
        
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
        public async Task<IEnumerable<Relation>> ProcessAsync(
            string content,
            IEnumerable<Entity> entities,
            string sourceDocumentId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));
                
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                _logger.LogInformation(
                    "Starting relation extraction pipeline for tenant {TenantId}",
                    tenantId);
                    
                // Select extractors to use
                var extractorsToUse = extractorTypes != null
                    ? GetExtractorsOfTypes(extractorTypes)
                    : _extractors;
                    
                if (!extractorsToUse.Any())
                {
                    _logger.LogWarning(
                        "No relation extractors available for processing. Using all registered extractors.");
                        
                    extractorsToUse = _extractors;
                }
                
                _logger.LogInformation(
                    "Using {Count} relation extractors for processing",
                    extractorsToUse.Count);
                    
                // Process using each selected extractor
                var allRelations = new List<Relation>();
                
                foreach (var extractor in extractorsToUse)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    try
                    {
                        _logger.LogInformation(
                            "Processing with {ExtractorType} extractor",
                            extractor.GetType().Name);
                            
                        var relations = await extractor.ExtractRelationsAsync(
                            content,
                            entities,
                            sourceDocumentId,
                            tenantId,
                            cancellationToken);
                            
                        if (relations != null)
                        {
                            allRelations.AddRange(relations);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error extracting relations with {ExtractorType}: {ErrorMessage}",
                            extractor.GetType().Name,
                            ex.Message);
                    }
                }
                
                // Validate and deduplicate relations
                var validatedRelations = ValidateRelations(allRelations, _options.MinConfidenceThreshold);
                var uniqueRelations = DeduplicateRelations(validatedRelations);
                
                _logger.LogInformation(
                    "Extracted and validated {Count} unique relations",
                    uniqueRelations.Count());
                    
                // Store relations in the triple store if enabled
                if (_options.AutoConvertToTriples)
                {
                    await StoreRelationsAsTriples(uniqueRelations, tenantId, cancellationToken);
                }
                
                return uniqueRelations;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in relation mapping pipeline: {ErrorMessage}",
                    ex.Message);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Gets all registered relation extractors
        /// </summary>
        /// <returns>The registered relation extractors</returns>
        public IEnumerable<IRelationExtractor> GetRegisteredExtractors()
        {
            return _extractors;
        }
        
        /// <summary>
        /// Gets a specific relation extractor by type name
        /// </summary>
        /// <param name="typeName">The relation extractor type name</param>
        /// <returns>The relation extractor instance, or null if not found</returns>
        public IRelationExtractor GetExtractor(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
                
            return _extractors.FirstOrDefault(e => 
                e.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Gets the relation types supported across all extractors
        /// </summary>
        /// <returns>The supported relation types</returns>
        public IEnumerable<RelationType> GetSupportedRelationTypes()
        {
            return _extractors
                .SelectMany(e => e.GetSupportedRelationTypes())
                .Distinct();
        }
        
        /// <summary>
        /// Validates all extracted relations
        /// </summary>
        /// <param name="relations">The relations to validate</param>
        /// <param name="minConfidenceThreshold">Minimum confidence threshold</param>
        /// <returns>The validated relations</returns>
        public IEnumerable<Relation> ValidateRelations(
            IEnumerable<Relation> relations,
            double minConfidenceThreshold = 0.5)
        {
            if (relations == null)
                return Enumerable.Empty<Relation>();
                
            var validRelations = new List<Relation>();
            
            foreach (var relation in relations)
            {
                // Skip relations below the confidence threshold
                if (relation.ConfidenceScore < minConfidenceThreshold)
                {
                    _logger.LogDebug(
                        "Skipping low-confidence relation ({Confidence}) between '{Source}' and '{Target}'",
                        relation.ConfidenceScore,
                        relation.SourceEntity?.Name,
                        relation.TargetEntity?.Name);
                        
                    continue;
                }
                
                // Skip self-relations unless explicitly allowed
                if (!_options.AllowSelfRelations && relation.SourceEntityId == relation.TargetEntityId)
                {
                    _logger.LogDebug(
                        "Skipping self-relation for entity '{Entity}'",
                        relation.SourceEntity?.Name);
                        
                    continue;
                }
                
                // Validate that source and target entities exist
                if (relation.SourceEntity == null || relation.TargetEntity == null)
                {
                    _logger.LogWarning(
                        "Skipping relation with missing source or target entity: {RelationType}",
                        relation.RelationType);
                        
                    continue;
                }
                
                // Validate tenant ID
                if (string.IsNullOrEmpty(relation.TenantId))
                {
                    _logger.LogWarning(
                        "Skipping relation without tenant ID: {RelationType} between '{Source}' and '{Target}'",
                        relation.RelationType,
                        relation.SourceEntity.Name,
                        relation.TargetEntity.Name);
                        
                    continue;
                }
                
                // Validate entity types for the relation if the validation is enabled
                if (_options.ValidateEntityTypes)
                {
                    // Find any extractor that supports this relation type
                    var supportingExtractor = _extractors.FirstOrDefault(e => 
                        e.GetSupportedRelationTypes().Contains(relation.RelationType));
                        
                    if (supportingExtractor != null)
                    {
                        // Check if this is a valid entity type combination for this relation
                        bool isValid = supportingExtractor.ValidateRelation(
                            relation.SourceEntity, 
                            relation.TargetEntity, 
                            relation.RelationType);
                            
                        if (!isValid)
                        {
                            _logger.LogWarning(
                                "Skipping invalid entity type combination for relation {RelationType}: {SourceType} -> {TargetType}",
                                relation.RelationType,
                                relation.SourceEntity.Type,
                                relation.TargetEntity.Type);
                                
                            continue;
                        }
                    }
                }
                
                validRelations.Add(relation);
            }
            
            return validRelations;
        }
        
        /// <summary>
        /// Stores all relations as triples in the triple store
        /// </summary>
        /// <param name="relations">The relations to store</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of triples stored</returns>
        private async Task<int> StoreRelationsAsTriples(
            IEnumerable<Relation> relations,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Converting relations to triples for tenant {TenantId}",
                    tenantId);
                    
                // Use the triple mapper to convert and store relations
                var count = await _tripleMapper.MapAndStoreBatchAsync(
                    relations,
                    tenantId,
                    _options.DefaultGraphUri,
                    cancellationToken);
                    
                _logger.LogInformation(
                    "Stored {Count} triples in the triple store for tenant {TenantId}",
                    count,
                    tenantId);
                    
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error storing relations as triples for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                return 0;
            }
        }
        
        /// <summary>
        /// Gets extractors of specific types
        /// </summary>
        /// <param name="extractorTypes">The extractor type names</param>
        /// <returns>The extractors of the specified types</returns>
        private List<IRelationExtractor> GetExtractorsOfTypes(IEnumerable<string> extractorTypes)
        {
            if (extractorTypes == null)
                return new List<IRelationExtractor>();
                
            return _extractors
                .Where(e => extractorTypes.Any(t => 
                    e.GetType().Name.Contains(t, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        
        /// <summary>
        /// Deduplicates relations based on source and target entities and relation type
        /// </summary>
        /// <param name="relations">The relations to deduplicate</param>
        /// <returns>Deduplicated relations</returns>
        private IEnumerable<Relation> DeduplicateRelations(IEnumerable<Relation> relations)
        {
            if (relations == null)
                return Enumerable.Empty<Relation>();
                
            // Group by source entity, target entity, and relation type
            var uniqueRelations = new Dictionary<string, Relation>();
            
            foreach (var relation in relations)
            {
                // Create a composite key for the relation
                var key = $"{relation.SourceEntityId}_{relation.TargetEntityId}_{relation.RelationType}";
                
                if (uniqueRelations.TryGetValue(key, out var existingRelation))
                {
                    // Keep the relation with the higher confidence score
                    if (relation.ConfidenceScore > existingRelation.ConfidenceScore)
                    {
                        uniqueRelations[key] = relation;
                    }
                }
                else
                {
                    uniqueRelations[key] = relation;
                }
            }
            
            return uniqueRelations.Values;
        }
    }
    
    /// <summary>
    /// Options for configuring relation mapping behavior
    /// </summary>
    public class RelationMappingOptions
    {
        /// <summary>
        /// Minimum confidence threshold for relations to be considered valid
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;
        
        /// <summary>
        /// Whether to allow relations between an entity and itself
        /// </summary>
        public bool AllowSelfRelations { get; set; } = false;
        
        /// <summary>
        /// Whether to validate entity types for relation compatibility
        /// </summary>
        public bool ValidateEntityTypes { get; set; } = true;
        
        /// <summary>
        /// Whether to automatically convert relations to triples in the triple store
        /// </summary>
        public bool AutoConvertToTriples { get; set; } = true;
        
        /// <summary>
        /// The default graph URI to use for storing triples
        /// </summary>
        public string DefaultGraphUri { get; set; } = null;
    }
} 