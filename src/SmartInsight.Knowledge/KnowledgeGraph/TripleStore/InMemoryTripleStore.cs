using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore
{
    /// <summary>
    /// In-memory implementation of the Triple Store using dotNetRDF
    /// </summary>
    public class InMemoryTripleStore : Interfaces.ITripleStore
    {
        private readonly ILogger<InMemoryTripleStore> _logger;
        private readonly TripleStoreOptions _options;
        private readonly VDS.RDF.TripleStore _store;
        private readonly Dictionary<string, IGraph> _graphCache = new Dictionary<string, IGraph>();
        private readonly Dictionary<string, Models.Triple> _tripleCache = new Dictionary<string, Models.Triple>();
        private readonly IKnowledgeGraphVersioningManager _versioningManager;
        
        /// <summary>
        /// Initializes a new instance of the InMemoryTripleStore class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="options">The triple store options</param>
        /// <param name="versioningManager">The knowledge graph versioning manager</param>
        public InMemoryTripleStore(
            ILogger<InMemoryTripleStore> logger,
            IOptions<TripleStoreOptions> options,
            IKnowledgeGraphVersioningManager versioningManager = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _versioningManager = versioningManager; // Can be null if versioning is not required
            _store = new VDS.RDF.TripleStore();
            
            _logger.LogInformation("Initialized in-memory triple store {Versioning}", 
                _versioningManager != null ? "with versioning support" : "without versioning support");
        }
        
        /// <summary>
        /// Adds a triple to the store
        /// </summary>
        /// <param name="triple">The triple to add</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> AddTripleAsync(
            Models.Triple triple, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (triple == null)
                throw new ArgumentNullException(nameof(triple));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Ensure triple has the correct tenant ID
                triple.TenantId = tenantId;
                
                // Set default graph URI if not specified
                if (string.IsNullOrEmpty(triple.GraphUri))
                {
                    triple.GraphUri = _options.DefaultGraphUri;
                }
                
                // Get or create the graph
                var graph = await GetOrCreateGraphAsync(triple.GraphUri, tenantId);
                
                // Create the triple in the RDF graph
                var subjectNode = CreateUriNode(graph, triple.SubjectId);
                var predicateNode = CreateUriNode(graph, triple.PredicateUri);
                INode objectNode;
                
                if (triple.IsLiteral)
                {
                    if (!string.IsNullOrEmpty(triple.LiteralDataType))
                    {
                        // Typed literal
                        var dataTypeUri = new Uri(triple.LiteralDataType);
                        objectNode = graph.CreateLiteralNode(triple.ObjectId, dataTypeUri);
                    }
                    else if (!string.IsNullOrEmpty(triple.LanguageTag))
                    {
                        // Language tagged literal
                        objectNode = graph.CreateLiteralNode(triple.ObjectId, triple.LanguageTag);
                    }
                    else
                    {
                        // Plain literal
                        objectNode = graph.CreateLiteralNode(triple.ObjectId);
                    }
                }
                else
                {
                    // URI node
                    objectNode = CreateUriNode(graph, triple.ObjectId);
                }
                
                var rdfTriple = new VDS.RDF.Triple(subjectNode, predicateNode, objectNode);
                
                // Add the triple to the graph
                graph.Assert(rdfTriple);
                
                // Cache the triple
                _tripleCache[triple.Id] = triple;
                
                _logger.LogDebug(
                    "Added triple {TripleId} to graph {GraphUri} for tenant {TenantId}",
                    triple.Id,
                    triple.GraphUri,
                    tenantId);
                
                // Record version if versioning manager is available
                if (_versioningManager != null)
                {
                    try
                    {
                        await _versioningManager.RecordVersionAsync(
                            triple, 
                            ChangeType.Creation, 
                            tenantId,
                            triple.ProvenanceInfo.ContainsKey("ChangedByUserId") ? triple.ProvenanceInfo["ChangedByUserId"].ToString() : null,
                            triple.ProvenanceInfo.ContainsKey("ChangeComment") ? triple.ProvenanceInfo["ChangeComment"].ToString() : null,
                            cancellationToken);
                            
                        _logger.LogDebug(
                            "Recorded version for triple {TripleId} with change type Creation",
                            triple.Id);
                    }
                    catch (Exception versionEx)
                    {
                        _logger.LogWarning(
                            versionEx,
                            "Failed to record version for triple {TripleId}: {ErrorMessage}",
                            triple.Id,
                            versionEx.Message);
                        // Continue despite versioning failure
                    }
                }
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding triple {TripleId} for tenant {TenantId}: {ErrorMessage}",
                    triple.Id,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Creates a URI node in the graph
        /// </summary>
        private IUriNode CreateUriNode(IGraph graph, string uri)
        {
            // If the URI doesn't start with http://, https://, or urn:, prepend http://
            if (!uri.StartsWith("http://") && !uri.StartsWith("https://") && !uri.StartsWith("urn:"))
            {
                uri = "http://" + uri;
            }
            
            return graph.CreateUriNode(new Uri(uri));
        }
        
        /// <summary>
        /// Gets or creates a graph with the specified URI
        /// </summary>
        private async Task<IGraph> GetOrCreateGraphAsync(string graphUri, string tenantId)
        {
            // Use tenant-specific cache key
            var cacheKey = $"{tenantId}:{graphUri}";
            
            if (_graphCache.TryGetValue(cacheKey, out var graph))
            {
                return graph;
            }
            
            // Create new graph
            graph = new Graph();
            graph.BaseUri = new Uri(graphUri);
            
            // Add tenant ID as a graph property
            var tenantPredicate = graph.CreateUriNode(new Uri("http://smartinsight.com/ontology/tenantId"));
            var tenantObject = graph.CreateLiteralNode(tenantId);
            graph.Assert(graph.CreateUriNode(graph.BaseUri), tenantPredicate, tenantObject);
            
            // Add to the store
            _store.Add(graph);
            
            // Cache the graph
            _graphCache[cacheKey] = graph;
            
            _logger.LogDebug(
                "Created graph {GraphUri} for tenant {TenantId}",
                graphUri,
                tenantId);
                
            return graph;
        }
        
        /// <summary>
        /// Adds multiple triples to the store in a batch
        /// </summary>
        /// <param name="triples">The triples to add</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of triples successfully added</returns>
        public async Task<int> AddTriplesAsync(
            IEnumerable<Models.Triple> triples, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (triples == null)
                throw new ArgumentNullException(nameof(triples));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            int count = 0;
            
            try
            {
                foreach (var triple in triples)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    if (await AddTripleAsync(triple, tenantId, cancellationToken))
                    {
                        count++;
                    }
                }
                
                _logger.LogInformation(
                    "Added {Count} triples out of {Total} for tenant {TenantId}",
                    count,
                    triples.Count(),
                    tenantId);
                    
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding triples in batch for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                return count;
            }
        }
        
        /// <summary>
        /// Creates a triple from a relation and adds it to the store
        /// </summary>
        /// <param name="relation">The relation to convert to a triple</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional named graph URI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created triple if successful, null otherwise</returns>
        public async Task<Models.Triple> AddRelationAsTripleAsync(
            Relation relation,
            string tenantId,
            string graphUri = null,
            CancellationToken cancellationToken = default)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Create a triple from the relation
                var triple = new Models.Triple
                {
                    SubjectId = relation.SourceEntityId,
                    PredicateUri = $"http://smartinsight.com/ontology/relation/{relation.RelationType.ToString()}",
                    ObjectId = relation.TargetEntityId,
                    IsLiteral = false,
                    TenantId = tenantId,
                    GraphUri = graphUri ?? _options.DefaultGraphUri,
                    ConfidenceScore = relation.ConfidenceScore,
                    SourceDocumentId = relation.SourceDocumentId
                };
                
                // Add the triple to the store
                bool success = await AddTripleAsync(triple, tenantId, cancellationToken);
                
                if (success)
                {
                    _logger.LogDebug(
                        "Added relation {RelationType} as triple {TripleId} for tenant {TenantId}",
                        relation.RelationType,
                        triple.Id,
                        tenantId);
                        
                    return triple;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding relation as triple for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                return null;
            }
        }
        
        /// <summary>
        /// Creates triples from entity attributes and adds them to the store
        /// </summary>
        /// <param name="entity">The entity to extract attribute triples from</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional named graph URI</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created triples if successful</returns>
        public async Task<IEnumerable<Models.Triple>> AddEntityAttributesAsTripleAsync(
            Entity entity,
            string tenantId,
            string graphUri = null,
            CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                var triples = new List<Models.Triple>();
                
                // Add a triple for the entity type
                var typeTriple = new Models.Triple
                {
                    SubjectId = entity.Id,
                    PredicateUri = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                    ObjectId = $"http://smartinsight.com/ontology/entity/{entity.Type.ToString()}",
                    IsLiteral = false,
                    TenantId = tenantId,
                    GraphUri = graphUri ?? _options.DefaultGraphUri,
                    ConfidenceScore = 1.0,
                    SourceDocumentId = entity.SourceId
                };
                
                if (await AddTripleAsync(typeTriple, tenantId, cancellationToken))
                {
                    triples.Add(typeTriple);
                }
                
                // Add triples for each attribute
                foreach (var attr in entity.Attributes)
                {
                    var attrTriple = new Models.Triple
                    {
                        SubjectId = entity.Id,
                        PredicateUri = $"http://smartinsight.com/ontology/attribute/{attr.Key}",
                        ObjectId = attr.Value?.ToString() ?? string.Empty,
                        IsLiteral = true,
                        TenantId = tenantId,
                        GraphUri = graphUri ?? _options.DefaultGraphUri,
                        ConfidenceScore = 1.0,
                        SourceDocumentId = entity.SourceId
                    };
                    
                    if (await AddTripleAsync(attrTriple, tenantId, cancellationToken))
                    {
                        triples.Add(attrTriple);
                    }
                }
                
                // Add a triple for the entity name/text
                var nameTriple = new Models.Triple
                {
                    SubjectId = entity.Id,
                    PredicateUri = "http://smartinsight.com/ontology/attribute/name",
                    ObjectId = entity.Name,
                    IsLiteral = true,
                    TenantId = tenantId,
                    GraphUri = graphUri ?? _options.DefaultGraphUri,
                    ConfidenceScore = 1.0,
                    SourceDocumentId = entity.SourceId
                };
                
                if (await AddTripleAsync(nameTriple, tenantId, cancellationToken))
                {
                    triples.Add(nameTriple);
                }
                
                _logger.LogDebug(
                    "Added {Count} attribute triples for entity {EntityId} for tenant {TenantId}",
                    triples.Count,
                    entity.Id,
                    tenantId);
                    
                return triples;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error adding entity attributes as triples for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                return Enumerable.Empty<Models.Triple>();
            }
        }
        
        /// <summary>
        /// Removes a triple from the store
        /// </summary>
        /// <param name="tripleId">The ID of the triple to remove</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RemoveTripleAsync(
            string tripleId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tripleId))
                throw new ArgumentNullException(nameof(tripleId));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Check if the triple exists and belongs to the tenant
                if (!_tripleCache.TryGetValue(tripleId, out var triple) || triple.TenantId != tenantId)
                {
                    _logger.LogWarning(
                        "Triple {TripleId} not found or doesn't belong to tenant {TenantId}",
                        tripleId,
                        tenantId);
                    return false;
                }
                
                // Get the graph
                var cacheKey = $"{tenantId}:{triple.GraphUri}";
                if (!_graphCache.TryGetValue(cacheKey, out var graph))
                {
                    _logger.LogWarning(
                        "Graph {GraphUri} not found for tenant {TenantId}",
                        triple.GraphUri,
                        tenantId);
                    return false;
                }
                
                // Try to find the triple in the RDF graph
                var subjectNode = CreateUriNode(graph, triple.SubjectId);
                var predicateNode = CreateUriNode(graph, triple.PredicateUri);
                
                // Find triples to remove
                var triplesToRemove = graph.GetTriplesWithSubjectPredicate(subjectNode, predicateNode).ToList();
                
                if (!triplesToRemove.Any())
                {
                    _logger.LogWarning(
                        "Triple {TripleId} not found in graph {GraphUri}",
                        tripleId,
                        triple.GraphUri);
                    return false;
                }
                
                // Remove triples from the graph
                foreach (var t in triplesToRemove)
                {
                    graph.Retract(t);
                }
                
                // Record version if versioning manager is available
                if (_versioningManager != null)
                {
                    try
                    {
                        // Extract user ID and comment from provenance info if available
                        string userId = null;
                        string comment = null;
                        
                        if (triple.ProvenanceInfo.ContainsKey("ChangedByUserId"))
                        {
                            userId = triple.ProvenanceInfo["ChangedByUserId"].ToString();
                        }
                        
                        if (triple.ProvenanceInfo.ContainsKey("ChangeComment"))
                        {
                            comment = triple.ProvenanceInfo["ChangeComment"].ToString();
                        }
                        
                        // Record deletion in version history
                        await _versioningManager.RecordVersionAsync(
                            triple, 
                            ChangeType.Deletion, 
                            tenantId,
                            userId,
                            comment,
                            cancellationToken);
                            
                        _logger.LogDebug(
                            "Recorded version for triple {TripleId} with change type Deletion",
                            tripleId);
                    }
                    catch (Exception versionEx)
                    {
                        _logger.LogWarning(
                            versionEx,
                            "Failed to record version for deleted triple {TripleId}: {ErrorMessage}",
                            tripleId,
                            versionEx.Message);
                        // Continue despite versioning failure
                    }
                }
                
                // Remove from cache
                _tripleCache.Remove(tripleId);
                
                _logger.LogDebug(
                    "Removed triple {TripleId} from graph {GraphUri} for tenant {TenantId}",
                    tripleId,
                    triple.GraphUri,
                    tenantId);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing triple {TripleId} for tenant {TenantId}: {ErrorMessage}",
                    tripleId,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Updates an existing triple in the store
        /// </summary>
        /// <param name="triple">The triple with updated values</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateTripleAsync(
            Models.Triple triple, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            if (triple == null)
                throw new ArgumentNullException(nameof(triple));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Check if the triple exists and belongs to the tenant
                if (!_tripleCache.TryGetValue(triple.Id, out var existingTriple) || existingTriple.TenantId != tenantId)
                {
                    _logger.LogWarning(
                        "Triple {TripleId} not found or doesn't belong to tenant {TenantId}",
                        triple.Id,
                        tenantId);
                    return false;
                }
                
                // Store a copy of the existing triple before removing it
                var originalTriple = new Models.Triple
                {
                    Id = existingTriple.Id,
                    TenantId = existingTriple.TenantId,
                    SubjectId = existingTriple.SubjectId,
                    PredicateUri = existingTriple.PredicateUri,
                    ObjectId = existingTriple.ObjectId,
                    IsLiteral = existingTriple.IsLiteral,
                    LiteralDataType = existingTriple.LiteralDataType,
                    LanguageTag = existingTriple.LanguageTag,
                    GraphUri = existingTriple.GraphUri,
                    ConfidenceScore = existingTriple.ConfidenceScore,
                    CreatedAt = existingTriple.CreatedAt,
                    UpdatedAt = existingTriple.UpdatedAt,
                    SourceDocumentId = existingTriple.SourceDocumentId,
                    IsVerified = existingTriple.IsVerified,
                    Version = existingTriple.Version
                };
                
                // Copy provenance info
                foreach (var item in existingTriple.ProvenanceInfo)
                {
                    originalTriple.ProvenanceInfo[item.Key] = item.Value;
                }
                
                // Remove the existing triple
                bool removed = await RemoveTripleAsync(triple.Id, tenantId, cancellationToken);
                if (!removed)
                {
                    return false;
                }
                
                // Set the version and updated timestamp
                triple.Version = existingTriple.Version + 1;
                triple.UpdatedAt = DateTime.UtcNow;
                
                // Add the updated triple
                bool added = await AddTripleAsync(triple, tenantId, cancellationToken);
                
                if (added && _versioningManager != null)
                {
                    try
                    {
                        // Extract user ID and comment from provenance info if available
                        string userId = null;
                        string comment = null;
                        
                        if (triple.ProvenanceInfo.ContainsKey("ChangedByUserId"))
                        {
                            userId = triple.ProvenanceInfo["ChangedByUserId"].ToString();
                        }
                        
                        if (triple.ProvenanceInfo.ContainsKey("ChangeComment"))
                        {
                            comment = triple.ProvenanceInfo["ChangeComment"].ToString();
                        }
                        
                        // Record update in version history
                        await _versioningManager.RecordVersionAsync(
                            triple, 
                            ChangeType.Update, 
                            tenantId,
                            userId,
                            comment,
                            cancellationToken);
                            
                        _logger.LogDebug(
                            "Recorded version for triple {TripleId} with change type Update",
                            triple.Id);
                    }
                    catch (Exception versionEx)
                    {
                        _logger.LogWarning(
                            versionEx,
                            "Failed to record version for updated triple {TripleId}: {ErrorMessage}",
                            triple.Id,
                            versionEx.Message);
                        // Continue despite versioning failure
                    }
                }
                
                if (added)
                {
                    _logger.LogDebug(
                        "Updated triple {TripleId} for tenant {TenantId} to version {Version}",
                        triple.Id,
                        tenantId,
                        triple.Version);
                }
                
                return added;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating triple {TripleId} for tenant {TenantId}: {ErrorMessage}",
                    triple.Id,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Queries the triple store with various filter options
        /// </summary>
        /// <param name="query">The triple query parameters</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query results</returns>
        public async Task<TripleQueryResult> QueryAsync(
            TripleQuery query, 
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
                
            try
            {
                var sw = Stopwatch.StartNew();
                
                var result = new TripleQueryResult
                {
                    Query = query
                };
                
                // Filter triples based on the query parameters
                var filteredTriples = _tripleCache.Values
                    .Where(t => 
                        (string.IsNullOrEmpty(tenantId) || t.TenantId == tenantId) &&
                        (string.IsNullOrEmpty(query.SubjectId) || t.SubjectId == query.SubjectId) &&
                        (string.IsNullOrEmpty(query.PredicateUri) || t.PredicateUri == query.PredicateUri) &&
                        (string.IsNullOrEmpty(query.ObjectId) || t.ObjectId == query.ObjectId) &&
                        (string.IsNullOrEmpty(query.GraphUri) || t.GraphUri == query.GraphUri) &&
                        (!query.MinConfidenceScore.HasValue || t.ConfidenceScore >= query.MinConfidenceScore.Value) &&
                        (!query.IsVerified.HasValue || t.IsVerified == query.IsVerified.Value) &&
                        (string.IsNullOrEmpty(query.SourceDocumentId) || t.SourceDocumentId == query.SourceDocumentId) &&
                        (!query.CreatedAfter.HasValue || t.CreatedAt >= query.CreatedAfter.Value) &&
                        (!query.CreatedBefore.HasValue || t.CreatedAt <= query.CreatedBefore.Value));
                
                result.TotalCount = filteredTriples.Count();
                
                // Apply sorting
                var orderedTriples = query.SortAscending
                    ? OrderTriplesByProperty(filteredTriples, query.SortBy, true)
                    : OrderTriplesByProperty(filteredTriples, query.SortBy, false);
                
                // Apply pagination
                result.Triples = orderedTriples
                    .Skip(query.Offset)
                    .Take(query.Limit)
                    .ToList();
                
                result.HasMore = result.TotalCount > (query.Offset + query.Limit);
                
                sw.Stop();
                
                // Add metadata
                result.Metadata["QueryTimeMs"] = sw.ElapsedMilliseconds;
                result.Metadata["ExecutedAt"] = DateTime.UtcNow;
                
                _logger.LogDebug(
                    "Query returned {Count} of {Total} triples for tenant {TenantId} in {ElapsedMs}ms",
                    result.Triples.Count,
                    result.TotalCount,
                    tenantId,
                    sw.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error querying triples for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                
                return new TripleQueryResult
                {
                    Triples = new List<Models.Triple>(),
                    TotalCount = 0,
                    HasMore = false,
                    Query = query,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message,
                        ["ExecutedAt"] = DateTime.UtcNow
                    }
                };
            }
        }
        
        /// <summary>
        /// Orders triples by the specified property
        /// </summary>
        private IEnumerable<Models.Triple> OrderTriplesByProperty(IEnumerable<Models.Triple> triples, string propertyName, bool ascending)
        {
            switch (propertyName.ToLowerInvariant())
            {
                case "createdat":
                    return ascending ? triples.OrderBy(t => t.CreatedAt) : triples.OrderByDescending(t => t.CreatedAt);
                case "updatedat":
                    return ascending ? triples.OrderBy(t => t.UpdatedAt) : triples.OrderByDescending(t => t.UpdatedAt);
                case "confidencescore":
                    return ascending ? triples.OrderBy(t => t.ConfidenceScore) : triples.OrderByDescending(t => t.ConfidenceScore);
                case "subjectid":
                    return ascending ? triples.OrderBy(t => t.SubjectId) : triples.OrderByDescending(t => t.SubjectId);
                case "predicateuri":
                    return ascending ? triples.OrderBy(t => t.PredicateUri) : triples.OrderByDescending(t => t.PredicateUri);
                case "objectid":
                    return ascending ? triples.OrderBy(t => t.ObjectId) : triples.OrderByDescending(t => t.ObjectId);
                case "id":
                    return ascending ? triples.OrderBy(t => t.Id) : triples.OrderByDescending(t => t.Id);
                case "version":
                    return ascending ? triples.OrderBy(t => t.Version) : triples.OrderByDescending(t => t.Version);
                default:
                    return ascending ? triples.OrderBy(t => t.CreatedAt) : triples.OrderByDescending(t => t.CreatedAt);
            }
        }
        
        /// <summary>
        /// Executes a SPARQL query on the triple store
        /// </summary>
        /// <param name="sparqlQuery">The SPARQL query string</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query results as a SparqlResultSet or RDF graph</returns>
        public async Task<object> ExecuteSparqlQueryAsync(
            string sparqlQuery,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sparqlQuery))
                throw new ArgumentNullException(nameof(sparqlQuery));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Filter store to only include graphs for this tenant
                var tenantGraphs = _graphCache
                    .Where(kvp => kvp.Key.StartsWith($"{tenantId}:"))
                    .Select(kvp => kvp.Value)
                    .ToList();
                
                if (!tenantGraphs.Any())
                {
                    _logger.LogWarning(
                        "No graphs found for tenant {TenantId}",
                        tenantId);
                    return null;
                }
                
                // Create a tenant-specific store
                var tenantStore = new VDS.RDF.TripleStore();
                foreach (var graph in tenantGraphs)
                {
                    tenantStore.Add(graph);
                }
                
                // Parse and execute the SPARQL query
                var parser = new SparqlQueryParser();
                var parsedQuery = parser.ParseFromString(sparqlQuery);
                
                // Set query timeout
                parsedQuery.Timeout = _options.QueryTimeoutSeconds * 1000;
                
                var processor = new LeviathanQueryProcessor(tenantStore);
                var results = processor.ProcessQuery(parsedQuery);
                
                _logger.LogDebug(
                    "Executed SPARQL query for tenant {TenantId}",
                    tenantId);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing SPARQL query for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                
                return null;
            }
        }
        
        /// <summary>
        /// Creates a named graph in the triple store
        /// </summary>
        /// <param name="graphUri">The URI of the graph to create</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CreateGraphAsync(
            string graphUri,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(graphUri))
                throw new ArgumentNullException(nameof(graphUri));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Get or create the graph - reusing our existing method
                await GetOrCreateGraphAsync(graphUri, tenantId);
                
                _logger.LogInformation(
                    "Created graph {GraphUri} for tenant {TenantId}",
                    graphUri,
                    tenantId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating graph {GraphUri} for tenant {TenantId}: {ErrorMessage}",
                    graphUri,
                    tenantId,
                    ex.Message);
                
                return false;
            }
        }
        
        /// <summary>
        /// Removes a named graph and all its triples from the store
        /// </summary>
        /// <param name="graphUri">The URI of the graph to remove</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RemoveGraphAsync(
            string graphUri,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(graphUri))
                throw new ArgumentNullException(nameof(graphUri));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Create cache key
                var cacheKey = $"{tenantId}:{graphUri}";
                
                if (!_graphCache.TryGetValue(cacheKey, out var graph))
                {
                    _logger.LogWarning(
                        "Graph {GraphUri} not found for tenant {TenantId}",
                        graphUri,
                        tenantId);
                    return false;
                }
                
                // Remove from the RDF store
                _store.Remove(graph.BaseUri);
                
                // Remove from cache
                _graphCache.Remove(cacheKey);
                
                // Remove all triples in this graph from the cache
                var triplesToRemove = _tripleCache.Values
                    .Where(t => t.TenantId == tenantId && t.GraphUri == graphUri)
                    .Select(t => t.Id)
                    .ToList();
                
                foreach (var id in triplesToRemove)
                {
                    _tripleCache.Remove(id);
                }
                
                _logger.LogInformation(
                    "Removed graph {GraphUri} with {Count} triples for tenant {TenantId}",
                    graphUri,
                    triplesToRemove.Count,
                    tenantId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing graph {GraphUri} for tenant {TenantId}: {ErrorMessage}",
                    graphUri,
                    tenantId,
                    ex.Message);
                
                return false;
            }
        }
        
        /// <summary>
        /// Gets statistics about the triple store
        /// </summary>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of statistics</returns>
        public async Task<Dictionary<string, object>> GetStoreStatisticsAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Get all tenant graphs
                var tenantGraphs = _graphCache
                    .Where(kvp => kvp.Key.StartsWith($"{tenantId}:"))
                    .Select(kvp => kvp.Value)
                    .ToList();
                
                // Get all tenant triples
                var tenantTriples = _tripleCache.Values
                    .Where(t => t.TenantId == tenantId)
                    .ToList();
                
                // Calculate statistics
                var stats = new Dictionary<string, object>
                {
                    ["GraphCount"] = tenantGraphs.Count,
                    ["TripleCount"] = tenantTriples.Count,
                    ["SubjectCount"] = tenantTriples.Select(t => t.SubjectId).Distinct().Count(),
                    ["PredicateCount"] = tenantTriples.Select(t => t.PredicateUri).Distinct().Count(),
                    ["ObjectCount"] = tenantTriples.Select(t => t.ObjectId).Distinct().Count(),
                    ["LiteralCount"] = tenantTriples.Count(t => t.IsLiteral),
                    ["VerifiedCount"] = tenantTriples.Count(t => t.IsVerified),
                    ["AvgConfidence"] = tenantTriples.Any() ? tenantTriples.Average(t => t.ConfidenceScore) : 0.0,
                    ["LastUpdated"] = tenantTriples.Any() ? tenantTriples.Max(t => t.UpdatedAt) : DateTime.MinValue
                };
                
                _logger.LogDebug(
                    "Retrieved statistics for tenant {TenantId}: {TripleCount} triples in {GraphCount} graphs",
                    tenantId,
                    stats["TripleCount"],
                    stats["GraphCount"]);
                
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting store statistics for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                
                return new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                };
            }
        }
    }
} 