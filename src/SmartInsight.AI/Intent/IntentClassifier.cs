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
        private readonly IContextManager? _contextManager;
        private IntentClassificationModel _model;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IntentClassifier"/> class
        /// </summary>
        /// <param name="ollamaClient">Ollama client for generating embeddings</param>
        /// <param name="options">Classification options</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="contextManager">Optional context manager for context-aware classification</param>
        public IntentClassifier(
            IOllamaClient ollamaClient,
            IOptions<IntentClassificationOptions> options,
            ILogger<IntentClassifier> logger,
            IContextManager? contextManager = null)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextManager = contextManager;
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
                    var match = new IntentMatch
                    {
                        IntentName = intent.Name,
                        MatchedExample = bestExample,
                        SemanticSimilarityScore = bestSimilarity,
                        RawScore = bestSimilarity
                    };
                    
                    // Apply default confidence using only semantic similarity
                    match.Confidence = bestSimilarity;
                    
                    results.Add(match);
                }
            }
            
            // Sort by confidence (descending)
            results.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            
            // Limit number of matches
            if (results.Count > _options.MaxMatches)
            {
                results = results.Take(_options.MaxMatches).ToList();
            }
            
            var result = new ClassificationResult
            {
                Query = query,
                Matches = results,
                TopMatch = results.Count > 0 ? results[0] : null
            };
            
            _logger.LogDebug("Found {MatchCount} intent matches for query. Top match: {TopMatch} with initial confidence {Confidence}", 
                results.Count, 
                result.TopMatch?.IntentName ?? "none", 
                result.TopMatch?.Confidence.ToString("F2") ?? "0");
            
            return result;
        }
        
        /// <summary>
        /// Classifies a query with conversation context for enhanced confidence scoring
        /// </summary>
        /// <param name="query">The query to classify</param>
        /// <param name="conversationId">The conversation ID for context retrieval</param>
        /// <param name="similarityThreshold">Optional override for similarity threshold</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Classification result with context-aware confidence</returns>
        public async Task<ClassificationResult> ClassifyWithContextAsync(
            string query,
            string conversationId,
            double? similarityThreshold = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }
            
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be empty", nameof(conversationId));
            }
            
            if (_contextManager == null)
            {
                _logger.LogWarning("Context manager not available. Falling back to context-less classification.");
                return await ClassifyAsync(query, similarityThreshold, cancellationToken);
            }
            
            // Get base classification
            var result = await ClassifyAsync(query, similarityThreshold, cancellationToken);
            
            if (!result.HasMatches)
            {
                return result;
            }
            
            try
            {
                // Get conversation context
                var context = await _contextManager.GetOrCreateContextAsync(conversationId, "user");
                
                // Add the message to context
                await _contextManager.AddUserMessageAsync(conversationId, query);
                
                // Calculate context relevance
                double contextRelevance = CalculateContextRelevance(result, context);
                result.ContextRelevanceScore = contextRelevance;
                
                // Calculate historical accuracy
                double historicalAccuracy = CalculateHistoricalAccuracy(result, context);
                result.HistoricalAccuracyScore = historicalAccuracy;
                
                // Apply multi-factor confidence calculation to each match
                foreach (var match in result.Matches)
                {
                    match.CalculateConfidence(
                        match.SemanticSimilarityScore,
                        contextRelevance,
                        historicalAccuracy,
                        _options);
                    
                    // Apply contextual boost if applicable
                    if (_options.EnableContextualConfidence)
                    {
                        double boost = CalculateContextualBoost(match.IntentName, context);
                        if (boost > 0)
                        {
                            match.ApplyContextualBoost(boost);
                        }
                    }
                }
                
                // Re-sort after applying boosts
                result.Matches.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
                result.TopMatch = result.Matches.Count > 0 ? result.Matches[0] : null;
                
                // Calculate ambiguity
                result.CalculateAmbiguity(_options.AmbiguityThreshold);
                
                // Determine if we're confident
                result.IsConfident = result.TopMatch != null && 
                                    result.TopMatch.Confidence >= _options.HighConfidenceThreshold &&
                                    !result.IsAmbiguous;
                
                // Determine recommended action
                result.DetermineRecommendedAction(_options);
                
                // Build explanation
                result.BuildExplanation();
                
                _logger.LogDebug("Context-aware classification complete. Top match: {TopMatch} with final confidence {Confidence}. Action: {Action}", 
                    result.TopMatch?.IntentName ?? "none",
                    result.TopMatch?.Confidence.ToString("F2") ?? "0",
                    result.RecommendedAction);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during context-aware classification. Falling back to basic result.");
                
                // Still set basic confidence properties
                result.IsConfident = result.TopMatch != null && 
                                    result.TopMatch.Confidence >= _options.HighConfidenceThreshold;
                
                result.CalculateAmbiguity(_options.AmbiguityThreshold);
                result.DetermineRecommendedAction(_options);
                
                return result;
            }
        }
        
        /// <summary>
        /// Calculates relevance score based on conversation context
        /// </summary>
        /// <param name="result">The classification result</param>
        /// <param name="context">The conversation context</param>
        /// <returns>Context relevance score (0.0 to 1.0)</returns>
        private double CalculateContextRelevance(ClassificationResult result, ConversationContext context)
        {
            if (context.Messages.Count <= 1)
            {
                return 0.0; // No meaningful context yet
            }
            
            // Analyze conversation flow for relevance
            // This is a simple implementation - in a production system, you'd use more sophisticated semantic analysis
            
            // Check if this query seems to be a follow-up to previous exchange
            var previousMessage = context.Messages.Where(m => m.Role == "assistant").LastOrDefault();
            if (previousMessage != null)
            {
                string prevMsg = previousMessage.Content.ToLowerInvariant();
                string query = result.Query.ToLowerInvariant();
                
                // Check for follow-up patterns
                bool hasPronounReference = query.Contains("it") || query.Contains("that") || 
                                           query.Contains("this") || query.Contains("those") ||
                                           query.Contains("they") || query.Contains("them");
                
                bool hasImplicitReference = query.StartsWith("what about") || 
                                            query.StartsWith("how about") ||
                                            query.StartsWith("and ");
                
                bool isVeryShort = query.Split(' ').Length <= 3;
                
                // Assign higher context relevance for likely follow-ups
                if (hasPronounReference || hasImplicitReference || isVeryShort)
                {
                    return 0.8; // High context relevance
                }
                
                // Moderate context relevance for normal conversation flow
                return 0.4;
            }
            
            return 0.2; // Low but non-zero context relevance
        }
        
        /// <summary>
        /// Calculates historical accuracy based on previous intent detections
        /// </summary>
        /// <param name="result">The classification result</param>
        /// <param name="context">The conversation context</param>
        /// <returns>Historical accuracy score (0.0 to 1.0)</returns>
        private double CalculateHistoricalAccuracy(ClassificationResult result, ConversationContext context)
        {
            if (context.DetectedIntents.Count == 0 || !result.HasMatches)
            {
                return 0.0; // No history to evaluate
            }
            
            // Get recent intents
            var recentIntents = context.DetectedIntents
                .OrderByDescending(i => i.DetectedAt)
                .Take(_options.HistoricalInteractionsCount)
                .ToList();
            
            if (recentIntents.Count == 0)
            {
                return 0.0;
            }
            
            // Count how many times the top match has been successfully used before
            int matchCount = recentIntents.Count(i => i.Intent == result.TopMatch!.IntentName);
            
            // Calculate success rate
            double successRate = (double)matchCount / recentIntents.Count;
            
            // Weight more heavily if we have a good sample size
            double weightFactor = Math.Min(1.0, recentIntents.Count / 5.0);
            
            return successRate * weightFactor;
        }
        
        /// <summary>
        /// Calculates contextual boost based on conversation history
        /// </summary>
        /// <param name="intentName">The intent name to check</param>
        /// <param name="context">The conversation context</param>
        /// <returns>Boost amount (0.0 to 0.5)</returns>
        private double CalculateContextualBoost(string intentName, ConversationContext context)
        {
            if (!_options.EnableContextualConfidence || context.DetectedIntents.Count == 0)
            {
                return 0.0;
            }
            
            // Check if this intent is related to recent intents
            var recentIntents = context.DetectedIntents
                .OrderByDescending(i => i.DetectedAt)
                .Take(3) // Look at the most recent 3 intents
                .ToList();
            
            // For exact matches in recent history, provide a boost
            bool hasExactMatch = recentIntents.Any(i => i.Intent == intentName);
            if (hasExactMatch)
            {
                return _options.ContextualBoostFactor;
            }
            
            // For parent/child relationships, provide a smaller boost
            var intent = _model.Intents.GetValueOrDefault(intentName);
            var parentIntent = intent?.ParentIntent;
            var childIntents = intent?.ChildIntents;
            
            bool hasRelatedIntent = recentIntents.Any(i => 
                i.Intent == parentIntent || 
                (childIntents != null && childIntents.Contains(i.Intent)));
                
            if (hasRelatedIntent)
            {
                return _options.ContextualBoostFactor * 0.5; // Half boost for related intents
            }
            
            return 0.0;
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
        
        /// <summary>
        /// Analyzes the ambiguity in a classification result with detailed diagnostics
        /// </summary>
        /// <param name="result">The classification result to analyze</param>
        /// <returns>Diagnostic information about the ambiguity</returns>
        public string AnalyzeAmbiguity(ClassificationResult result)
        {
            if (!result.HasMatches)
            {
                return "No matches found to analyze.";
            }
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Analysis for query: \"{result.Query}\"");
            sb.AppendLine($"Number of matches: {result.Matches.Count}");
            sb.AppendLine();
            
            // Analyze top match
            if (result.TopMatch != null)
            {
                sb.AppendLine($"Top match: {result.TopMatch.IntentName} (Confidence: {result.TopMatch.Confidence:F3})");
                sb.AppendLine($"  Semantic similarity: {result.TopMatch.SemanticSimilarityScore:F3}");
                sb.AppendLine($"  Matched example: \"{result.TopMatch.MatchedExample}\"");
                
                if (result.TopMatch.ContextualBoost > 0)
                {
                    sb.AppendLine($"  Contextual boost: +{result.TopMatch.ContextualBoost:F3}");
                }
                
                sb.AppendLine($"  Raw score: {result.TopMatch.RawScore:F3}");
                sb.AppendLine();
            }
            
            // Analyze ambiguity
            sb.AppendLine($"Ambiguity analysis:");
            sb.AppendLine($"  Is ambiguous: {result.IsAmbiguous}");
            sb.AppendLine($"  Is confident: {result.IsConfident}");
            sb.AppendLine($"  Confidence differential: {result.ConfidenceDifferential:F3}");
            sb.AppendLine($"  Ambiguity threshold: {_options.AmbiguityThreshold:F3}");
            sb.AppendLine($"  High confidence threshold: {_options.HighConfidenceThreshold:F3}");
            sb.AppendLine($"  Mismatch threshold: {_options.MismatchThreshold:F3}");
            sb.AppendLine();
            
            // Compare top matches
            if (result.Matches.Count >= 2)
            {
                sb.AppendLine("Top competing matches:");
                for (int i = 0; i < Math.Min(3, result.Matches.Count); i++)
                {
                    var match = result.Matches[i];
                    sb.AppendLine($"  {i+1}. {match.IntentName} (Confidence: {match.Confidence:F3}, Semantic: {match.SemanticSimilarityScore:F3})");
                }
                sb.AppendLine();
            }
            
            // Action recommendation
            sb.AppendLine($"Recommended action: {result.RecommendedAction}");
            
            if (result.RecommendedAction == ConfidenceActionType.Clarify && !string.IsNullOrEmpty(result.ClarificationQuestion))
            {
                sb.AppendLine($"Suggested clarification: \"{result.ClarificationQuestion}\"");
            }
            
            return sb.ToString();
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

        /// <summary>
        /// Whether the result is ambiguous (multiple intents with similar confidence)
        /// </summary>
        public bool IsAmbiguous { get; set; }

        /// <summary>
        /// The confidence differential between the top two matches
        /// </summary>
        public double ConfidenceDifferential { get; set; }

        /// <summary>
        /// Type of action that should be taken based on confidence
        /// </summary>
        public ConfidenceActionType RecommendedAction { get; set; } = ConfidenceActionType.Proceed;

        /// <summary>
        /// Suggested clarification question if the result is ambiguous
        /// </summary>
        public string? ClarificationQuestion { get; set; }

        /// <summary>
        /// Explanation of the confidence score calculation
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// Context relevance factor used in confidence calculation (0.0 to 1.0)
        /// </summary>
        public double ContextRelevanceScore { get; set; }

        /// <summary>
        /// Historical accuracy factor used in confidence calculation (0.0 to 1.0)
        /// </summary>
        public double HistoricalAccuracyScore { get; set; }

        /// <summary>
        /// Calculates the confidence differential between the top two matches
        /// </summary>
        /// <param name="threshold">The ambiguity threshold to use</param>
        /// <returns>true if the result is ambiguous, false otherwise</returns>
        public bool CalculateAmbiguity(double threshold)
        {
            if (Matches.Count < 2)
            {
                ConfidenceDifferential = 1.0; // No ambiguity with single match
                IsAmbiguous = false;
                return false;
            }

            ConfidenceDifferential = Matches[0].Confidence - Matches[1].Confidence;
            IsAmbiguous = ConfidenceDifferential < threshold;
            return IsAmbiguous;
        }

        /// <summary>
        /// Generates a clarification question based on the top matches
        /// </summary>
        /// <returns>A question to disambiguate between intents</returns>
        public string GenerateClarificationQuestion()
        {
            if (!IsAmbiguous || Matches.Count < 2)
            {
                return string.Empty;
            }

            if (Matches.Count == 2)
            {
                return $"It seems like you might be asking about {Matches[0].IntentName} or {Matches[1].IntentName}. Could you please clarify?";
            }
            
            var topIntents = string.Join(", ", Matches.Take(3).Select(m => m.IntentName));
            return $"I'm not sure if you're asking about {topIntents}. Could you please provide more details?";
        }

        /// <summary>
        /// Determines the recommended action based on confidence scores
        /// </summary>
        /// <param name="options">The classification options with thresholds</param>
        public void DetermineRecommendedAction(IntentClassificationOptions options)
        {
            if (!HasMatches)
            {
                RecommendedAction = ConfidenceActionType.NoMatch;
                return;
            }

            if (TopMatch!.Confidence < options.MismatchThreshold)
            {
                RecommendedAction = ConfidenceActionType.Fallback;
                return;
            }

            if (IsAmbiguous && TopMatch.Confidence < options.HighConfidenceThreshold)
            {
                RecommendedAction = ConfidenceActionType.Clarify;
                ClarificationQuestion = GenerateClarificationQuestion();
                return;
            }

            if (TopMatch.Confidence >= options.HighConfidenceThreshold)
            {
                RecommendedAction = ConfidenceActionType.Proceed;
                return;
            }

            RecommendedAction = ConfidenceActionType.ProceedWithCaution;
        }

        /// <summary>
        /// Builds a detailed explanation of why this confidence score was assigned
        /// </summary>
        public void BuildExplanation()
        {
            if (!HasMatches)
            {
                Explanation = "No matching intents were found for this query.";
                return;
            }

            var explanation = new System.Text.StringBuilder();
            explanation.AppendLine($"Primary confidence based on semantic similarity: {TopMatch!.SemanticSimilarityScore:F2}");
            
            if (ContextRelevanceScore > 0)
            {
                explanation.AppendLine($"Context relevance factor: {ContextRelevanceScore:F2}");
            }
            
            if (HistoricalAccuracyScore > 0)
            {
                explanation.AppendLine($"Historical accuracy factor: {HistoricalAccuracyScore:F2}");
            }

            if (TopMatch.ContextualBoost > 0)
            {
                explanation.AppendLine($"Applied contextual boost: +{TopMatch.ContextualBoost:F2}");
            }

            if (IsAmbiguous)
            {
                explanation.AppendLine($"Result is ambiguous with confidence differential of {ConfidenceDifferential:F2} between top matches.");
            }

            explanation.AppendLine($"Final confidence: {TopMatch.Confidence:F2}");
            explanation.AppendLine($"Recommended action: {RecommendedAction}");

            Explanation = explanation.ToString();
        }
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

        /// <summary>
        /// Raw semantic similarity score (0.0 to 1.0)
        /// </summary>
        public double SemanticSimilarityScore { get; set; }

        /// <summary>
        /// Boost from contextual matches (0.0 to 0.5)
        /// </summary>
        public double ContextualBoost { get; set; }

        /// <summary>
        /// Raw score before applying weights and boosts
        /// </summary>
        public double RawScore { get; set; }

        /// <summary>
        /// Calculates the final confidence score based on multiple factors
        /// </summary>
        /// <param name="semanticSimilarity">The semantic similarity score</param>
        /// <param name="contextRelevance">The context relevance score</param>
        /// <param name="historicalAccuracy">The historical accuracy score</param>
        /// <param name="options">The classification options with weights</param>
        /// <returns>The final confidence score</returns>
        public double CalculateConfidence(
            double semanticSimilarity,
            double contextRelevance,
            double historicalAccuracy,
            IntentClassificationOptions options)
        {
            SemanticSimilarityScore = semanticSimilarity;
            RawScore = semanticSimilarity;

            // Calculate weighted score
            double weightedScore = 
                (semanticSimilarity * options.SemanticSimilarityWeight) +
                (contextRelevance * options.ContextRelevanceWeight) +
                (historicalAccuracy * options.HistoricalAccuracyWeight);

            // Ensure the score is properly normalized
            weightedScore = Math.Min(1.0, Math.Max(0.0, weightedScore));

            Confidence = weightedScore;
            return Confidence;
        }

        /// <summary>
        /// Applies a contextual boost to the confidence score based on conversation history
        /// </summary>
        /// <param name="boostAmount">The amount to boost by</param>
        /// <param name="maxConfidence">The maximum confidence score (default 1.0)</param>
        public void ApplyContextualBoost(double boostAmount, double maxConfidence = 1.0)
        {
            ContextualBoost = boostAmount;
            Confidence = Math.Min(maxConfidence, Confidence + boostAmount);
        }
    }

    /// <summary>
    /// Types of actions recommended based on confidence scores
    /// </summary>
    public enum ConfidenceActionType
    {
        /// <summary>
        /// Proceed with the detected intent with high confidence
        /// </summary>
        Proceed,

        /// <summary>
        /// Proceed but with monitoring due to moderate confidence
        /// </summary>
        ProceedWithCaution,

        /// <summary>
        /// Ask for clarification due to ambiguity
        /// </summary>
        Clarify,

        /// <summary>
        /// Use fallback handling due to low confidence
        /// </summary>
        Fallback,

        /// <summary>
        /// No match found
        /// </summary>
        NoMatch
    }
} 