using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.Options;

namespace SmartInsight.AI.Intent
{
    /// <summary>
    /// Service for classifying intents using vector embeddings and similarity matching
    /// </summary>
    public class IntentClassifier : IIntentClassifier
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly IntentClassificationOptions _options;
        private readonly ILogger<IntentClassifier> _logger;
        private IntentClassificationModel _model;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IntentClassifier"/> class
        /// </summary>
        /// <param name="ollamaClient">Ollama client for generating embeddings</param>
        /// <param name="options">Classification options</param>
        /// <param name="logger">Logger instance</param>
        public IntentClassifier(
            IOllamaClient ollamaClient,
            IOptions<IntentClassificationOptions> options,
            ILogger<IntentClassifier> logger)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _model = new IntentClassificationModel
            {
                EmbeddingModel = _options.EmbeddingModelName,
                SimilarityThreshold = _options.SimilarityThreshold
            };
        }
        
        /// <inheritdoc />
        public IntentClassificationModel GetModel()
        {
            return _model;
        }
        
        /// <inheritdoc />
        public void SetModel(IntentClassificationModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }
        
        /// <inheritdoc />
        public async Task<IntentDefinition> AddIntentAsync(
            string name, 
            string description, 
            List<string> examples,
            List<EntitySlot>? entitySlots = null,
            CancellationToken cancellationToken = default)
        {
            var intent = new IntentDefinition
            {
                Name = name,
                Description = description,
                Examples = examples,
                EntitySlots = entitySlots ?? new List<EntitySlot>()
            };
            
            // Generate embeddings for examples
            if (examples.Count > 0)
            {
                var embeddings = await _ollamaClient.GenerateBatchEmbeddingsWithModelAsync(
                    _model.EmbeddingModel, 
                    examples, 
                    null, 
                    cancellationToken);
                
                intent.ExampleEmbeddings = embeddings.Select(e => e.Embedding).ToList();
            }
            
            _model.AddIntent(intent);
            
            _logger.LogInformation("Added intent {IntentName} with {ExampleCount} examples", 
                name, examples.Count);
                
            return intent;
        }
        
        /// <inheritdoc />
        public async Task<ClassificationResult> ClassifyAsync(
            string query, 
            double? similarityThreshold = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }
            
            var threshold = similarityThreshold ?? _model.SimilarityThreshold;
            
            _logger.LogDebug("Classifying query: {Query}", query);
            
            // Generate embedding for the query
            var queryEmbeddingResponse = await _ollamaClient.GenerateEmbeddingWithModelAsync(
                _model.EmbeddingModel, 
                query, 
                null, 
                cancellationToken);
                
            var queryEmbedding = queryEmbeddingResponse.Embedding;
            
            var results = new List<IntentMatch>();
            
            // Compare with all intent examples
            foreach (var intent in _model.Intents.Values)
            {
                if (intent.ExampleEmbeddings.Count == 0)
                {
                    continue;
                }
                
                double bestSimilarity = 0;
                string bestExample = "";
                
                // Find best matching example for this intent
                for (int i = 0; i < intent.ExampleEmbeddings.Count; i++)
                {
                    var exampleEmbedding = intent.ExampleEmbeddings[i];
                    var similarity = IntentClassificationModel.CalculateCosineSimilarity(queryEmbedding, exampleEmbedding);
                    
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestExample = intent.Examples[i];
                    }
                }
                
                if (bestSimilarity >= threshold)
                {
                    results.Add(new IntentMatch
                    {
                        IntentName = intent.Name,
                        Confidence = bestSimilarity,
                        MatchedExample = bestExample
                    });
                }
            }
            
            // Sort by confidence (descending)
            results.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            
            var topMatch = results.Count > 0 ? results[0] : null;
            var matchCount = results.Count;
            
            _logger.LogDebug("Found {MatchCount} intent matches for query. Top match: {TopMatch} with confidence {Confidence}", 
                matchCount, 
                topMatch?.IntentName ?? "none", 
                topMatch?.Confidence.ToString("F2") ?? "0");
            
            return new ClassificationResult
            {
                Query = query,
                Matches = results,
                TopMatch = topMatch,
                IsConfident = topMatch != null && topMatch.Confidence >= _options.HighConfidenceThreshold
            };
        }
        
        /// <inheritdoc />
        public async Task<bool> UpdateIntentExamplesAsync(
            string intentName, 
            List<string> examples,
            CancellationToken cancellationToken = default)
        {
            var resolvedName = _model.ResolveIntentName(intentName);
            
            if (!_model.Intents.TryGetValue(resolvedName, out var intent))
            {
                _logger.LogWarning("Intent {IntentName} not found", intentName);
                return false;
            }
            
            intent.Examples = examples;
            
            // Generate new embeddings
            if (examples.Count > 0)
            {
                var embeddings = await _ollamaClient.GenerateBatchEmbeddingsWithModelAsync(
                    _model.EmbeddingModel, 
                    examples, 
                    null, 
                    cancellationToken);
                
                intent.ExampleEmbeddings = embeddings.Select(e => e.Embedding).ToList();
            }
            else
            {
                intent.ExampleEmbeddings.Clear();
            }
            
            _logger.LogInformation("Updated intent {IntentName} with {ExampleCount} examples", 
                intentName, examples.Count);
                
            return true;
        }
        
        /// <inheritdoc />
        public bool RemoveIntent(string intentName)
        {
            try
            {
                var resolvedName = _model.ResolveIntentName(intentName);
                
                if (!_model.Intents.Remove(resolvedName))
                {
                    return false;
                }
                
                // Remove any aliases pointing to this intent
                var aliasesToRemove = _model.IntentAliases
                    .Where(kv => kv.Value == resolvedName)
                    .Select(kv => kv.Key)
                    .ToList();
                    
                foreach (var alias in aliasesToRemove)
                {
                    _model.IntentAliases.Remove(alias);
                }
                
                _logger.LogInformation("Removed intent {IntentName} and {AliasCount} aliases", 
                    intentName, aliasesToRemove.Count);
                    
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Result of intent classification
    /// </summary>
    public class ClassificationResult
    {
        /// <summary>
        /// The original query that was classified
        /// </summary>
        public string Query { get; set; } = null!;
        
        /// <summary>
        /// All matching intents above the threshold
        /// </summary>
        public List<IntentMatch> Matches { get; set; } = new List<IntentMatch>();
        
        /// <summary>
        /// The highest confidence match, or null if no matches found
        /// </summary>
        public IntentMatch? TopMatch { get; set; }
        
        /// <summary>
        /// Whether the classification is confident (above the high confidence threshold)
        /// </summary>
        public bool IsConfident { get; set; }
        
        /// <summary>
        /// Whether any matches were found
        /// </summary>
        public bool HasMatches => Matches.Count > 0;
    }
    
    /// <summary>
    /// A matched intent with confidence score
    /// </summary>
    public class IntentMatch
    {
        /// <summary>
        /// Name of the matched intent
        /// </summary>
        public string IntentName { get; set; } = null!;
        
        /// <summary>
        /// Confidence score of the match (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// The example that best matched the query
        /// </summary>
        public string MatchedExample { get; set; } = null!;
    }
} 