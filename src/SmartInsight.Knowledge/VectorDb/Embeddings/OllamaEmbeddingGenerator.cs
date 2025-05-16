using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Models;

namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Implementation of embedding generator using Ollama
    /// </summary>
    public class OllamaEmbeddingGenerator : IEmbeddingGenerator
    {
        private readonly ILogger<OllamaEmbeddingGenerator> _logger;
        private readonly IOllamaClient _ollamaClient;
        private readonly EmbeddingOptions _options;
        private readonly Dictionary<string, int> _modelDimensions = new Dictionary<string, int>();
        
        public OllamaEmbeddingGenerator(
            ILogger<OllamaEmbeddingGenerator> logger,
            IOllamaClient ollamaClient,
            IOptions<EmbeddingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }
        
        /// <inheritdoc />
        public async Task<float[]> GenerateEmbeddingAsync(
            string text, 
            string? modelName = null, 
            string? tenantId = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Attempt to generate embedding for empty text");
                return Array.Empty<float>();
            }
            
            // Use default model if none specified
            modelName ??= _options.DefaultModel;
            
            // Truncate text if it exceeds max length
            if (text.Length > _options.MaxInputLength)
            {
                _logger.LogWarning("Text for embedding exceeds max length ({Length} > {MaxLength}), truncating",
                    text.Length, _options.MaxInputLength);
                text = text.Substring(0, _options.MaxInputLength);
            }
            
            try
            {
                _logger.LogDebug("Generating embedding with model {ModelName} for text of length {TextLength}",
                    modelName, text.Length);
                
                // Get model-specific parameters if any
                Dictionary<string, object>? parameters = null;
                if (_options.ModelOptions.TryGetValue(modelName, out var modelOptions))
                {
                    parameters = modelOptions;
                }
                
                // Generate embedding
                var response = await _ollamaClient.GenerateEmbeddingWithModelAsync(
                    modelName, 
                    text, 
                    parameters, 
                    cancellationToken);
                
                var embedding = response.Embedding.ToArray();
                
                // Normalize if enabled
                if (_options.NormalizeVectors)
                {
                    embedding = NormalizeVector(embedding);
                }
                
                // Cache model dimension
                if (!_modelDimensions.ContainsKey(modelName))
                {
                    _modelDimensions[modelName] = embedding.Length;
                }
                
                _logger.LogDebug("Generated embedding with dimension {Dimension}", embedding.Length);
                
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding with model {ModelName}: {ErrorMessage}",
                    modelName, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(
            List<string> texts, 
            string? modelName = null, 
            string? tenantId = null, 
            CancellationToken cancellationToken = default)
        {
            if (texts == null || texts.Count == 0)
            {
                _logger.LogWarning("Attempt to generate batch embeddings for empty text list");
                return new List<float[]>();
            }
            
            // Use default model if none specified
            modelName ??= _options.DefaultModel;
            
            try
            {
                _logger.LogDebug("Generating batch embeddings with model {ModelName} for {TextCount} texts",
                    modelName, texts.Count);
                
                // Get model-specific parameters if any
                Dictionary<string, object>? parameters = null;
                if (_options.ModelOptions.TryGetValue(modelName, out var modelOptions))
                {
                    parameters = modelOptions;
                }
                
                // Process in batches to avoid overwhelming the API
                var batchSize = Math.Min(texts.Count, _options.MaxBatchSize);
                var batches = new List<List<string>>();
                
                for (int i = 0; i < texts.Count; i += batchSize)
                {
                    batches.Add(texts.Skip(i).Take(batchSize).ToList());
                }
                
                _logger.LogDebug("Processing {BatchCount} batches of max size {BatchSize}", 
                    batches.Count, batchSize);
                
                var results = new List<float[]>();
                
                foreach (var batch in batches)
                {
                    // Truncate texts that exceed max length
                    var processedBatch = batch.Select(text => 
                        text.Length > _options.MaxInputLength 
                            ? text.Substring(0, _options.MaxInputLength) 
                            : text).ToList();
                    
                    // Generate batch embeddings
                    var batchResults = await _ollamaClient.GenerateBatchEmbeddingsWithModelAsync(
                        modelName, 
                        processedBatch, 
                        parameters, 
                        cancellationToken);
                    
                    // Extract embeddings
                    var embeddings = batchResults.Select(r => r.Embedding.ToArray()).ToList();
                    
                    // Normalize if enabled
                    if (_options.NormalizeVectors)
                    {
                        embeddings = embeddings.Select(NormalizeVector).ToList();
                    }
                    
                    results.AddRange(embeddings);
                    
                    // Cache model dimension from first result
                    if (!_modelDimensions.ContainsKey(modelName) && embeddings.Count > 0)
                    {
                        _modelDimensions[modelName] = embeddings[0].Length;
                    }
                }
                
                _logger.LogDebug("Generated {EmbeddingCount} embeddings with dimension {Dimension}", 
                    results.Count, results.FirstOrDefault()?.Length ?? 0);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch embeddings with model {ModelName}: {ErrorMessage}",
                    modelName, ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<int> GetEmbeddingDimensionAsync(string? modelName = null)
        {
            // Use default model if none specified
            modelName ??= _options.DefaultModel;
            
            // Return cached dimension if available
            if (_modelDimensions.TryGetValue(modelName, out int dimension))
            {
                return dimension;
            }
            
            try
            {
                _logger.LogDebug("Determining embedding dimension for model {ModelName}", modelName);
                
                // Generate a test embedding to determine the dimension
                var testResponse = await _ollamaClient.GenerateEmbeddingWithModelAsync(
                    modelName, 
                    "This is a test to determine embedding dimension.");
                
                dimension = testResponse.Embedding.Count;
                
                // Cache the result
                _modelDimensions[modelName] = dimension;
                
                _logger.LogDebug("Model {ModelName} has embedding dimension {Dimension}", modelName, dimension);
                
                return dimension;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining embedding dimension for model {ModelName}: {ErrorMessage}",
                    modelName, ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Normalizes a vector to unit length (L2 norm)
        /// </summary>
        private float[] NormalizeVector(float[] vector)
        {
            if (vector == null || vector.Length == 0)
            {
                return vector;
            }
            
            // Calculate L2 norm (Euclidean length)
            float sumOfSquares = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                sumOfSquares += vector[i] * vector[i];
            }
            
            float magnitude = (float)Math.Sqrt(sumOfSquares);
            
            // Avoid division by zero
            if (magnitude <= float.Epsilon)
            {
                return vector;
            }
            
            // Normalize
            var normalizedVector = new float[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                normalizedVector[i] = vector[i] / magnitude;
            }
            
            return normalizedVector;
        }
    }
} 