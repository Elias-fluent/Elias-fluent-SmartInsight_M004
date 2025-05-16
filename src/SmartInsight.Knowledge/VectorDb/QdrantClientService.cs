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
using Qdrant.Client.Grpc.Collections;
using Qdrant.Client.Grpc.Points;
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
            
            // Create client with proper constructor
            var uri = new Uri($"http://{_options.Host}:{_options.GrpcPort}");
            if (_options.UseHttps)
            {
                uri = new Uri($"https://{_options.Host}:{_options.GrpcPort}");
            }
            
            _client = new QdrantClient(uri, _options.ApiKey);
            
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
                    
                    // Create the collection with correct parameters
                    var createParams = new CreateCollectionParams
                    {
                        VectorsConfig = new VectorsConfig
                        {
                            Params = new VectorParams
                            {
                                Size = (ulong)vectorSize,
                                Distance = Distance.Cosine
                            }
                        }
                    };
                    
                    await _client.CreateCollectionAsync(collectionName, createParams, cancellationToken);
                    
                    // Add payload index for tenant_id to support efficient filtering
                    var tenantIdIndexParams = new PayloadIndexParams
                    {
                        FieldName = "tenant_id",
                        SchemaType = PayloadSchemaType.Keyword
                    };
                    await _client.CreatePayloadIndexAsync(collectionName, tenantIdIndexParams, cancellationToken);
                    
                    // Add payload index for document_id
                    var documentIdIndexParams = new PayloadIndexParams
                    {
                        FieldName = "document_id",
                        SchemaType = PayloadSchemaType.Keyword
                    };
                    await _client.CreatePayloadIndexAsync(collectionName, documentIdIndexParams, cancellationToken);
                    
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
                // Build the filter
                var filterBuilder = new Filter();
                
                // Add tenant filter for isolation
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    var tenantCondition = new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "tenant_id",
                            Match = new Match
                            {
                                Keyword = tenantId
                            }
                        }
                    };
                    filter.Must.Add(tenantCondition);
                }
                
                // Add additional filter conditions if provided
                if (filter != null && filter.Count > 0)
                {
                    foreach (var item in filter)
                    {
                        Condition condition;
                        
                        // Handle different types of values
                        if (item.Value is string stringValue)
                        {
                            condition = new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = item.Key,
                                    Match = new Match
                                    {
                                        Keyword = stringValue
                                    }
                                }
                            };
                        }
                        else if (item.Value is int intValue)
                        {
                            condition = new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = item.Key,
                                    Match = new Match
                                    {
                                        Integer = intValue
                                    }
                                }
                            };
                        }
                        else
                        {
                            // Convert other types to string
                            condition = new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = item.Key,
                                    Match = new Match
                                    {
                                        Keyword = item.Value.ToString()
                                    }
                                }
                            };
                        }
                        
                        filterBuilder.Must.Add(condition);
                    }
                }
                
                // Prepare search request
                var searchRequest = new SearchPointsRequest
                {
                    CollectionName = collectionName,
                    Vector = queryVector,
                    Filter = filterBuilder,
                    Limit = (uint)limit,
                    WithPayload = true,
                    ScoreThreshold = scoreThreshold
                };
                
                // Perform search with retry
                var result = await ExecuteWithRetryAsync(() => 
                    _client.SearchPointsAsync(searchRequest, cancellationToken));
                
                // Process results
                return result.Result.Select(r => new SearchResult
                {
                    Id = r.Id.Uuid,
                    Score = r.Score,
                    Payload = ConvertQdrantPayloadToDictionary(r.Payload)
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
        private Dictionary<string, object> ConvertQdrantPayloadToDictionary(Dictionary<string, Value> payload)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var pair in payload)
            {
                object value;
                
                if (pair.Value.NullValue != null)
                    value = null;
                else if (pair.Value.StringValue != null)
                    value = pair.Value.StringValue;
                else if (pair.Value.IntegerValue != 0)
                    value = pair.Value.IntegerValue;
                else if (pair.Value.DoubleValue != 0)
                    value = pair.Value.DoubleValue;
                else if (pair.Value.BoolValue)
                    value = pair.Value.BoolValue;
                else if (pair.Value.ListValue != null)
                    value = pair.Value.ListValue;
                else if (pair.Value.StructValue != null)
                    value = pair.Value.StructValue;
                else
                    value = pair.Value.ToString();
                
                result[pair.Key] = value;
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
                    var point = new PointStruct
                    {
                        Id = new PointId { Uuid = pointIds[i] },
                        Vectors = new Vectors { Vector = new Vector { Data = { vectors[i] } } }
                    };
                    
                    if (payloads != null && payloads[i] != null)
                    {
                        foreach (var kvp in payloads[i])
                        {
                            var valueToAdd = ConvertToQdrantValue(kvp.Value);
                            point.Payload[kvp.Key] = valueToAdd;
                        }
                    }
                    
                    points.Add(point);
                }
                
                // Process in batches to avoid overwhelming the API
                var batchSize = _options.BatchSize;
                for (int i = 0; i < points.Count; i += batchSize)
                {
                    var batch = points.Skip(i).Take(batchSize).ToList();
                    
                    // Create upsert request
                    var upsertRequest = new UpsertPointsRequest
                    {
                        CollectionName = collectionName,
                        Points = { batch }
                    };
                    
                    // Use retry logic
                    await ExecuteWithRetryAsync(() => 
                        _client.UpsertPointsAsync(upsertRequest, cancellationToken));
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
        /// Convert a C# object to a Qdrant payload Value
        /// </summary>
        private Value ConvertToQdrantValue(object value)
        {
            var qdrantValue = new Value();
            
            if (value == null)
            {
                qdrantValue.NullValue = NullValue.NullValue;
            }
            else if (value is string stringValue)
            {
                qdrantValue.StringValue = stringValue;
            }
            else if (value is int intValue)
            {
                qdrantValue.IntegerValue = intValue;
            }
            else if (value is long longValue)
            {
                qdrantValue.IntegerValue = longValue;
            }
            else if (value is float floatValue)
            {
                qdrantValue.DoubleValue = floatValue;
            }
            else if (value is double doubleValue)
            {
                qdrantValue.DoubleValue = doubleValue;
            }
            else if (value is bool boolValue)
            {
                qdrantValue.BoolValue = boolValue;
            }
            else if (value is DateTime dateTime)
            {
                qdrantValue.StringValue = dateTime.ToString("o");
            }
            else
            {
                qdrantValue.StringValue = value.ToString();
            }
            
            return qdrantValue;
        }
        
        /// <summary>
        /// Delete points from a collection by IDs with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="pointIds">Point IDs to delete</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of points deleted</returns>
        public async Task<int> DeletePointsAsync(
            string collectionName,
            IEnumerable<string> pointIds,
            string tenantId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = new Filter();
                
                // Add tenant filter for isolation if provided
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    var tenantCondition = new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "tenant_id",
                            Match = new Match
                            {
                                Keyword = tenantId
                            }
                        }
                    };
                    filter.Must.Add(tenantCondition);
                }
                
                // Convert string IDs to PointId objects
                var ids = pointIds.Select(id => new PointId { Uuid = id }).ToList();
                
                // Create delete points request
                var deleteRequest = new DeletePointsRequest
                {
                    CollectionName = collectionName,
                    Points = new PointsSelector
                    {
                        Points = new PointsIdsList { Ids = { ids } }
                    }
                };
                
                // Add filter if tenant ID is specified
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    deleteRequest.Points.Filter = filter;
                }
                
                // Perform deletion with retry
                var result = await ExecuteWithRetryAsync(() => 
                    _client.DeletePointsAsync(deleteRequest, cancellationToken));
                
                return (int)result.Status.OperationResult;
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
                // Create delete points request
                var deleteRequest = new DeletePointsRequest
                {
                    CollectionName = collectionName,
                    Points = new PointsSelector
                    {
                        Filter = filter
                    }
                };
                
                // Perform deletion with retry
                var result = await ExecuteWithRetryAsync(() => 
                    _client.DeletePointsAsync(deleteRequest, cancellationToken));
                
                return (int)result.Status.OperationResult;
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
                var docCondition = new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "document_id",
                        Match = new Match
                        {
                            Keyword = documentId
                        }
                    }
                };
                filter.Must.Add(docCondition);
                
                // Add tenant filter for isolation if provided
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    var tenantCondition = new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "tenant_id",
                            Match = new Match
                            {
                                Keyword = tenantId
                            }
                        }
                    };
                    filter.Must.Add(tenantCondition);
                }
                
                // Create delete points request
                var deleteRequest = new DeletePointsRequest
                {
                    CollectionName = collectionName,
                    Points = new PointsSelector
                    {
                        Filter = filter
                    }
                };
                
                // Perform deletion with retry
                var result = await ExecuteWithRetryAsync(() => 
                    _client.DeletePointsAsync(deleteRequest, cancellationToken));
                
                return (int)result.Status.OperationResult;
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
                var countRequest = new CountPointsRequest
                {
                    CollectionName = collectionName,
                    Filter = filter
                };
                
                var result = await ExecuteWithRetryAsync(() => 
                    _client.CountPointsAsync(countRequest, cancellationToken));
                
                return (long)result.Result.Count;
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
                var request = new ListCollectionsRequest();
                var response = await ExecuteWithRetryAsync(() => 
                    _client.ListCollectionsAsync(request, cancellationToken));
                
                return response.Collections.Select(c => c.Name).ToList();
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
                var request = new GetCollectionInfoRequest
                {
                    CollectionName = collectionName
                };
                
                var response = await ExecuteWithRetryAsync(() => 
                    _client.GetCollectionInfoAsync(request, cancellationToken));
                
                return response.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting info for collection {CollectionName}", collectionName);
                return null;
            }
        }
        
        /// <summary>
        /// Execute a function with retry logic
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="func">Function to execute</param>
        /// <returns>Result of the function</returns>
        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> func)
        {
            int retries = 0;
            TimeSpan delay = TimeSpan.FromMilliseconds(100);
            
            while (true)
            {
                try
                {
                    return await func();
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