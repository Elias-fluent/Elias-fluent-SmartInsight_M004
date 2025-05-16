using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartInsight.Knowledge.VectorDb.Embeddings;

namespace SmartInsight.Knowledge.VectorDb
{
    /// <summary>
    /// Qdrant vector database client service with tenant isolation
    /// </summary>
    public class QdrantClientService : IDisposable
    {
        private readonly ILogger<QdrantClientService> _logger;
        private readonly QdrantClient _client;
        private readonly QdrantOptions _options;
        private readonly SemaphoreSlim _collectionCreationLock = new SemaphoreSlim(1, 1);
        private readonly HashSet<string> _existingCollections = new HashSet<string>();

        public QdrantClientService(
            IOptions<QdrantOptions> options,
            ILogger<QdrantClientService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            // Create client with the proper URI constructor
            var uri = new Uri($"{(_options.UseHttps ? "https" : "http")}://{_options.Host}:{_options.GrpcPort}");
            _client = new QdrantClient(uri);
            
            _logger.LogInformation("Initialized Qdrant client for {Host}:{Port}", _options.Host, _options.GrpcPort);
        }

        /// <summary>
        /// Checks if a collection exists in Qdrant
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the collection exists</returns>
        public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check the cache first
                if (_existingCollections.Contains(collectionName))
                {
                    return true;
                }
                
                var collections = await _client.ListCollectionsAsync(cancellationToken);
                var exists = collections.Contains(collectionName);
                
