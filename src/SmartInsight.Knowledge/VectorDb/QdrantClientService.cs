using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartInsight.Knowledge.VectorDb
{
    /// <summary>
    /// Qdrant vector database client service with tenant isolation
    /// </summary>
    public class QdrantClientService : IVectorDbService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QdrantClientService> _logger;
        private readonly string _endpoint;
        private readonly JsonSerializerOptions _jsonOptions;

        public QdrantClientService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<QdrantClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _endpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Search for vectors in a collection with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="queryVector">Query vector</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="scoreThreshold">Minimum similarity score</param>
        /// <returns>List of search results with similarity scores</returns>
        public async Task<List<SearchResult>> SearchAsync(
            string collectionName,
            float[] queryVector,
            string tenantId,
            int limit = 10,
            float scoreThreshold = 0.7f)
        {
            try
            {
                var searchRequest = new
                {
                    vector = queryVector,
                    limit = limit,
                    score_threshold = scoreThreshold,
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
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_endpoint}/collections/{collectionName}/points/search",
                    searchRequest,
                    _jsonOptions);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>();
                
                return result?.Result
                    .Select(r => new SearchResult
                    {
                        Id = r.Id,
                        Score = r.Score,
                        Payload = r.Payload
                    })
                    .ToList() ?? new List<SearchResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching vectors in Qdrant collection {CollectionName} for tenant {TenantId}", 
                    collectionName, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Insert vectors into a collection with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="vectors">Vectors to insert</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpsertVectorsAsync(
            string collectionName,
            List<VectorRecord> vectors,
            string tenantId)
        {
            try
            {
                // Ensure each vector has the tenant_id in its payload for proper isolation
                foreach (var vector in vectors)
                {
                    if (vector.Payload == null)
                    {
                        vector.Payload = new Dictionary<string, object>();
                    }
                    
                    vector.Payload["tenant_id"] = tenantId;
                }

                var points = vectors.Select(v => new
                {
                    id = v.Id,
                    vector = v.Vector,
                    payload = v.Payload
                }).ToArray();

                var request = new { points };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_endpoint}/collections/{collectionName}/points",
                    request,
                    _jsonOptions);

                response.EnsureSuccessStatusCode();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting vectors to Qdrant collection {CollectionName} for tenant {TenantId}", 
                    collectionName, tenantId);
                return false;
            }
        }

        /// <summary>
        /// Delete vectors from a collection with tenant isolation
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="ids">Vector IDs to delete</param>
        /// <param name="tenantId">Tenant ID for isolation</param>
        /// <returns>True if successful</returns>
        public async Task<bool> DeleteVectorsAsync(
            string collectionName,
            IEnumerable<string> ids,
            string tenantId)
        {
            try
            {
                // The filter ensures we only delete vectors belonging to the specified tenant
                var request = new
                {
                    points = ids.ToArray(),
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
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_endpoint}/collections/{collectionName}/points/delete",
                    content);

                response.EnsureSuccessStatusCode();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vectors from Qdrant collection {CollectionName} for tenant {TenantId}", 
                    collectionName, tenantId);
                return false;
            }
        }

        /// <summary>
        /// Create a new collection with tenant isolation support
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="vectorSize">Vector dimension size</param>
        /// <returns>True if successful</returns>
        public async Task<bool> CreateCollectionAsync(string collectionName, int vectorSize)
        {
            try
            {
                var request = new
                {
                    vectors = new
                    {
                        size = vectorSize,
                        distance = "Cosine"
                    }
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_endpoint}/collections/{collectionName}",
                    request,
                    _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create collection {CollectionName}: {Error}", collectionName, error);
                    return false;
                }

                // Add payload index for tenant_id to support efficient filtering
                var indexRequest = new
                {
                    field_name = "tenant_id",
                    field_schema = "keyword"
                };

                var indexResponse = await _httpClient.PutAsJsonAsync(
                    $"{_endpoint}/collections/{collectionName}/index",
                    indexRequest,
                    _jsonOptions);

                return indexResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Qdrant collection {CollectionName}", collectionName);
                return false;
            }
        }
    }

    /// <summary>
    /// Interface for vector database operations
    /// </summary>
    public interface IVectorDbService
    {
        Task<List<SearchResult>> SearchAsync(
            string collectionName,
            float[] queryVector,
            string tenantId,
            int limit = 10,
            float scoreThreshold = 0.7f);

        Task<bool> UpsertVectorsAsync(
            string collectionName,
            List<VectorRecord> vectors,
            string tenantId);

        Task<bool> DeleteVectorsAsync(
            string collectionName,
            IEnumerable<string> ids,
            string tenantId);

        Task<bool> CreateCollectionAsync(
            string collectionName,
            int vectorSize);
    }

    /// <summary>
    /// Vector record to be stored in Qdrant
    /// </summary>
    public class VectorRecord
    {
        public string Id { get; set; } = null!;
        public float[] Vector { get; set; } = null!;
        public Dictionary<string, object>? Payload { get; set; }
    }

    /// <summary>
    /// Search result from Qdrant
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = null!;
        public float Score { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
    }

    /// <summary>
    /// Response model for Qdrant search API
    /// </summary>
    internal class QdrantSearchResponse
    {
        [JsonPropertyName("result")]
        public List<QdrantSearchResult> Result { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public double Time { get; set; }
    }

    /// <summary>
    /// Single result item from Qdrant search API
    /// </summary>
    internal class QdrantSearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }
    }
} 