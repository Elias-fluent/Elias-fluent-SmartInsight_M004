using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Core.Models;

namespace SmartInsight.Core.Interfaces
{
    /// <summary>
    /// Interface for client interactions with Ollama server
    /// </summary>
    public interface IOllamaClient
    {
        /// <summary>
        /// Generates an embedding vector for a single text input using the specified model
        /// </summary>
        /// <param name="modelName">Name of the model to use</param>
        /// <param name="text">Text to embed</param>
        /// <param name="parameters">Optional additional parameters for the model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The embedding response containing the vector</returns>
        Task<EmbeddingResponse> GenerateEmbeddingWithModelAsync(
            string modelName, 
            string text,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Generates embedding vectors for multiple text inputs using the specified model
        /// </summary>
        /// <param name="modelName">Name of the model to use</param>
        /// <param name="texts">List of texts to embed</param>
        /// <param name="parameters">Optional additional parameters for the model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of embedding responses containing the vectors</returns>
        Task<List<EmbeddingResponse>> GenerateBatchEmbeddingsWithModelAsync(
            string modelName,
            List<string> texts,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);
    }
} 