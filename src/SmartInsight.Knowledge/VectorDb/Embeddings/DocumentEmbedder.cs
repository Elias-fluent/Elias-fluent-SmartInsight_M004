using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Service for embedding document content into vector representations
    /// </summary>
    public class DocumentEmbedder
    {
        private readonly ILogger<DocumentEmbedder> _logger;
        private readonly IEmbeddingGenerator _embeddingGenerator;
        private readonly ITextChunker _textChunker;
        private readonly EmbeddingOptions _options;
        private readonly QdrantClientService _qdrantClient;
        
        public DocumentEmbedder(
            ILogger<DocumentEmbedder> logger,
            IEmbeddingGenerator embeddingGenerator,
            ITextChunker textChunker,
            IOptions<EmbeddingOptions> options,
            QdrantClientService qdrantClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
            _textChunker = textChunker ?? throw new ArgumentNullException(nameof(textChunker));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _qdrantClient = qdrantClient ?? throw new ArgumentNullException(nameof(qdrantClient));
        }
        
        /// <summary>
        /// Processes a document into vector embeddings and stores them in the vector database
        /// </summary>
        /// <param name="documentId">Unique identifier for the document</param>
        /// <param name="documentText">Content of the document</param>
        /// <param name="documentTitle">Title of the document</param>
        /// <param name="metadata">Additional metadata for the document</param>
        /// <param name="tenantId">Tenant ID for multi-tenant isolation</param>
        /// <param name="collectionName">Vector database collection name (defaults to DocumentCollection from options)</param>
        /// <param name="chunkSize">Maximum chunk size for splitting document</param>
        /// <param name="overlap">Overlap between chunks</param>
        /// <param name="modelName">Embedding model name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of chunks processed and stored</returns>
        public async Task<int> ProcessDocumentAsync(
            string documentId,
            string documentText,
            string documentTitle,
            Dictionary<string, object> metadata = null,
            string tenantId = null,
            string collectionName = null,
            int chunkSize = 0,
            int overlap = 0,
            string modelName = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("Document ID must be provided", nameof(documentId));
                
            if (string.IsNullOrWhiteSpace(documentText))
                throw new ArgumentException("Document text cannot be empty", nameof(documentText));
                
            collectionName ??= _options.DocumentCollection;
            chunkSize = chunkSize > 0 ? chunkSize : _options.DefaultChunkSize;
            overlap = overlap >= 0 ? overlap : _options.DefaultChunkOverlap;
            metadata ??= new Dictionary<string, object>();
            
            try
            {
                // 1. Ensure the collection exists
                await EnsureCollectionExistsAsync(collectionName, modelName, tenantId, cancellationToken);
                
                // 2. Split document into chunks
                _logger.LogInformation("Splitting document {DocumentId} into chunks", documentId);
                var chunks = _textChunker.ChunkDocument(documentText, documentTitle, chunkSize, overlap);
                
                if (chunks.Count == 0)
                {
                    _logger.LogWarning("Document {DocumentId} produced no chunks", documentId);
                    return 0;
                }
                
                _logger.LogInformation("Document {DocumentId} split into {ChunkCount} chunks", 
                    documentId, chunks.Count);
                
                // 3. Add document metadata to each chunk
                foreach (var chunk in chunks)
                {
                    // Add standard metadata
                    chunk.SourceId = documentId;
                    chunk.Metadata["document_id"] = documentId;
                    chunk.Metadata["document_title"] = documentTitle;
                    chunk.Metadata["chunk_index"] = chunk.Position;
                    chunk.Metadata["created_at"] = DateTime.UtcNow;
                    
                    // Add tenant ID if provided
                    if (!string.IsNullOrWhiteSpace(tenantId))
                    {
                        chunk.Metadata["tenant_id"] = tenantId;
                    }
                    
                    // Add custom metadata
                    if (metadata != null)
                    {
                        foreach (var kvp in metadata)
                        {
                            if (!chunk.Metadata.ContainsKey(kvp.Key))
                            {
                                chunk.Metadata[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                
                // 4. Generate embeddings for all chunks
                _logger.LogInformation("Generating embeddings for {ChunkCount} chunks using model {ModelName}", 
                    chunks.Count, modelName ?? _options.DefaultModel);
                
                var texts = chunks.Select(c => c.Text).ToList();
                var embeddings = await _embeddingGenerator.GenerateBatchEmbeddingsAsync(
                    texts, modelName, tenantId, cancellationToken);
                
                if (embeddings.Count != chunks.Count)
                {
                    _logger.LogError("Embedding count mismatch: {EmbeddingCount} embeddings for {ChunkCount} chunks",
                        embeddings.Count, chunks.Count);
                    throw new InvalidOperationException("Embedding count mismatch");
                }
                
                // 5. Store embeddings in vector database
                _logger.LogInformation("Storing {EmbeddingCount} embeddings in collection {CollectionName}", 
                    embeddings.Count, collectionName);
                
                var vectorMap = new Dictionary<string, float[]>();
                var payloadMap = new Dictionary<string, Dictionary<string, object>>();
                
                for (int i = 0; i < chunks.Count; i++)
                {
                    // Create unique ID for each chunk
                    var pointId = $"{documentId}_{i}";
                    
                    // Add embedding vector
                    vectorMap[pointId] = embeddings[i];
                    
                    // Create payload with chunk text and metadata
                    var payload = new Dictionary<string, object>(chunks[i].Metadata)
                    {
                        ["text"] = chunks[i].Text,
                        ["section"] = chunks[i].Section ?? documentTitle
                    };
                    
                    // Always add tenant ID for isolation if provided
                    if (!string.IsNullOrWhiteSpace(tenantId) && !payload.ContainsKey("tenant_id"))
                    {
                        payload["tenant_id"] = tenantId;
                    }
                    
                    payloadMap[pointId] = payload;
                }
                
                // Store in vector database
                var insertedCount = await _qdrantClient.UpsertVectorsAsync(
                    collectionName, 
                    vectorMap, 
                    payloadMap, 
                    tenantId, 
                    cancellationToken);
                
                if (insertedCount <= 0)
                {
                    throw new InvalidOperationException($"Failed to store document chunks in vector database for document {documentId}");
                }
                
                _logger.LogInformation("Successfully stored {ChunkCount} document chunks in vector database", 
                    insertedCount);
                
                return chunks.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}: {ErrorMessage}", 
                    documentId, ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Creates the vector database collection if it doesn't exist
        /// </summary>
        private async Task EnsureCollectionExistsAsync(
            string collectionName, 
            string modelName, 
            string tenantId,
            CancellationToken cancellationToken)
        {
            try
            {
                // Determine vector size from the model
                var vectorSize = await _embeddingGenerator.GetEmbeddingDimensionAsync(modelName);
                if (vectorSize <= 0)
                {
                    throw new InvalidOperationException($"Invalid vector dimension {vectorSize} for model {modelName ?? _options.DefaultModel}");
                }
                
                // Ensure the collection exists
                var collectionCreated = await _qdrantClient.EnsureCollectionExistsAsync(
                    collectionName, 
                    vectorSize, 
                    cancellationToken);
                
                if (collectionCreated)
                {
                    _logger.LogInformation("Created collection {CollectionName} with vector size {VectorSize}", 
                        collectionName, vectorSize);
                }
                else
                {
                    _logger.LogDebug("Collection {CollectionName} already exists", collectionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring collection {CollectionName} exists", collectionName);
                throw;
            }
        }
        
        /// <summary>
        /// Searches for similar document chunks based on a query text
        /// </summary>
        /// <param name="queryText">The text to search for</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <param name="tenantId">Tenant ID for multi-tenant isolation</param>
        /// <param name="collectionName">Vector database collection name</param>
        /// <param name="modelName">Embedding model name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of search results with scores and payloads</returns>
        public async Task<List<DocumentSearchResult>> SearchSimilarAsync(
            string queryText,
            int limit = 10,
            string tenantId = null,
            string collectionName = null,
            string modelName = null,
            CancellationToken cancellationToken = default)
        {
            collectionName ??= _options.DocumentCollection;
            
            try
            {
                // Generate embedding for query text
                var queryVector = await _embeddingGenerator.GenerateEmbeddingAsync(
                    queryText, modelName, tenantId, cancellationToken);
                
                // Perform vector search
                var searchResults = await _qdrantClient.SearchAsync(
                    collectionName,
                    queryVector,
                    limit,
                    tenantId,
                    null,
                    cancellationToken);
                
                // Map to document search results
                return searchResults.Select(r => new DocumentSearchResult
                {
                    Id = r.Id,
                    Score = r.Score,
                    Text = r.Payload.TryGetValue("text", out var text) ? text?.ToString() : null,
                    DocumentId = r.Payload.TryGetValue("document_id", out var docId) ? docId?.ToString() : null,
                    DocumentTitle = r.Payload.TryGetValue("document_title", out var title) ? title?.ToString() : null,
                    Section = r.Payload.TryGetValue("section", out var section) ? section?.ToString() : null,
                    ChunkIndex = r.Payload.TryGetValue("chunk_index", out var index) && index is int idx ? idx : 
                               (r.Payload.TryGetValue("chunk_index", out var strIdx) && int.TryParse(strIdx?.ToString(), out var parsedIdx) ? parsedIdx : 0),
                    Payload = r.Payload
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for similar documents: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a document and all its chunks from the vector database
        /// </summary>
        /// <param name="documentId">ID of document to delete</param>
        /// <param name="tenantId">Tenant ID for multi-tenant isolation</param>
        /// <param name="collectionName">Vector database collection name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of chunks deleted</returns>
        public async Task<int> DeleteDocumentAsync(
            string documentId,
            string tenantId = null,
            string collectionName = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("Document ID must be provided", nameof(documentId));
                
            collectionName ??= _options.DocumentCollection;
            
            try
            {
                // Create filter for document deletion
                var filterDocIds = new[] { documentId };
                
                // Delete all points for this document
                var deletedCount = await _qdrantClient.DeletePointsAsync(
                    collectionName,
                    filterDocIds, 
                    tenantId, 
                    cancellationToken);
                
                _logger.LogInformation("Deleted {DeletedCount} document chunks for document {DocumentId}", 
                    deletedCount, documentId);
                
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}: {ErrorMessage}", 
                    documentId, ex.Message);
                throw;
            }
        }
    }
    
    /// <summary>
    /// Result object for document similarity searches
    /// </summary>
    public class DocumentSearchResult
    {
        /// <summary>
        /// ID of the point in the vector database
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Similarity score (higher is more similar)
        /// </summary>
        public float Score { get; set; }
        
        /// <summary>
        /// Text content of the chunk
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// ID of the source document
        /// </summary>
        public string DocumentId { get; set; }
        
        /// <summary>
        /// Title of the source document
        /// </summary>
        public string DocumentTitle { get; set; }
        
        /// <summary>
        /// Section name within the document
        /// </summary>
        public string Section { get; set; }
        
        /// <summary>
        /// Position of this chunk within the document
        /// </summary>
        public int ChunkIndex { get; set; }
        
        /// <summary>
        /// Full payload from the vector database
        /// </summary>
        public Dictionary<string, object> Payload { get; set; }
    }
} 