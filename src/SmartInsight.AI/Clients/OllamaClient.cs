using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Exceptions;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.Options;

namespace SmartInsight.AI.Clients
{
    /// <summary>
    /// Client for interacting with the Ollama API.
    /// </summary>
    public class OllamaClient : IOllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;
        private readonly ILogger<OllamaClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for requests.</param>
        /// <param name="options">The options for the client.</param>
        /// <param name="logger">The logger to use.</param>
        public OllamaClient(
            HttpClient httpClient,
            IOptions<OllamaOptions> options,
            ILogger<OllamaClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure HTTP client
            if (!string.Equals(_httpClient.BaseAddress?.ToString(), _options.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds);

            // Configure JSON serialization
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        #region API Request Helpers

        /// <summary>
        /// Sends a request to the Ollama API with retry logic.
        /// </summary>
        private async Task<T> SendRequestWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> requestFunc,
            string operationName,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < _options.MaxRetryAttempts)
            {
                try
                {
                    attempt++;
                    return await requestFunc(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (
                    ex is HttpRequestException ||
                    ex is TaskCanceledException ||
                    ex is TimeoutException ||
                    ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    
                    if (attempt >= _options.MaxRetryAttempts)
                    {
                        _logger.LogError(ex, "Failed to {OperationName} after {Attempts} attempts", 
                            operationName, attempt);
                        break;
                    }

                    _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} to {OperationName} failed. Retrying in {RetryDelay}ms", 
                        attempt, _options.MaxRetryAttempts, operationName, _options.RetryDelayMilliseconds);
                    
                    await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }
            }

            if (lastException is TimeoutException or TaskCanceledException)
            {
                throw OllamaException.CreateTimeoutException(operationName, lastException);
            }
            else if (lastException is HttpRequestException httpEx)
            {
                throw OllamaException.CreateConnectionException(httpEx);
            }
            else if (lastException != null)
            {
                throw new OllamaException($"Failed to {operationName} after {attempt} attempts.", lastException);
            }
            else
            {
                throw new OllamaException($"Failed to {operationName} after {attempt} attempts.");
            }
        }

        /// <summary>
        /// Creates a content object for a JSON request with optional parameters.
        /// </summary>
        private StringContent CreateJsonContent<T>(T request, Dictionary<string, object>? additionalParams = null)
            where T : class
        {
            // If we have additional params and the object supports them via JsonExtensionData
            if (additionalParams != null && request is OllamaRequestBase requestWithOptions)
            {
                // Add the additional params via reflection to the JsonExtensionData property
                var optionsProperty = request.GetType().GetProperty("Options");
                if (optionsProperty != null)
                {
                    var options = optionsProperty.GetValue(request) as Dictionary<string, object> ?? new Dictionary<string, object>();
                    foreach (var param in additionalParams)
                    {
                        options[param.Key] = param.Value;
                    }
                    optionsProperty.SetValue(request, options);
                }
            }

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Attempts to execute a fallback strategy when the primary model fails.
        /// </summary>
        private async Task<T> TryWithFallbackAsync<T>(
            Func<string, Task<T>> operation,
            string primaryModel,
            CancellationToken cancellationToken)
        {
            try
            {
                return await operation(primaryModel).ConfigureAwait(false);
            }
            catch (OllamaException ex) when (_options.EnableFallback && 
                                             !string.IsNullOrEmpty(_options.FallbackModel) && 
                                             _options.FallbackModel != primaryModel)
            {
                _logger.LogWarning(ex, "Primary model {PrimaryModel} failed. Falling back to {FallbackModel}", 
                    primaryModel, _options.FallbackModel);
                
                // Try the fallback model
                try
                {
                    return await operation(_options.FallbackModel).ConfigureAwait(false);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback model {FallbackModel} also failed", _options.FallbackModel);
                    throw new OllamaException(
                        $"Both primary model '{primaryModel}' and fallback model '{_options.FallbackModel}' failed.",
                        fallbackEx);
                }
            }
        }

        #endregion

        #region Model Management

        /// <inheritdoc />
        public async Task<List<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return await SendRequestWithRetryAsync(async (token) =>
            {
                _logger.LogDebug("Listing available models");
                
                var response = await _httpClient.GetAsync("tags", token).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(response, "list models").ConfigureAwait(false);
                
                var result = await response.Content.ReadFromJsonAsync<OllamaModelListResponse>(_jsonOptions, token)
                    .ConfigureAwait(false);
                
                if (result == null)
                {
                    throw new OllamaException("Failed to deserialize model list response");
                }
                
                _logger.LogDebug("Found {Count} models", result.Models.Count);
                return result.Models;
            }, "list models", cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> ModelExistsAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }

            try
            {
                var models = await ListModelsAsync(cancellationToken).ConfigureAwait(false);
                return models.Any(m => m.Name == modelName);
            }
            catch (OllamaException)
            {
                // If we can't list models, assume the model doesn't exist
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<OllamaModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }

            return await SendRequestWithRetryAsync(async (token) =>
            {
                _logger.LogDebug("Getting information for model {ModelName}", modelName);
                
                var models = await ListModelsAsync(token).ConfigureAwait(false);
                var model = models.FirstOrDefault(m => m.Name == modelName);
                
                if (model == null)
                {
                    throw OllamaException.CreateModelNotFoundException(modelName);
                }
                
                return model;
            }, $"get model info for {modelName}", cancellationToken);
        }

