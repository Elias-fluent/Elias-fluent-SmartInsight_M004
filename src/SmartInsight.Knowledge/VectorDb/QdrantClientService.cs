using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Knowledge.VectorDb.Embeddings;

namespace SmartInsight.Knowledge.VectorDb
{
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
        public int Port { get; set; } = 6333;
        
        /// <summary>
        /// Whether to use HTTPS
        /// </summary>
        public bool UseHttps { get; set; } = false;
        
        /// <summary>
        /// API key for Qdrant server (if enabled)
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// Default collection name
        /// </summary>
        public string DefaultCollection { get; set; } = "documents";
        
        /// <summary>
        /// Maximum number of retries for operations
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// Maximum retry delay in milliseconds
        /// </summary>
        public int MaxRetryDelayMs { get; set; } = 5000;
        
        /// <summary>
        /// Batch size for bulk operations
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// Qdrant vector database client service with tenant isolation
    /// </summary>
    public class QdrantClientService : IDisposable
    {
        private readonly ILogger<QdrantClientService> _logger;
        private readonly HttpClient _httpClient;
        private readonly QdrantOptions _options;
        private readonly SemaphoreSlim _collectionCreationLock = new SemaphoreSlim(1, 1);
        private readonly HashSet<string> _existingCollections = new HashSet<string>();
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="QdrantClientService"/> class.
        /// </summary>
        /// <param name="options">Qdrant options</param>
        /// <param name="logger">Logger</param>
        public QdrantClientService(IOptions<QdrantOptions> options, ILogger<QdrantClientService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create HTTP client
            _httpClient = new HttpClient();
            
            // Configure base address
            var protocol = _options.UseHttps ? "https" : "http";
            _httpClient.BaseAddress = new Uri($"{protocol}://{_options.Host}:{_options.Port}");
            
            // Add API key if provided
            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
            }
            
            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _logger.LogInformation("Initialized Qdrant client with host {Host}:{Port}", 
                _options.Host, _options.Port);
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
                