                // Add to cache if it exists
                if (exists)
                {
                    _existingCollections.Add(collectionName);
                }
                
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if collection {CollectionName} exists", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Creates a new collection with tenant isolation support
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="vectorSize">Vector dimension size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        public async Task<bool> CreateCollectionAsync(
            string collectionName, 
            int vectorSize, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _collectionCreationLock.WaitAsync(cancellationToken);
                
                try
                {
                    // Check if collection already exists
                    if (await CollectionExistsAsync(collectionName, cancellationToken))
                    {
                        _logger.LogInformation("Collection {CollectionName} already exists", collectionName);
                        return true;
                    }
                    
                    // Create the collection with correct parameter types
                    await _client.CreateCollectionAsync(
                        collectionName,
                        new VectorParams
                        {
                            Size = (ulong)vectorSize,
                            Distance = Distance.Cosine
                        },
                        cancellationToken: cancellationToken);
                    
                    // Add payload index for tenant_id to support efficient filtering
                    await _client.CreatePayloadIndexAsync(
                        collectionName,
                        "tenant_id",
                        PayloadSchemaType.Keyword,
                        cancellationToken: cancellationToken);
                    
                    // Add payload index for document_id
                    await _client.CreatePayloadIndexAsync(
                        collectionName,
                        "document_id",
                        PayloadSchemaType.Keyword,
                        cancellationToken: cancellationToken);
                    
                    // Add to cache
                    _existingCollections.Add(collectionName);
                    
                    _logger.LogInformation("Created collection {CollectionName} with vector size {VectorSize}", 
                        collectionName, vectorSize);
                    
                    return true;
                }
                finally
                {
                    _collectionCreationLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collection {CollectionName}", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Search for vectors in a collection with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="queryVector">Query vector</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="scoreThreshold">Minimum similarity score</param>
        /// <param name="filter">Additional filter conditions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of search results with similarity scores</returns>
        public async Task<List<SearchResult>> SearchAsync(
            string collectionName,
            float[] queryVector,
            string tenantId,
            int limit = 10,
            float scoreThreshold = 0.7f,
            Dictionary<string, object> filter = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build the filter for tenant isolation and additional conditions
                var queryFilter = new Filter();
                
                // Add tenant filter for isolation
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    queryFilter.Must.Add(new Condition 
                    { 
                        Field = new FieldCondition 
                        { 
                            Key = "tenant_id", 
                            Match = new Match 
                            { 
                                Keyword = tenantId 
                            } 
                        } 
                    });
                }
                
                // Add additional filter conditions if provided
                if (filter != null && filter.Count > 0)
                {
                    foreach (var item in filter)
                    {
                        // Handle different types of values
                        if (item.Value is string stringValue)
                        {
                            queryFilter.Must.Add(new Condition 
                            { 
                                Field = new FieldCondition 
                                { 
                                    Key = item.Key, 
                                    Match = new Match 
                                    { 
                                        Keyword = stringValue 
                                    } 
                                } 
                            });
                        }
                        else if (item.Value is int intValue)
                        {
                            queryFilter.Must.Add(new Condition 
                            { 
                                Field = new FieldCondition 
                                { 
                                    Key = item.Key, 
                                    Match = new Match 
                                    { 
                                        Integer = intValue 
                                    } 
                                } 
                            });
                        }
                        else
                        {
                            // Convert other types to string
                            queryFilter.Must.Add(new Condition 
                            { 
                                Field = new FieldCondition 
                                { 
                                    Key = item.Key, 
                                    Match = new Match 
                                    { 
                                        Keyword = item.Value.ToString() 
                                    } 
                                } 
                            });
                        }
                    }
                }
                
                // Perform search using standard search method
                var finalFilter = queryFilter.Must.Count > 0 ? queryFilter : null;
                var result = await _client.SearchAsync(
                    collectionName,
                    queryVector,
                    limit: (uint)limit,
                    filter: finalFilter,
                    scoreThreshold: scoreThreshold,
                    cancellationToken: cancellationToken);
                
                // Convert to our SearchResult type
                return result.Select(r => new SearchResult
                {
                    Id = r.Id.ToString(),
                    Score = r.Score,
                    Payload = ConvertToPayloadDictionary(r.Payload)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching vectors in Qdrant collection {CollectionName} for tenant {TenantId}", 
                    collectionName, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Convert Qdrant payload to a simpler Dictionary format
        /// </summary>
        private Dictionary<string, object> ConvertToPayloadDictionary(
            IReadOnlyDictionary<string, Value> payload)
        {
            var result = new Dictionary<string, object>();
            
            if (payload == null)
                return result;
                
            foreach (var pair in payload)
            {
                // Handle different value types from Qdrant
                if (pair.Value.StringValue != null && !string.IsNullOrEmpty(pair.Value.StringValue))
                {
                    result[pair.Key] = pair.Value.StringValue;
                }
                else if (pair.Value.IntegerValue != 0)
                {
                    result[pair.Key] = pair.Value.IntegerValue;
                }
                else if (pair.Value.DoubleValue != 0)
                {
                    result[pair.Key] = pair.Value.DoubleValue;
                }
                else if (pair.Value.BoolValue)
                {
                    result[pair.Key] = true;
                }
                else
                {
                    // Default fallback or null value
                    result[pair.Key] = null;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Perform similarity search based on raw text (generates embedding automatically)
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="queryText">Text to search for</param>
        /// <param name="embeddingGenerator">Embedding generator to convert text to vector</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="scoreThreshold">Minimum similarity score</param>
        /// <param name="filter">Additional filter conditions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results</returns>
        public async Task<List<SearchResult>> SearchByTextAsync(
            string collectionName,
            string queryText,
            IEmbeddingGenerator embeddingGenerator,
            string tenantId,
            int limit = 10,
            float scoreThreshold = 0.7f,
            Dictionary<string, object> filter = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Generate embedding vector for the query text
                var queryVector = await embeddingGenerator.GenerateEmbeddingAsync(
                    queryText, tenantId: tenantId, cancellationToken: cancellationToken);
                
                // Perform vector search
                return await SearchAsync(
                    collectionName,
                    queryVector,
                    tenantId,
                    limit,
                    scoreThreshold,
                    filter,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing text-based search in collection {CollectionName}", collectionName);
                throw;
            }
        }
        
        /// <summary>
        /// Insert or update points in a collection with explicit IDs
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="pointIds">Point IDs</param>
        /// <param name="vectors">Vector values</param>
        /// <param name="payloads">Optional payload data for each point</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpsertPointsAsync(
            string collectionName,
            List<string> pointIds,
            List<float[]> vectors,
            List<Dictionary<string, object>> payloads = null,
            CancellationToken cancellationToken = default)
        {
            if (pointIds == null || vectors == null || pointIds.Count != vectors.Count)
            {
                throw new ArgumentException("Point IDs and vectors must have the same count");
            }
            
            if (payloads != null && payloads.Count != pointIds.Count)
            {
                throw new ArgumentException("Payloads count must match point IDs count");
            }
            
            try
            {
                var points = new List<PointStruct>();
                
                for (int i = 0; i < pointIds.Count; i++)
                {
                    // Create point ID based on whether it's a number or guid
                    var pointId = pointIds[i];
                    PointStruct point = new PointStruct();
                    
                    // Try to parse as ulong
                    if (ulong.TryParse(pointId, out var ulongId))
                    {
                        point.Id = ulongId;
                    }
                    else if (Guid.TryParse(pointId, out var guidId))
                    {
                        // This is a Guid
                        point.Id = new PointId { Uuid = guidId.ToString() };
                    }
                    else
                    {
                        // This is a string ID
                        point.Id = new PointId { Uuid = pointId };
                    }
                    
                    // Set the vector
                    point.Vectors = vectors[i];
                    
                    // Add payload if provided
                    if (payloads != null && payloads[i] != null)
                    {
                        foreach (var kvp in payloads[i])
                        {
                            // Convert each value type appropriately
                            if (kvp.Value == null)
                            {
                                // Skip null values or set them explicitly if needed
                                continue;
                            }
                            else if (kvp.Value is string stringValue)
                            {
                                point.Payload[kvp.Key] = stringValue;
                            }
                            else if (kvp.Value is int intValue)
                            {
                                point.Payload[kvp.Key] = intValue;
                            }
                            else if (kvp.Value is bool boolValue)
                            {
                                point.Payload[kvp.Key] = boolValue;
                            }
                            else if (kvp.Value is double doubleValue)
                            {
                                point.Payload[kvp.Key] = doubleValue;
                            }
                            else if (kvp.Value is float floatValue)
                            {
                                point.Payload[kvp.Key] = (double)floatValue;
                            }
                            else
                            {
                                // Default to string for other types
                                point.Payload[kvp.Key] = kvp.Value.ToString();
                            }
                        }
                    }
                    
                    points.Add(point);
                }
                
                // Process in batches to avoid overwhelming the API
                var batchSize = _options.BatchSize;
                for (int i = 0; i < points.Count; i += batchSize)
                {
                    var batch = points.Skip(i).Take(batchSize).ToList();
                    
                    // Upsert points batch
                    await _client.UpsertAsync(
                        collectionName,
                        batch,
                        cancellationToken: cancellationToken);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting points to collection {CollectionName}", collectionName);
                return false;
            }
        }

        /// <summary>
        /// Delete points from a collection by IDs with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="ids">Point IDs to delete</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of points deleted</returns>
        public async Task<int> DeletePointsAsync(
            string collectionName,
            IEnumerable<string> ids,
            string tenantId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build filter for tenant isolation
                Filter filter = null;
                
                // Add tenant filter for isolation if provided
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    filter = new Filter();
                    filter.Must.Add(new Condition 
                    { 
                        Field = new FieldCondition 
                        { 
                            Key = "tenant_id", 
                            Match = new Match 
                            { 
                                Keyword = tenantId 
                            } 
                        } 
                    });
                }
                
                // Delete points individually 
                int totalDeleted = 0;
                foreach (var id in ids)
                {
                    try
                    {
                        // Create a filter for this ID
                        var idFilter = new Filter();
                        
                        // Try to parse as ulong
                        if (ulong.TryParse(id, out var ulongId))
                        {
                            var pointId = new PointId { Num = ulongId };
                            idFilter.Must.Add(new Condition 
                            { 
                                HasId = new HasIdCondition 
                                { 
                                    HasId = { pointId } 
                                } 
                            });
                        }
                        else if (Guid.TryParse(id, out var guidId))
                        {
                            // This is a GUID - use HasId condition with the GUID
                            var pointId = new PointId { Uuid = guidId.ToString() };
                            idFilter.Must.Add(new Condition 
                            { 
                                HasId = new HasIdCondition 
                                { 
                                    HasId = { pointId } 
                                } 
                            });
                        }
                        else
                        {
                            // This is a string ID - use HasId condition with the string
                            var pointId = new PointId { Uuid = id };
                            idFilter.Must.Add(new Condition 
                            { 
                                HasId = new HasIdCondition 
                                { 
                                    HasId = { pointId } 
                                } 
                            });
                        }
                        
                        // Combine with tenant filter if needed
                        if (filter != null && filter.Must.Count > 0)
                        {
                            idFilter.Must.AddRange(filter.Must);
                        }
                        
                        // Use filter-based deletion for all ID types for consistency
                        await _client.DeleteAsync(
                            collectionName,
                            filter: idFilter,
                            cancellationToken: cancellationToken);
                        
                        totalDeleted++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting point {PointId} from collection {CollectionName}", 
                            id, collectionName);
                    }
                }
                
                return totalDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting points from collection {CollectionName}", collectionName);
                return 0;
            }
        }
        
        /// <summary>
        /// Delete points from a collection by filter
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="filter">Filter to select points to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of points deleted</returns>
        public async Task<int> DeletePointsByFilterAsync(
            string collectionName,
            Filter filter,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Delete by filter
                await _client.DeleteAsync(
                    collectionName,
                    filter: filter,
                    cancellationToken: cancellationToken);
                
                // Note: the Qdrant API doesn't return the exact count of deleted items
                // when deleting by filter, so we return 1 to indicate success
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting points by filter from collection {CollectionName}", collectionName);
                return 0;
            }
        }

        /// <summary>
        /// Delete all points that belong to a specific document
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="documentId">Document ID</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of points deleted</returns>
        public async Task<int> DeleteDocumentAsync(
            string collectionName,
            string documentId,
            string tenantId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = new Filter();
                
                // Filter by document ID
                filter.Must.Add(new Condition 
                { 
                    Field = new FieldCondition 
                    { 
                        Key = "document_id", 
                        Match = new Match 
                        { 
                            Keyword = documentId 
                        } 
                    } 
                });
                
                // Add tenant filter for isolation if provided
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    filter.Must.Add(new Condition 
                    { 
                        Field = new FieldCondition 
                        { 
                            Key = "tenant_id", 
                            Match = new Match 
                            { 
                                Keyword = tenantId 
                            } 
                        } 
                    });
                }
                
                // Delete by filter
                await _client.DeleteAsync(
                    collectionName,
                    filter: filter,
                    cancellationToken: cancellationToken);
                
                // Note: the Qdrant API doesn't return the exact count of deleted items
                // when deleting by filter, so we return 1 to indicate success
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId} from collection {CollectionName}", 
                    documentId, collectionName);
                return 0;
            }
        }
        
