using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Examples
{
    /// <summary>
    /// Example class demonstrating how to use the relation mapping pipeline
    /// </summary>
    public class RelationMappingExample
    {
        private readonly ILogger<RelationMappingExample> _logger;
        private readonly IRelationMappingPipeline _relationPipeline;
        private readonly IRelationToTripleMapper _tripleMapper;
        private readonly ITripleStore _tripleStore;
        
        /// <summary>
        /// Initializes a new instance of the RelationMappingExample class
        /// </summary>
        public RelationMappingExample(
            ILogger<RelationMappingExample> logger,
            IRelationMappingPipeline relationPipeline,
            IRelationToTripleMapper tripleMapper,
            ITripleStore tripleStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _relationPipeline = relationPipeline ?? throw new ArgumentNullException(nameof(relationPipeline));
            _tripleMapper = tripleMapper ?? throw new ArgumentNullException(nameof(tripleMapper));
            _tripleStore = tripleStore ?? throw new ArgumentNullException(nameof(tripleStore));
        }
        
        /// <summary>
        /// Example method demonstrating the basic workflow for relation extraction and mapping to triples
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <returns>The number of triples created</returns>
        public async Task<int> ProcessContentAsync(string content, string tenantId)
        {
            try
            {
                _logger.LogInformation(
                    "Processing content for tenant {TenantId}",
                    tenantId);
                    
                // Simulate entity extraction with example entities
                var entities = ExtractEntitiesFromContent(content);
                
                // Extract relations between entities
                var relations = await _relationPipeline.ProcessAsync(
                    content,
                    entities,
                    "example-document-id",
                    tenantId);
                    
                // Map and store the relations as triples
                var tripleCount = await _tripleMapper.MapAndStoreBatchAsync(
                    relations,
                    tenantId);
                    
                _logger.LogInformation(
                    "Created {Count} triples for tenant {TenantId}",
                    tripleCount,
                    tenantId);
                    
                return tripleCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing content for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Example method showing how to query the triple store and analyze relations
        /// </summary>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <returns>A dictionary with relation type counts</returns>
        public async Task<Dictionary<string, int>> AnalyzeRelationsAsync(string tenantId)
        {
            try
            {
                // Get all relation types supported by our extractors
                var relationTypes = _relationPipeline.GetSupportedRelationTypes();
                
                var result = new Dictionary<string, int>();
                
                // Query the triple store for each relation type
                foreach (var relationType in relationTypes)
                {
                    // Convert relation type to predicate URI following the same pattern as mapper
                    string predicateUri = $"http://smartinsight.com/ontology/{relationType.ToString().ToLowerInvariant()}";
                    
                    // Create a query for triples with this predicate
                    var query = new TripleQuery
                    {
                        TenantId = tenantId,
                        PredicateUri = predicateUri
                    };
                    
                    // Query the triple store
                    var queryResult = await _tripleStore.QueryAsync(query, tenantId);
                    
                    // Add to the result dictionary
                    result[relationType.ToString()] = queryResult.Triples.Count;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error analyzing relations for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Simulated entity extraction for the example
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <returns>A list of extracted entities</returns>
        private List<Entity> ExtractEntitiesFromContent(string content)
        {
            // This is a simplified example of entity extraction
            // In a real application, you would use a proper entity extraction system
            
            var entities = new List<Entity>();
            
            // Example: Look for person names (simplified)
            if (content.Contains("John"))
            {
                entities.Add(new Entity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "John Smith",
                    Type = EntityType.Person,
                    ConfidenceScore = 0.9
                });
            }
            
            // Example: Look for organization names (simplified)
            if (content.Contains("Acme"))
            {
                entities.Add(new Entity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Acme Corporation",
                    Type = EntityType.Organization,
                    ConfidenceScore = 0.85
                });
            }
            
            // Example: Look for locations (simplified)
            if (content.Contains("New York"))
            {
                entities.Add(new Entity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "New York",
                    Type = EntityType.Location,
                    ConfidenceScore = 0.95
                });
            }
            
            return entities;
        }
    }
} 