                var response = await _httpClient.GetAsync($"/collections/{collectionName}", cancellationToken);
                var exists = response.IsSuccessStatusCode;
                
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
        /// Ensure collection exists with proper configuration
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="vectorSize">Vector size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection was created, false if it already existed</returns>
        public async Task<bool> EnsureCollectionExistsAsync(
            string collectionName, 
            int vectorSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _collectionCreationLock.WaitAsync(cancellationToken);

                // Check if collection already exists
                if (await CollectionExistsAsync(collectionName, cancellationToken))
                {
                    _logger.LogDebug("Collection {CollectionName} already exists", collectionName);
                    return false;
                }
                
                // Collection doesn't exist, create it
                _logger.LogInformation("Creating collection {CollectionName} with vector size {VectorSize}", 
                    collectionName, vectorSize);

                // Build collection creation payload
                var payload = new
                {
                    vectors = new
                    {
                        size = vectorSize,
                        distance = "Cosine"
                    },
                    optimizers_config = new
                    {
                        indexing_threshold = 0 // Index immediately
                    }
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(payload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                // Create collection
                var response = await _httpClient.PutAsync(
                    $"/collections/{collectionName}", 
                    content, 
                    cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create collection {CollectionName}. Status: {Status}, Error: {Error}",
                        collectionName, response.StatusCode, errorContent);
                    return false;
                }
                
                // Create payload index for tenant_id
                var tenantIndexPayload = new
                {
                    field_name = "tenant_id",
                    field_schema = "keyword"
                };
                
                var tenantIndexContent = new StringContent(
                    JsonSerializer.Serialize(tenantIndexPayload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                await _httpClient.PostAsync(
                    $"/collections/{collectionName}/index", 
                    tenantIndexContent, 
                    cancellationToken);
                
                // Create payload index for document_id
                var docIndexPayload = new
                {
                    field_name = "document_id",
                    field_schema = "keyword"
                };
                
                var docIndexContent = new StringContent(
                    JsonSerializer.Serialize(docIndexPayload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                await _httpClient.PostAsync(
                    $"/collections/{collectionName}/index", 
                    docIndexContent, 
                    cancellationToken);

                // Add to cache
                _existingCollections.Add(collectionName);
                
                _logger.LogInformation("Collection {CollectionName} created successfully", collectionName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating collection {CollectionName}", collectionName);
                return false;
            }
            finally
            {
                _collectionCreationLock.Release();
            }
        }

        /// <summary>
        /// Store vectors with payload in Qdrant with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="vectors">List of vectors to store</param>
        /// <param name="payloads">Dictionary mapping point ID to payload</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of points upserted</returns>
        public async Task<int> UpsertVectorsAsync(
            string collectionName,
            Dictionary<string, float[]> vectors,
            Dictionary<string, Dictionary<string, object>> payloads,
            string tenantId = null,
            CancellationToken cancellationToken = default)
        {
            if (vectors == null || vectors.Count == 0)
            {
                return 0;
            }

            try
            {
                // Ensure collection exists
                await EnsureCollectionExistsAsync(
                    collectionName,
                    vectors.First().Value.Length,
                    cancellationToken);

                // Create points for upsert
                var points = new List<object>();
                foreach (var (id, vector) in vectors)
                {
                    var pointPayload = new Dictionary<string, object>();
                    
                    // Add payload data if provided
                    if (payloads != null && payloads.TryGetValue(id, out var payload))
                    {
                        foreach (var (key, value) in payload)
                        {
                            pointPayload[key] = value;
                        }
                    }
                    
                    // Add tenant_id for isolation if provided
                    if (!string.IsNullOrWhiteSpace(tenantId))
                    {
                        pointPayload["tenant_id"] = tenantId;
                    }
                    
                    // Create point
                    points.Add(new
                    {
                        id = id,
                        vector = vector,
                        payload = pointPayload
                    });
                }
                
                // Process in batches to avoid overwhelming the API
                var batchSize = _options.BatchSize;
                int totalUpserted = 0;
                
                for (int i = 0; i < points.Count; i += batchSize)
                {
                    var batch = points.Skip(i).Take(batchSize).ToList();
                    
                    // Create upsert payload
                    var payload = new
                    {
                        points = batch
                    };
                    
                    var content = new StringContent(
                        JsonSerializer.Serialize(payload, _jsonOptions),
                        Encoding.UTF8,
                        "application/json");
                    
                    // Perform upsert with retry
                    var response = await ExecuteWithRetryAsync(() => 
                        _httpClient.PutAsync($"/collections/{collectionName}/points", content, cancellationToken));
                    
                    if (response.IsSuccessStatusCode)
                    {
                        totalUpserted += batch.Count;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Failed to upsert batch. Status: {Status}, Error: {Error}",
                            response.StatusCode, errorContent);
                    }
                }
                
                _logger.LogInformation("Upserted {PointCount} points to collection {CollectionName}",
                    totalUpserted, collectionName);
                
                return totalUpserted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting vectors to collection {CollectionName}", collectionName);
                throw;
            }
        }

        /// <summary>
        /// Search for similar vectors in a collection with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="queryVector">Query vector</param>
        /// <param name="limit">Max number of results</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="filterDocumentIds">Optional list of document IDs to filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results with scores and payload</returns>
        public async Task<List<SearchResult>> SearchAsync(
            string collectionName,
            float[] queryVector,
            int limit = 10,
            string tenantId = null,
            IEnumerable<string> filterDocumentIds = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build filter for tenant isolation
                object filter = null;
                
                if (!string.IsNullOrWhiteSpace(tenantId) || (filterDocumentIds != null && filterDocumentIds.Any()))
                {
                    var mustConditions = new List<object>();
                    
                    // Add tenant filter
                    if (!string.IsNullOrWhiteSpace(tenantId))
                    {
                        mustConditions.Add(new
                        {
                            key = "tenant_id",
                            match = new { value = tenantId }
                        });
                    }
                    
                    // Add document ID filter
                    if (filterDocumentIds != null && filterDocumentIds.Any())
                    {
                        var docIds = filterDocumentIds.ToArray();
                        if (docIds.Length == 1)
                        {
                            mustConditions.Add(new
                            {
                                key = "document_id",
                                match = new { value = docIds[0] }
                            });
                        }
                        else
                        {
                            var shouldConditions = new List<object>();
                            foreach (var docId in docIds)
                            {
                                shouldConditions.Add(new
                                {
                                    key = "document_id",
                                    match = new { value = docId }
                                });
                            }
                            
                            if (shouldConditions.Count > 0)
                            {
                                mustConditions.Add(new
                                {
                                    should = shouldConditions
                                });
                            }
                        }
                    }
                    
                    if (mustConditions.Count > 0)
                    {
                        filter = new
                        {
                            must = mustConditions
                        };
                    }
                }
                
                // Create search payload
                var payload = new
                {
                    vector = queryVector,
                    limit = limit,
                    filter = filter,
                    with_payload = true
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(payload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                // Perform search
                var response = await _httpClient.PostAsync(
                    $"/collections/{collectionName}/points/search", 
                    content, 
                    cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Search failed. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    return new List<SearchResult>();
                }
                
                // Parse results
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResults = JsonSerializer.Deserialize<QdrantSearchResponse>(responseJson, _jsonOptions);
                
                if (searchResults?.Result == null)
                {
                    return new List<SearchResult>();
                }
                
                // Convert to SearchResult model
                return searchResults.Result.Select(r => new SearchResult
                {
                    Id = r.Id.ToString(),
                    Score = r.Score,
                    Payload = r.Payload
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching vectors in collection {CollectionName}", collectionName);
                throw;
            }
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
                // Build filter for tenant isolation
                object filter = null;
                
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    filter = new
                    {
                        must = new[]
                        {
                            new
                            {
                                key = "tenant_id",
                                match = new { value = tenantId }
                            }
                        }
                    };
                }
                
                // Create delete payload
                var payload = new
                {
                    points = pointIds.ToArray(),
                    filter = filter
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(payload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                // Perform delete
                var response = await _httpClient.PostAsync(
                    $"/collections/{collectionName}/points/delete", 
                    content, 
                    cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Delete failed. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    return 0;
                }
                
                return pointIds.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting points from collection {CollectionName}", collectionName);
                throw;
            }
        }

        /// <summary>
        /// Delete collection
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if collection was deleted</returns>
        public async Task<bool> DeleteCollectionAsync(
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"/collections/{collectionName}",
                    cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete collection {CollectionName}. Status: {Status}, Error: {Error}",
                        collectionName, response.StatusCode, errorContent);
                    return false;
                }
                
                // Remove from cache
                _existingCollections.Remove(collectionName);
                
                _logger.LogInformation("Deleted collection {CollectionName}", collectionName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting collection {CollectionName}", collectionName);
                throw;
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

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _collectionCreationLock?.Dispose();
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Qdrant search response model
    /// </summary>
    public class QdrantSearchResponse
    {
        /// <summary>
        /// Search results
        /// </summary>
        [JsonPropertyName("result")]
        public List<QdrantSearchResultItem> Result { get; set; }
    }

    /// <summary>
    /// Qdrant search result item
    /// </summary>
    public class QdrantSearchResultItem
    {
        /// <summary>
        /// Point ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Similarity score
        /// </summary>
        [JsonPropertyName("score")]
        public float Score { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        [JsonPropertyName("payload")]
        public Dictionary<string, object> Payload { get; set; }
    }

    /// <summary>
    /// Search result from Qdrant
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Point ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Similarity score
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public Dictionary<string, object> Payload { get; set; }
    }
} 