using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Interface for generating vector embeddings used for semantic search
    /// </summary>
    public interface IEmbeddingGenerator
    {
        /// <summary>
        /// Generates an embedding vector for a single text input
        /// </summary>
        /// <param name="text">Text to embed</param>
        /// <param name="modelName">Optional model name (if not specified, uses default)</param>
        /// <param name="tenantId">Optional tenant ID for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A float array representing the embedding vector</returns>
        Task<float[]> GenerateEmbeddingAsync(
            string text, 
            string? modelName = null, 
            string? tenantId = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Generates embedding vectors for multiple text inputs
        /// </summary>
        /// <param name="texts">List of texts to embed</param>
        /// <param name="modelName">Optional model name (if not specified, uses default)</param>
        /// <param name="tenantId">Optional tenant ID for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of float arrays representing the embedding vectors</returns>
        Task<List<float[]>> GenerateBatchEmbeddingsAsync(
            List<string> texts, 
            string? modelName = null, 
            string? tenantId = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the embedding dimension size for a given model
        /// </summary>
        /// <param name="modelName">Optional model name (if not specified, uses default)</param>
        /// <returns>The dimension size of the embeddings</returns>
        Task<int> GetEmbeddingDimensionAsync(string? modelName = null);
    }
} 