        /// <summary>
        /// Count points in a collection with optional filtering
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="filter">Optional filter to count specific points</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of points</returns>
        public async Task<long> CountPointsAsync(
            string collectionName,
            Filter filter = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _client.CountAsync(
                    collectionName,
                    filter: filter,
                    exact: true,
                    cancellationToken: cancellationToken);
                
                // Return count directly, which is a ulong that implicitly converts to long
                return (long)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting points in collection {CollectionName}", collectionName);
                return 0;
            }
        }
        
        /// <summary>
        /// Get collections information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of collection names</returns>
        public async Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var collections = await _client.ListCollectionsAsync(cancellationToken);
                return collections.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing collections");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get collection info including vector count and configuration
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection info or null if not found</returns>
        public async Task<CollectionInfo> GetCollectionInfoAsync(
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var info = await _client.GetCollectionInfoAsync(
                    collectionName, 
                    cancellationToken);
                
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting info for collection {CollectionName}", collectionName);
                return null;
            }
        }
        
        /// <summary>
        /// Execute any operation with retry logic
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <returns>Result of the operation</returns>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
        {
            int retries = 0;
            TimeSpan delay = TimeSpan.FromMilliseconds(100);
            
            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    retries++;
                    
                    if (retries >= _options.MaxRetries)
                    {
                        _logger.LogError(ex, "Operation failed after {RetryCount} retries", retries);
                        throw;
                    }
                    
                    _logger.LogWarning(ex, "Operation failed, retrying ({RetryCount}/{MaxRetries}) after {Delay}ms", 
                        retries, _options.MaxRetries, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay);
                    
                    // Exponential backoff with jitter
                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(
                            _options.MaxRetryDelayMs,
                            delay.TotalMilliseconds * 2 * (0.8 + new Random().NextDouble() * 0.4)
                        )
                    );
                }
            }
        }
        
        public void Dispose()
        {
            _client?.Dispose();
            _collectionCreationLock?.Dispose();
        }
    }

    /// <summary>
    /// Configuration options for Qdrant client
    /// </summary>
    public class QdrantOptions
    {
        /// <summary>
        /// Qdrant server host
        /// </summary>
        public string Host { get; set; } = "localhost";
        
        /// <summary>
        /// Qdrant HTTP port
        /// </summary>
        public int HttpPort { get; set; } = 6333;
        
        /// <summary>
        /// Qdrant gRPC port
        /// </summary>
        public int GrpcPort { get; set; } = 6334;
        
        /// <summary>
        /// Whether to use HTTPS
        /// </summary>
        public bool UseHttps { get; set; } = false;
        
        /// <summary>
        /// API key for Qdrant server (if enabled)
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// Maximum number of retries for operations
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// Maximum retry delay in milliseconds
        /// </summary>
        public double MaxRetryDelayMs { get; set; } = 5000;
        
        /// <summary>
        /// Batch size for bulk operations
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// Search result from Qdrant
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Point ID
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// Similarity score
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Point payload data
        /// </summary>
        public Dictionary<string, object>? Payload { get; set; }
    }
} 