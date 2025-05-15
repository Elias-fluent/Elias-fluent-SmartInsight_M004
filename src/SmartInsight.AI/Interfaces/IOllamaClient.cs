using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for Ollama API client.
    /// </summary>
    public interface IOllamaClient
    {
        #region Model Management

        /// <summary>
        /// Gets a list of available models.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of model information.</returns>
        Task<List<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a model exists.
        /// </summary>
        /// <param name="modelName">The name of the model to check.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the model exists, false otherwise.</returns>
        Task<bool> ModelExistsAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a model.
        /// </summary>
        /// <param name="modelName">The name of the model to get information about.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Information about the model.</returns>
        Task<OllamaModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pulls a model from the Ollama registry.
        /// </summary>
        /// <param name="modelName">The name of the model to pull.</param>
        /// <param name="insecure">Whether to allow insecure connections.</param>
        /// <param name="progressCallback">A callback to receive progress updates.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when the model has been pulled.</returns>
        Task PullModelAsync(
            string modelName, 
            bool insecure = false, 
            Action<OllamaModelStatusResponse>? progressCallback = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new model from a Modelfile.
        /// </summary>
        /// <param name="modelName">The name to give the model.</param>
        /// <param name="modelfileContent">The content of the Modelfile.</param>
        /// <param name="path">The path to build the model from.</param>
        /// <param name="progressCallback">A callback to receive progress updates.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when the model has been created.</returns>
        Task CreateModelAsync(
            string modelName, 
            string modelfileContent, 
            string? path = null, 
            Action<OllamaModelStatusResponse>? progressCallback = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a model.
        /// </summary>
        /// <param name="modelName">The name of the model to delete.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when the model has been deleted.</returns>
        Task DeleteModelAsync(string modelName, CancellationToken cancellationToken = default);

        #endregion

        #region Completion

        /// <summary>
        /// Generates a completion for a prompt using the default model.
        /// </summary>
        /// <param name="prompt">The prompt to generate a completion for.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The completion response.</returns>
        Task<OllamaCompletionResponse> GenerateCompletionAsync(
            string prompt, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a completion for a prompt using a specified model.
        /// </summary>
        /// <param name="modelName">The name of the model to use.</param>
        /// <param name="prompt">The prompt to generate a completion for.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The completion response.</returns>
        Task<OllamaCompletionResponse> GenerateCompletionWithModelAsync(
            string modelName, 
            string prompt, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a completion for a prompt with streaming responses.
        /// </summary>
        /// <param name="prompt">The prompt to generate a completion for.</param>
        /// <param name="streamingCallback">A callback function to receive streaming responses.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when all streaming responses have been received.</returns>
        Task StreamCompletionAsync(
            string prompt, 
            Action<OllamaCompletionResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a completion for a prompt with streaming responses using a specified model.
        /// </summary>
        /// <param name="modelName">The name of the model to use.</param>
        /// <param name="prompt">The prompt to generate a completion for.</param>
        /// <param name="streamingCallback">A callback function to receive streaming responses.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when all streaming responses have been received.</returns>
        Task StreamCompletionWithModelAsync(
            string modelName, 
            string prompt, 
            Action<OllamaCompletionResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        #endregion

        #region Chat

        /// <summary>
        /// Generates a chat completion for a list of messages using the default model.
        /// </summary>
        /// <param name="messages">The chat messages to generate a completion for.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The chat completion response.</returns>
        Task<OllamaChatResponse> GenerateChatCompletionAsync(
            List<OllamaChatMessage> messages, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a chat completion for a list of messages using a specified model.
        /// </summary>
        /// <param name="modelName">The name of the model to use.</param>
        /// <param name="messages">The chat messages to generate a completion for.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The chat completion response.</returns>
        Task<OllamaChatResponse> GenerateChatCompletionWithModelAsync(
            string modelName, 
            List<OllamaChatMessage> messages, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a chat completion for a list of messages with streaming responses.
        /// </summary>
        /// <param name="messages">The chat messages to generate a completion for.</param>
        /// <param name="streamingCallback">A callback function to receive streaming responses.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when all streaming responses have been received.</returns>
        Task StreamChatCompletionAsync(
            List<OllamaChatMessage> messages, 
            Action<OllamaChatResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a chat completion for a list of messages with streaming responses using a specified model.
        /// </summary>
        /// <param name="modelName">The name of the model to use.</param>
        /// <param name="messages">The chat messages to generate a completion for.</param>
        /// <param name="streamingCallback">A callback function to receive streaming responses.</param>
        /// <param name="parameters">Optional parameters to control generation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when all streaming responses have been received.</returns>
        Task StreamChatCompletionWithModelAsync(
            string modelName, 
            List<OllamaChatMessage> messages, 
            Action<OllamaChatResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default);

        #endregion
    }
} 