        /// <inheritdoc />
        public async Task PullModelAsync(
            string modelName, 
            bool insecure = false, 
            Action<OllamaModelStatusResponse>? progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }

            await SendRequestWithRetryAsync(async (token) =>
            {
                _logger.LogInformation("Pulling model {ModelName} (insecure: {Insecure})", modelName, insecure);
                
                var request = new OllamaModelPullRequest
                {
                    Name = modelName,
                    Insecure = insecure
                };
                
                var content = CreateJsonContent(request);
                var response = await _httpClient.PostAsync("pull", content, token).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(response, $"pull model {modelName}").ConfigureAwait(false);
                
                // Process progress updates if requested
                if (progressCallback != null)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var reader = new StreamReader(stream);
                    
                    while (!reader.EndOfStream && !token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(line)) continue;
                        
                        try
                        {
                            var statusResponse = JsonSerializer.Deserialize<OllamaModelStatusResponse>(line, _jsonOptions);
                            if (statusResponse != null)
                            {
                                progressCallback(statusResponse);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse pull status update: {Line}", line);
                        }
                    }
                }
                
                _logger.LogInformation("Successfully pulled model {ModelName}", modelName);
                return true;
            }, $"pull model {modelName}", cancellationToken);
        }

        /// <inheritdoc />
        public async Task CreateModelAsync(
            string modelName, 
            string modelfileContent, 
            string? path = null, 
            Action<OllamaModelStatusResponse>? progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }
            
            if (string.IsNullOrWhiteSpace(modelfileContent))
            {
                throw new ArgumentException("Modelfile content cannot be empty", nameof(modelfileContent));
            }

            await SendRequestWithRetryAsync(async (token) =>
            {
                _logger.LogInformation("Creating model {ModelName}", modelName);
                
                var request = new OllamaModelCreateRequest
                {
                    Name = modelName,
                    Modelfile = modelfileContent,
                    Path = path
                };
                
                var content = CreateJsonContent(request);
                var response = await _httpClient.PostAsync("create", content, token).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(response, $"create model {modelName}").ConfigureAwait(false);
                
                // Process progress updates if requested
                if (progressCallback != null)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var reader = new StreamReader(stream);
                    
                    while (!reader.EndOfStream && !token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(line)) continue;
                        
                        try
                        {
                            var statusResponse = JsonSerializer.Deserialize<OllamaModelStatusResponse>(line, _jsonOptions);
                            if (statusResponse != null)
                            {
                                progressCallback(statusResponse);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse create status update: {Line}", line);
                        }
                    }
                }
                
                _logger.LogInformation("Successfully created model {ModelName}", modelName);
                return true;
            }, $"create model {modelName}", cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }

            await SendRequestWithRetryAsync(async (token) =>
            {
                _logger.LogInformation("Deleting model {ModelName}", modelName);
                
                var request = new OllamaModelDeleteRequest { Name = modelName };
                var content = CreateJsonContent(request);
                
                // For .NET 8, use HttpMethod.Delete for backward compatibility
                var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "delete")
                {
                    Content = content
                };
                
                var response = await _httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(response, $"delete model {modelName}").ConfigureAwait(false);
                
                _logger.LogInformation("Successfully deleted model {ModelName}", modelName);
                return true;
            }, $"delete model {modelName}", cancellationToken);
        }

        #endregion

        #region Completion Generation

        /// <inheritdoc />
        public async Task<OllamaCompletionResponse> GenerateCompletionAsync(
            string prompt, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            return await GenerateCompletionWithModelAsync(_options.PrimaryModel, prompt, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OllamaCompletionResponse> GenerateCompletionWithModelAsync(
            string modelName, 
            string prompt, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }
            
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
            }

            return await TryWithFallbackAsync(
                async (model) => await SendRequestWithRetryAsync(async (token) =>
                {
                    _logger.LogDebug("Generating completion with model {ModelName}", model);
                    
                    // Apply default parameters, then override with custom ones
                    var allParameters = new Dictionary<string, object>(_options.DefaultParameters);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            allParameters[param.Key] = param.Value;
                        }
                    }
                    
                    // Ensure streaming is disabled
                    allParameters["stream"] = false;
                    
                    var request = new OllamaCompletionRequest
                    {
                        Model = model,
                        Prompt = prompt,
                        Options = allParameters
                    };
                    
                    var content = CreateJsonContent(request);
                    var response = await _httpClient.PostAsync("generate", content, token).ConfigureAwait(false);
                    await EnsureSuccessStatusCodeAsync(response, "generate completion").ConfigureAwait(false);
                    
                    var result = await response.Content.ReadFromJsonAsync<OllamaCompletionResponse>(_jsonOptions, token)
                        .ConfigureAwait(false);
                    
                    if (result == null)
                    {
                        throw new OllamaException("Failed to deserialize completion response");
                    }
                    
                    return result;
                }, $"generate completion with model {model}", cancellationToken),
                modelName,
                cancellationToken
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task StreamCompletionAsync(
            string prompt, 
            Action<OllamaCompletionResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            await StreamCompletionWithModelAsync(_options.PrimaryModel, prompt, streamingCallback, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task StreamCompletionWithModelAsync(
            string modelName, 
            string prompt, 
            Action<OllamaCompletionResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }
            
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
            }
            
            if (streamingCallback == null)
            {
                throw new ArgumentNullException(nameof(streamingCallback));
            }

            await TryWithFallbackAsync(
                async (model) => await SendRequestWithRetryAsync(async (token) =>
                {
                    _logger.LogDebug("Streaming completion with model {ModelName}", model);
                    
                    // Apply default parameters, then override with custom ones
                    var allParameters = new Dictionary<string, object>(_options.DefaultParameters);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            allParameters[param.Key] = param.Value;
                        }
                    }
                    
                    // Ensure streaming is enabled
                    allParameters["stream"] = true;
                    
                    var request = new OllamaCompletionRequest
                    {
                        Model = model,
                        Prompt = prompt,
                        Options = allParameters
                    };
                    
                    var content = CreateJsonContent(request);
                    var response = await _httpClient.PostAsync("generate", content, token).ConfigureAwait(false);
                    await EnsureSuccessStatusCodeAsync(response, "stream completion").ConfigureAwait(false);
                    
                    using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var reader = new StreamReader(stream);
                    
                    while (!reader.EndOfStream && !token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(line)) continue;
                        
                        try
                        {
                            var streamResponse = JsonSerializer.Deserialize<OllamaCompletionResponse>(line, _jsonOptions);
                            if (streamResponse != null)
                            {
                                streamingCallback(streamResponse);
                                
                                if (streamResponse.Done)
                                {
                                    break;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse streaming completion: {Line}", line);
                        }
                    }
                    
                    return true;
                }, $"stream completion with model {model}", cancellationToken),
                modelName,
                cancellationToken
            ).ConfigureAwait(false);
        }

        #endregion

        #region Chat Completion

        /// <inheritdoc />
        public async Task<OllamaChatResponse> GenerateChatCompletionAsync(
            List<OllamaChatMessage> messages, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            return await GenerateChatCompletionWithModelAsync(_options.PrimaryModel, messages, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OllamaChatResponse> GenerateChatCompletionWithModelAsync(
            string modelName, 
            List<OllamaChatMessage> messages, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }
            
            if (messages == null || !messages.Any())
            {
                throw new ArgumentException("Messages cannot be empty", nameof(messages));
            }

            return await TryWithFallbackAsync(
                async (model) => await SendRequestWithRetryAsync(async (token) =>
                {
                    _logger.LogDebug("Generating chat completion with model {ModelName}", model);
                    
                    // Apply default parameters, then override with custom ones
                    var allParameters = new Dictionary<string, object>(_options.DefaultParameters);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            allParameters[param.Key] = param.Value;
                        }
                    }
                    
                    // Ensure streaming is disabled
                    allParameters["stream"] = false;
                    
                    var request = new OllamaChatRequest
                    {
                        Model = model,
                        Messages = messages,
                        Options = allParameters
                    };
                    
                    var content = CreateJsonContent(request);
                    var response = await _httpClient.PostAsync("chat", content, token).ConfigureAwait(false);
                    await EnsureSuccessStatusCodeAsync(response, "generate chat completion").ConfigureAwait(false);
                    
                    var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(_jsonOptions, token)
                        .ConfigureAwait(false);
                    
                    if (result == null)
                    {
                        throw new OllamaException("Failed to deserialize chat completion response");
                    }
                    
                    return result;
                }, $"generate chat completion with model {model}", cancellationToken),
                modelName,
                cancellationToken
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task StreamChatCompletionAsync(
            List<OllamaChatMessage> messages, 
            Action<OllamaChatResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            await StreamChatCompletionWithModelAsync(_options.PrimaryModel, messages, streamingCallback, parameters, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task StreamChatCompletionWithModelAsync(
            string modelName, 
            List<OllamaChatMessage> messages, 
            Action<OllamaChatResponse> streamingCallback, 
            Dictionary<string, object>? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }
            
            if (messages == null || !messages.Any())
            {
                throw new ArgumentException("Messages cannot be empty", nameof(messages));
            }
            
            if (streamingCallback == null)
            {
                throw new ArgumentNullException(nameof(streamingCallback));
            }

            await TryWithFallbackAsync(
                async (model) => await SendRequestWithRetryAsync(async (token) =>
                {
                    _logger.LogDebug("Streaming chat completion with model {ModelName}", model);
                    
                    // Apply default parameters, then override with custom ones
                    var allParameters = new Dictionary<string, object>(_options.DefaultParameters);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            allParameters[param.Key] = param.Value;
                        }
                    }
                    
                    // Ensure streaming is enabled
                    allParameters["stream"] = true;
                    
                    var request = new OllamaChatRequest
                    {
                        Model = model,
                        Messages = messages,
                        Options = allParameters
                    };
                    
                    var content = CreateJsonContent(request);
                    var response = await _httpClient.PostAsync("chat", content, token).ConfigureAwait(false);
                    await EnsureSuccessStatusCodeAsync(response, "stream chat completion").ConfigureAwait(false);
                    
                    using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var reader = new StreamReader(stream);
                    
                    while (!reader.EndOfStream && !token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(line)) continue;
                        
                        try
                        {
                            var streamResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line, _jsonOptions);
                            if (streamResponse != null)
                            {
                                streamingCallback(streamResponse);
                                
                                if (streamResponse.Done)
                                {
                                    break;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse streaming chat completion: {Line}", line);
                        }
                    }
                    
                    return true;
                }, $"stream chat completion with model {model}", cancellationToken),
                modelName,
                cancellationToken
            ).ConfigureAwait(false);
        }

        #endregion

        #region Embeddings

        /// <inheritdoc />
        public async Task<OllamaEmbeddingResponse> GenerateEmbeddingAsync(
            string text,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_options.DefaultModel))
            {
                throw new InvalidOperationException("DefaultModel is not configured in OllamaOptions");
            }

            return await GenerateEmbeddingWithModelAsync(
                _options.DefaultModel, text, parameters, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<OllamaEmbeddingResponse> GenerateEmbeddingWithModelAsync(
            string modelName,
            string text,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await TryWithFallbackAsync(
                async (model) =>
                {
                    return await SendRequestWithRetryAsync(
                        async (token) =>
                        {
                            // Check if model exists
                            var modelExists = await ModelExistsAsync(model, token).ConfigureAwait(false);
                            if (!modelExists)
                            {
                                throw new OllamaException($"Model '{model}' not found");
                            }

                            var request = new OllamaEmbeddingRequest
                            {
                                Model = model,
                                Prompt = text
                            };

                            var content = CreateJsonContent(request, parameters);
                            var response = await _httpClient.PostAsync("embeddings", content, token).ConfigureAwait(false);
                            await EnsureSuccessStatusCodeAsync(response, "generate embedding").ConfigureAwait(false);

                            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(_jsonOptions, token)
                                .ConfigureAwait(false);

                            if (result == null)
                            {
                                throw new OllamaException("Failed to parse embedding response");
                            }

                            _logger.LogDebug("Generated embedding with model {Model}, vector dimension: {Dimension}",
                                result.Model, result.Embedding.Count);

                            return result;
                        },
                        $"generate embedding with model {model}",
                        cancellationToken
                    ).ConfigureAwait(false);
                },
                modelName,
                cancellationToken
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<OllamaEmbeddingResponse>> GenerateBatchEmbeddingsAsync(
            List<string> texts,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_options.DefaultModel))
            {
                throw new InvalidOperationException("DefaultModel is not configured in OllamaOptions");
            }

            return await GenerateBatchEmbeddingsWithModelAsync(
                _options.DefaultModel, texts, parameters, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<List<OllamaEmbeddingResponse>> GenerateBatchEmbeddingsWithModelAsync(
            string modelName,
            List<string> texts,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Count == 0)
            {
                return new List<OllamaEmbeddingResponse>();
            }

            // Process texts in batches to avoid overloading the API
            var batchSize = _options.BatchSize > 0 ? _options.BatchSize : 10;
            var results = new List<OllamaEmbeddingResponse>();
            var tasks = new List<Task<OllamaEmbeddingResponse>>();

            // Process in batches with controlled concurrency
            for (int i = 0; i < texts.Count; i += batchSize)
            {
                var batchTexts = texts.Skip(i).Take(batchSize).ToList();
                tasks.Clear();

                foreach (var text in batchTexts)
                {
                    tasks.Add(GenerateEmbeddingWithModelAsync(modelName, text, parameters, cancellationToken));
                }

                try
                {
                    var batchResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                    results.AddRange(batchResults);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating batch embeddings");
                    throw;
                }

                // Add a short delay between batches to prevent overloading the API
                if (i + batchSize < texts.Count)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            return results;
        }

        #endregion

        #region HTTP Response Helpers

        /// <summary>
        /// Ensures that the HTTP response was successful, and throws an appropriate exception if not.
        /// </summary>
        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Check if this is a model not found error
                if (responseContent.Contains("model not found") || responseContent.Contains("no models found"))
                {
                    // Try to extract the model name from the response
                    string modelName = "unknown";
                    var match = System.Text.RegularExpressions.Regex.Match(responseContent, @"model [""'](.+?)[""'] not found");
                    if (match.Success && match.Groups.Count > 1)
                    {
                        modelName = match.Groups[1].Value;
                    }
                    
                    throw OllamaException.CreateModelNotFoundException(modelName);
                }
            }
            
            throw new OllamaException(
                $"The Ollama API returned an error when attempting to {operation}: {response.StatusCode} - {responseContent}",
                response.StatusCode,
                responseContent);
        }
        
        #endregion
    }
} 