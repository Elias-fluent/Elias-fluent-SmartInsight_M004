using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Intent
{
    /// <summary>
    /// Provides intent detection and classification capabilities using LLM reasoning.
    /// </summary>
    public class IntentDetector : IIntentDetector
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly IContextManager _contextManager;
        private readonly IFallbackManager _fallbackManager;
        private readonly IntentDetectionOptions _options;
        private readonly ILogger<IntentDetector> _logger;

        // Predefined prompt templates for different intent detection tasks
        private const string INTENT_DETECTION_PROMPT_TEMPLATE = @"
You are an advanced intent classification system. Analyze the following user query and determine the most likely intent.

User Query: ""{0}""

Respond in JSON format with the following structure:
{{
  ""intent"": ""<intent_name>"",
  ""confidence"": <0.0 to 1.0>,
  ""explanation"": ""<brief explanation of your classification>"",
  ""entities"": [
    {{
      ""type"": ""<entity_type>"",
      ""value"": ""<entity_value>"",
      ""confidence"": <0.0 to 1.0>
    }}
  ]
}}

Only return the JSON object, no additional text.";

        private const string HIERARCHICAL_INTENT_PROMPT_TEMPLATE = @"
You are an advanced hierarchical intent classification system. Analyze the following user query and determine the top-level intent and any sub-intents.

User Query: ""{0}""

Respond in JSON format with the following structure:
{{
  ""topLevelIntent"": {{
    ""intent"": ""<intent_name>"",
    ""confidence"": <0.0 to 1.0>,
    ""explanation"": ""<brief explanation of your classification>""
  }},
  ""subIntents"": [
    {{
      ""intent"": ""<sub_intent_name>"",
      ""confidence"": <0.0 to 1.0>,
      ""explanation"": ""<brief explanation>""
    }}
  ]
}}

Only return the JSON object, no additional text.";

        private const string REASONING_PROMPT_TEMPLATE = @"
You are an advanced reasoning system for intent classification. Carefully analyze the following user query, considering the conversation context, and determine the user's intent through step-by-step reasoning.

User Query: ""{0}""

{1}

Perform a step-by-step analysis:
1. Think about what the user is asking for
2. Consider the context from previous messages
3. Identify key entities in the query
4. Determine the most likely intent

Respond in JSON format with the following structure:
{{
  ""reasoningSteps"": [
    {{
      ""stepNumber"": 1,
      ""description"": ""<reasoning step description>"",
      ""outcome"": ""<conclusion from this step>"",
      ""confidence"": <0.0 to 1.0>
    }}
  ],
  ""detectedIntent"": {{
    ""intent"": ""<intent_name>"",
    ""confidence"": <0.0 to 1.0>,
    ""explanation"": ""<explanation based on reasoning>""
  }},
  ""extractedEntities"": [
    {{
      ""type"": ""<entity_type>"",
      ""value"": ""<entity_value>"",
      ""confidence"": <0.0 to 1.0>
    }}
  ],
  ""isSuccessful"": true
}}

Only return the JSON object, no additional text.";

        private const string CONTEXT_AWARE_DETECTION_PROMPT_TEMPLATE = @"
You are an advanced intent classification system with contextual understanding. Analyze the following user query in the context of the conversation history.

{0}

Latest User Query: ""{1}""

Respond in JSON format with the following structure:
{{
  ""intent"": ""<intent_name>"",
  ""confidence"": <0.0 to 1.0>,
  ""explanation"": ""<brief explanation of your classification, including how context influenced the decision>"",
  ""entities"": [
    {{
      ""type"": ""<entity_type>"",
      ""value"": ""<entity_value>"",
      ""confidence"": <0.0 to 1.0>
    }}
  ]
}}

Only return the JSON object, no additional text.";

        /// <summary>
        /// Initializes a new instance of the <see cref="IntentDetector"/> class.
        /// </summary>
        /// <param name="ollamaClient">The Ollama client for LLM access.</param>
        /// <param name="contextManager">The context manager service.</param>
        /// <param name="fallbackManager">The fallback manager for handling low-confidence results.</param>
        /// <param name="options">The options for intent detection.</param>
        /// <param name="logger">The logger instance.</param>
        public IntentDetector(
            IOllamaClient ollamaClient,
            IContextManager contextManager,
            IFallbackManager fallbackManager,
            IOptions<IntentDetectionOptions> options,
            ILogger<IntentDetector> logger)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            _fallbackManager = fallbackManager ?? throw new ArgumentNullException(nameof(fallbackManager));
            _options = options?.Value ?? new IntentDetectionOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IntentDetectionResult> DetectIntentAsync(
            string query, 
            CancellationToken cancellationToken = default)
        {
            return await DetectIntentAsync(query, null, cancellationToken);
        }

        /// <summary>
        /// Detects the intent from a query with optional conversation ID for context.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="conversationId">Optional conversation ID for context retrieval.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detected intent result.</returns>
        public async Task<IntentDetectionResult> DetectIntentAsync(
            string query,
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            try
            {
                _logger.LogDebug("Detecting intent for query: {Query}", query);

                // If conversation ID is provided, get the context and use it
                if (!string.IsNullOrEmpty(conversationId))
                {
                    try
                    {
                        // First add the user message to the context
                        await _contextManager.AddUserMessageAsync(conversationId, query);
                        
                        // Get a meaningful context summary
                        string contextSummary = await _contextManager.GenerateContextSummaryAsync(
                            conversationId, 
                            _options.MaxContextWindowMessages);
                        
                        // Format the prompt with context summary and user query
                        string prompt = string.Format(CONTEXT_AWARE_DETECTION_PROMPT_TEMPLATE, contextSummary, query);
                        
                        // Request completion from the LLM
                        var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                            _options.ModelName,
                            prompt,
                            new Dictionary<string, object>
                            {
                                { "temperature", 0.1 }, // Low temperature for more deterministic results
                                { "top_p", 0.95 }
                            },
                            cancellationToken);
                        
                        // Parse the result
                        var result = ParseIntentDetectionResponse(response.Response);
                        
                        // Apply fallback if confidence is low
                        if (_fallbackManager.NeedsFallback(result))
                        {
                            _logger.LogInformation(
                                "Intent detection result has low confidence ({Confidence}), applying fallback strategies", 
                                result.Confidence);
                                
                            var fallbackResult = await _fallbackManager.ApplyFallbackAsync(
                                query, 
                                result, 
                                conversationId, 
                                cancellationToken);
                                
                            // Use the final result from fallback processing
                            result = fallbackResult.FinalResult;
                            
                            // Record fallback details in context
                            // Add fallback level as a special entity for context tracking
                            var fallbackEntity = new Entity
                            {
                                Type = "fallback_level",
                                Value = fallbackResult.FallbackLevel.ToString(),
                                Confidence = 1.0
                            };
                            
                            if (!result.Entities.Any(e => e.Type == "fallback_level"))
                            {
                                result.Entities.Add(fallbackEntity);
                            }
                            
                            // If fallback requires user interaction, add that as an entity too
                            if (fallbackResult.RequiresUserInteraction)
                            {
                                result.Entities.Add(new Entity
                                {
                                    Type = "requires_clarification",
                                    Value = "true",
                                    Confidence = 1.0
                                });
                                
                                // Add clarification questions as special entities
                                for (int i = 0; i < fallbackResult.ClarificationQuestions.Count; i++)
                                {
                                    result.Entities.Add(new Entity
                                    {
                                        Type = $"clarification_question_{i + 1}",
                                        Value = fallbackResult.ClarificationQuestions[i],
                                        Confidence = 1.0
                                    });
                                }
                            }
                            
                            // Add fallback reason as an entity
                            if (!string.IsNullOrEmpty(fallbackResult.FallbackReason))
                            {
                                result.Entities.Add(new Entity
                                {
                                    Type = "fallback_reason",
                                    Value = fallbackResult.FallbackReason,
                                    Confidence = 1.0
                                });
                            }
                        }
                        
                        // Record the result in context
                        await _contextManager.AddIntentDetectionResultAsync(conversationId, result);
                        
                        _logger.LogInformation("Intent detection with context result: {Intent} with confidence {Confidence}", 
                            result.Intent, result.Confidence);
                        
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error using conversation context, falling back to context-less detection");
                    }
                }

                // Context-less detection as fallback
                // Format the prompt with the user query
                string noContextPrompt = string.Format(INTENT_DETECTION_PROMPT_TEMPLATE, query);

                // Request completion from the LLM
                var noContextResponse = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.ModelName,
                    noContextPrompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.1 }, // Low temperature for more deterministic results
                        { "top_p", 0.95 }
                    },
                    cancellationToken);

                // Parse the JSON response
                var noContextResult = ParseIntentDetectionResponse(noContextResponse.Response);

                // Apply self-verification if enabled
                if (_options.EnableSelfVerification && noContextResult.Confidence < _options.ConfidenceThreshold)
                {
                    _logger.LogDebug("Self-verification triggered due to low confidence: {Confidence}", noContextResult.Confidence);
                    noContextResult = await VerifyIntent(query, noContextResult, cancellationToken);
                }

                // Apply fallback if confidence is still low after self-verification
                if (_fallbackManager.NeedsFallback(noContextResult))
                {
                    _logger.LogInformation(
                        "Context-less intent detection result has low confidence ({Confidence}), applying fallback strategies", 
                        noContextResult.Confidence);
                        
                    var fallbackResult = await _fallbackManager.ApplyFallbackAsync(
                        query, 
                        noContextResult, 
                        null, // No conversation ID for this path
                        cancellationToken);
                        
                    // Use the final result from fallback processing
                    noContextResult = fallbackResult.FinalResult;
                    
                    // Add fallback level as a special entity for context tracking
                    var fallbackEntity = new Entity
                    {
                        Type = "fallback_level",
                        Value = fallbackResult.FallbackLevel.ToString(),
                        Confidence = 1.0
                    };
                    
                    if (!noContextResult.Entities.Any(e => e.Type == "fallback_level"))
                    {
                        noContextResult.Entities.Add(fallbackEntity);
                    }
                    
                    // If fallback requires user interaction, add that as an entity too
                    if (fallbackResult.RequiresUserInteraction)
                    {
                        noContextResult.Entities.Add(new Entity
                        {
                            Type = "requires_clarification",
                            Value = "true",
                            Confidence = 1.0
                        });
                        
                        // Add clarification questions as special entities
                        for (int i = 0; i < fallbackResult.ClarificationQuestions.Count; i++)
                        {
                            noContextResult.Entities.Add(new Entity
                            {
                                Type = $"clarification_question_{i + 1}",
                                Value = fallbackResult.ClarificationQuestions[i],
                                Confidence = 1.0
                            });
                        }
                    }
                    
                    // Add fallback reason as an entity
                    if (!string.IsNullOrEmpty(fallbackResult.FallbackReason))
                    {
                        noContextResult.Entities.Add(new Entity
                        {
                            Type = "fallback_reason",
                            Value = fallbackResult.FallbackReason,
                            Confidence = 1.0
                        });
                    }
                }

                // Record the intent detection result in the conversation context if available
                if (!string.IsNullOrEmpty(conversationId))
                {
                    try
                    {
                        await _contextManager.AddIntentDetectionResultAsync(conversationId, noContextResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to record intent detection result in context");
                    }
                }

                return noContextResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting intent for query: {Query}", query);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IntentDetectionResult> DetectIntentWithContextAsync(
            string query, 
            IEnumerable<ConversationMessage> conversationContext, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            if (conversationContext == null)
            {
                conversationContext = new List<ConversationMessage>();
            }

            try
            {
                _logger.LogDebug("Detecting intent with explicit context for query: {Query}", query);

                // Format the conversation context for the prompt
                string formattedContext = FormatConversationContext(conversationContext);
                
                // Format the prompt with context and query
                string prompt = string.Format(CONTEXT_AWARE_DETECTION_PROMPT_TEMPLATE, formattedContext, query);
                
                // Request completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.ModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.1 }, // Low temperature for more deterministic results
                        { "top_p", 0.95 }
                    },
                    cancellationToken);
                
                // Parse the result
                var result = ParseIntentDetectionResponse(response.Response);
                
                // Apply fallback if confidence is low
                if (_fallbackManager.NeedsFallback(result))
                {
                    _logger.LogInformation(
                        "Intent detection with context result has low confidence ({Confidence}), applying fallback strategies", 
                        result.Confidence);
                        
                    var fallbackResult = await _fallbackManager.ApplyFallbackWithContextAsync(
                        query, 
                        result, 
                        conversationContext, 
                        cancellationToken);
                        
                    // Use the final result from fallback processing
                    result = fallbackResult.FinalResult;
                    
                    // Add fallback level as a special entity for context tracking
                    var fallbackEntity = new Entity
                    {
                        Type = "fallback_level",
                        Value = fallbackResult.FallbackLevel.ToString(),
                        Confidence = 1.0
                    };
                    
                    if (!result.Entities.Any(e => e.Type == "fallback_level"))
                    {
                        result.Entities.Add(fallbackEntity);
                    }
                    
                    // If fallback requires user interaction, add that as an entity too
                    if (fallbackResult.RequiresUserInteraction)
                    {
                        result.Entities.Add(new Entity
                        {
                            Type = "requires_clarification",
                            Value = "true",
                            Confidence = 1.0
                        });
                        
                        // Add clarification questions as special entities
                        for (int i = 0; i < fallbackResult.ClarificationQuestions.Count; i++)
                        {
                            result.Entities.Add(new Entity
                            {
                                Type = $"clarification_question_{i + 1}",
                                Value = fallbackResult.ClarificationQuestions[i],
                                Confidence = 1.0
                            });
                        }
                    }
                    
                    // Add fallback reason as an entity
                    if (!string.IsNullOrEmpty(fallbackResult.FallbackReason))
                    {
                        result.Entities.Add(new Entity
                        {
                            Type = "fallback_reason",
                            Value = fallbackResult.FallbackReason,
                            Confidence = 1.0
                        });
                    }
                }
                
                _logger.LogInformation("Intent detection with explicit context result: {Intent} with confidence {Confidence}", 
                    result.Intent, result.Confidence);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting intent with context for query: {Query}", query);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<HierarchicalIntentResult> ClassifyHierarchicalIntentAsync(
            string query, 
            CancellationToken cancellationToken = default)
        {
            return await ClassifyHierarchicalIntentAsync(query, null, cancellationToken);
        }

        /// <summary>
        /// Classifies the hierarchical intent from a query with optional conversation ID for context.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="conversationId">Optional conversation ID for context retrieval.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The hierarchical intent result.</returns>
        public async Task<HierarchicalIntentResult> ClassifyHierarchicalIntentAsync(
            string query,
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            try
            {
                _logger.LogDebug("Classifying hierarchical intent for query: {Query}", query);

                // If conversation ID is provided, add the query to the context
                if (!string.IsNullOrEmpty(conversationId))
                {
                    try
                    {
                        await _contextManager.AddUserMessageAsync(conversationId, query);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error adding user message to conversation context");
                    }
                }

                // Format the prompt with the user query
                string prompt = string.Format(HIERARCHICAL_INTENT_PROMPT_TEMPLATE, query);
                
                // Request completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.ModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.1 }, // Low temperature for more deterministic results
                        { "top_p", 0.95 }
                    },
                    cancellationToken);
                
                // Parse the JSON response
                var result = ParseHierarchicalIntentResponse(response.Response);
                
                _logger.LogInformation("Hierarchical intent classification result: {TopIntent} with {SubIntentCount} sub-intents", 
                    result.TopLevelIntent.Intent, result.SubIntents.Count);
                
                // Record the intent detection result in the conversation context if available
                if (!string.IsNullOrEmpty(conversationId))
                {
                    try
                    {
                        await _contextManager.AddIntentDetectionResultAsync(conversationId, result.TopLevelIntent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add intent detection result to conversation context");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying hierarchical intent for query: {Query}", query);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ReasoningResult> PerformReasoningAsync(
            string query,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken = default)
        {
            return await PerformReasoningAsync(query, conversationContext, null, cancellationToken);
        }

        /// <summary>
        /// Performs reasoning on a query using the chain-of-thought approach with optional conversation ID.
        /// </summary>
        /// <param name="query">The query to reason about.</param>
        /// <param name="conversationContext">Optional conversation context for reasoning.</param>
        /// <param name="conversationId">Optional conversation ID for updating the context manager.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The reasoning result with detected intent and reasoning steps.</returns>
        public async Task<ReasoningResult> PerformReasoningAsync(
            string query,
            IEnumerable<ConversationMessage> conversationContext,
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            try
            {
                _logger.LogDebug("Performing reasoning for query: {Query}", query);

                // If conversation ID is provided but no context, try to get it from the context manager
                if (!string.IsNullOrEmpty(conversationId))
                {
                    try
                    {
                        // First add the user message to ensure it's in the context
                        await _contextManager.AddUserMessageAsync(conversationId, query);
                        
                        // Generate a rich context summary for the reasoning
                        string contextSummary = await _contextManager.GenerateContextSummaryAsync(
                            conversationId, 
                            _options.MaxContextWindowMessages);
                        
                        // Format the reasoning prompt with the summary and query
                        string reasoningPrompt = string.Format(REASONING_PROMPT_TEMPLATE, 
                            query, 
                            $"Conversation Context:\n{contextSummary}");
                        
                        // Request completion from the LLM
                        var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                            _options.ModelName,
                            reasoningPrompt,
                            new Dictionary<string, object>
                            {
                                { "temperature", 0.2 },
                                { "top_p", 0.95 },
                                { "max_tokens", 1500 }
                            },
                            cancellationToken);
                        
                        // Parse the reasoning response
                        var result = ParseReasoningResponse(response.Response);
                        
                        // Record intent in context
                        if (result.IsSuccessful && result.DetectedIntent != null)
                        {
                            await _contextManager.AddIntentDetectionResultAsync(conversationId, result.DetectedIntent);
                            
                            // Add any extracted entities to the intent result for tracking
                            if (result.ExtractedEntities.Any())
                            {
                                result.DetectedIntent.Entities.AddRange(result.ExtractedEntities);
                            }
                        }
                        
                        // Add assistant response
                        if (result.IsSuccessful)
                        {
                            string reasoningSummary = $"Reasoning completed. Detected intent: {result.DetectedIntent.Intent} " +
                                $"with confidence {result.DetectedIntent.Confidence:F2}.";
                            await _contextManager.AddAssistantMessageAsync(conversationId, reasoningSummary);
                        }
                        
                        _logger.LogInformation("Reasoning result for query: Intent={Intent}, Success={Success}", 
                            result.DetectedIntent?.Intent ?? "none", result.IsSuccessful);
                        
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error using conversation context for reasoning, proceeding with available context");
                    }
                }

                // Format the reasoning prompt with the query and available context
                string contextString = conversationContext != null && conversationContext.Any() 
                    ? FormatConversationContext(conversationContext) 
                    : "No previous conversation context available.";
                
                string prompt = string.Format(REASONING_PROMPT_TEMPLATE, query, contextString);

                // Request completion from the LLM
                var fallbackResponse = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.ModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.2 },
                        { "top_p", 0.95 },
                        { "max_tokens", 1500 }
                    },
                    cancellationToken);

                // Parse the reasoning response
                var fallbackResult = ParseReasoningResponse(fallbackResponse.Response);
                
                _logger.LogInformation("Fallback reasoning result for query: Intent={Intent}, Success={Success}", 
                    fallbackResult.DetectedIntent?.Intent ?? "none", fallbackResult.IsSuccessful);
                
                return fallbackResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing reasoning for query: {Query}", query);
                throw;
            }
        }

        /// <inheritdoc />
        public bool IsConfidentClassification(double confidenceScore)
        {
            // Check if the confidence score exceeds the configured threshold
            return confidenceScore >= _options.ConfidenceThreshold;
        }

        #region Private Helper Methods

        /// <summary>
        /// Formats conversation context messages into a string for prompt inclusion
        /// </summary>
        /// <param name="conversationContext">The conversation messages to format</param>
        /// <returns>A formatted string representing the conversation context</returns>
        private string FormatConversationContext(IEnumerable<ConversationMessage> conversationContext)
        {
            if (conversationContext == null || !conversationContext.Any())
            {
                return "No conversation context available.";
            }

            var contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine("Conversation History:");
            
            int messageCount = 0;
            foreach (var message in conversationContext)
            {
                messageCount++;
                string roleName = message.Role == "user" ? "User" : "Assistant";
                contextBuilder.AppendLine($"{roleName}: {message.Content}");
                
                // Limit the number of messages to include to prevent oversized prompts
                if (messageCount >= _options.MaxContextWindowMessages && _options.MaxContextWindowMessages > 0)
                {
                    contextBuilder.AppendLine("...(earlier conversation history omitted)...");
                    break;
                }
            }
            
            return contextBuilder.ToString();
        }

        /// <summary>
        /// Parses the LLM response to extract intent detection information.
        /// </summary>
        private IntentDetectionResult ParseIntentDetectionResponse(string response)
        {
            try
            {
                // Attempt to parse as JSON
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(response);

                var result = new IntentDetectionResult
                {
                    Intent = GetStringPropertySafe(jsonResult, "intent", "unknown"),
                    Confidence = GetDoublePropertySafe(jsonResult, "confidence", 0),
                    Explanation = GetStringPropertySafe(jsonResult, "explanation", null)
                };

                // Extract entities if present
                if (jsonResult.TryGetProperty("entities", out var entitiesElement) && entitiesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entityElement in entitiesElement.EnumerateArray())
                    {
                        var entity = new Entity
                        {
                            Type = GetStringPropertySafe(entityElement, "type", "unknown"),
                            Value = GetStringPropertySafe(entityElement, "value", ""),
                            Confidence = GetDoublePropertySafe(entityElement, "confidence", 0)
                        };
                        result.Entities.Add(entity);
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse intent detection response: {Response}", response);
                
                // Return a fallback result
                return new IntentDetectionResult
                {
                    Intent = "parse_error",
                    Confidence = 0,
                    Explanation = "Failed to parse LLM response"
                };
            }
        }

        /// <summary>
        /// Parses the LLM response to extract hierarchical intent information.
        /// </summary>
        private HierarchicalIntentResult ParseHierarchicalIntentResponse(string response)
        {
            try
            {
                // Attempt to parse as JSON
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(response);

                var result = new HierarchicalIntentResult();

                // Extract top-level intent
                if (jsonResult.TryGetProperty("topLevelIntent", out var topLevelElement))
                {
                    result.TopLevelIntent = new IntentDetectionResult
                    {
                        Intent = GetStringPropertySafe(topLevelElement, "intent", "unknown"),
                        Confidence = GetDoublePropertySafe(topLevelElement, "confidence", 0),
                        Explanation = GetStringPropertySafe(topLevelElement, "explanation", null)
                    };
                }
                else
                {
                    // Fallback if topLevelIntent not found
                    result.TopLevelIntent = new IntentDetectionResult
                    {
                        Intent = "unknown",
                        Confidence = 0,
                        Explanation = "No top-level intent found in response"
                    };
                }

                // Extract sub-intents if present
                if (jsonResult.TryGetProperty("subIntents", out var subIntentsElement) && 
                    subIntentsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var subIntentElement in subIntentsElement.EnumerateArray())
                    {
                        var subIntent = new IntentDetectionResult
                        {
                            Intent = GetStringPropertySafe(subIntentElement, "intent", "unknown"),
                            Confidence = GetDoublePropertySafe(subIntentElement, "confidence", 0),
                            Explanation = GetStringPropertySafe(subIntentElement, "explanation", null)
                        };
                        result.SubIntents.Add(subIntent);
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse hierarchical intent response: {Response}", response);
                
                // Return a fallback result
                return new HierarchicalIntentResult
                {
                    TopLevelIntent = new IntentDetectionResult
                    {
                        Intent = "parse_error",
                        Confidence = 0,
                        Explanation = "Failed to parse LLM response"
                    }
                };
            }
        }

        /// <summary>
        /// Parses the LLM response to extract reasoning information.
        /// </summary>
        private ReasoningResult ParseReasoningResponse(string response)
        {
            try
            {
                // Attempt to parse as JSON
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(response);

                var result = new ReasoningResult
                {
                    IsSuccessful = GetBoolPropertySafe(jsonResult, "isSuccessful", false)
                };

                // Extract detected intent
                if (jsonResult.TryGetProperty("detectedIntent", out var intentElement))
                {
                    result.DetectedIntent = new IntentDetectionResult
                    {
                        Intent = GetStringPropertySafe(intentElement, "intent", "unknown"),
                        Confidence = GetDoublePropertySafe(intentElement, "confidence", 0),
                        Explanation = GetStringPropertySafe(intentElement, "explanation", null)
                    };
                }
                else
                {
                    // Fallback if detectedIntent not found
                    result.DetectedIntent = new IntentDetectionResult
                    {
                        Intent = "unknown",
                        Confidence = 0,
                        Explanation = "No intent found in reasoning response"
                    };
                }

                // Extract reasoning steps if present
                if (jsonResult.TryGetProperty("reasoningSteps", out var stepsElement) && 
                    stepsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var stepElement in stepsElement.EnumerateArray())
                    {
                        var step = new ReasoningStep
                        {
                            StepNumber = GetIntPropertySafe(stepElement, "stepNumber", 0),
                            Description = GetStringPropertySafe(stepElement, "description", ""),
                            Outcome = GetStringPropertySafe(stepElement, "outcome", ""),
                            Confidence = GetDoublePropertySafe(stepElement, "confidence", 0)
                        };
                        result.ReasoningSteps.Add(step);
                    }
                }

                // Extract entities if present
                if (jsonResult.TryGetProperty("extractedEntities", out var entitiesElement) && 
                    entitiesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entityElement in entitiesElement.EnumerateArray())
                    {
                        var entity = new Entity
                        {
                            Type = GetStringPropertySafe(entityElement, "type", "unknown"),
                            Value = GetStringPropertySafe(entityElement, "value", ""),
                            Confidence = GetDoublePropertySafe(entityElement, "confidence", 0)
                        };
                        result.ExtractedEntities.Add(entity);
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse reasoning response: {Response}", response);
                
                // Return a fallback result
                return new ReasoningResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Failed to parse reasoning response: {ex.Message}",
                    DetectedIntent = new IntentDetectionResult
                    {
                        Intent = "parse_error",
                        Confidence = 0,
                        Explanation = "Failed to parse LLM response"
                    }
                };
            }
        }

        /// <summary>
        /// Performs verification when initial confidence is low.
        /// </summary>
        private async Task<IntentDetectionResult> VerifyIntent(
            string query, 
            IntentDetectionResult initialResult, 
            CancellationToken cancellationToken)
        {
            // Create a verification prompt that asks the LLM to review its classification
            string verificationPrompt = $@"
Review the following intent classification for accuracy:

User Query: ""{query}""
Classified Intent: ""{initialResult.Intent}""
Confidence: {initialResult.Confidence}

Is this classification correct? If not, provide the correct intent classification.

Respond in JSON format with the following structure:
{{
  ""isCorrect"": <true/false>,
  ""correctedIntent"": ""<intent_name_if_corrected>"",
  ""confidence"": <0.0 to 1.0>,
  ""explanation"": ""<explanation of your verification>""
}}

Only return the JSON object, no additional text.";

            // Request completion for verification
            var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                _options.ModelName,
                verificationPrompt,
                new Dictionary<string, object>
                {
                    { "temperature", 0.1 }, // Low temperature for more deterministic results
                    { "top_p", 0.95 }
                },
                cancellationToken);

            try
            {
                // Parse verification response
                var verificationJson = JsonSerializer.Deserialize<JsonElement>(response.Response);
                bool isCorrect = GetBoolPropertySafe(verificationJson, "isCorrect", true);

                if (!isCorrect)
                {
                    // Return the corrected intent
                    var correctedResult = new IntentDetectionResult
                    {
                        Intent = GetStringPropertySafe(verificationJson, "correctedIntent", initialResult.Intent),
                        Confidence = GetDoublePropertySafe(verificationJson, "confidence", initialResult.Confidence),
                        Explanation = GetStringPropertySafe(verificationJson, "explanation", initialResult.Explanation),
                        Entities = initialResult.Entities // Keep original entities
                    };

                    _logger.LogInformation("Intent corrected from '{OriginalIntent}' to '{CorrectedIntent}'",
                        initialResult.Intent, correctedResult.Intent);

                    return correctedResult;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse verification response: {Response}", response.Response);
                // Continue with original result on parsing error
            }

            return initialResult;
        }

        #region Json Helper Methods

        private string GetStringPropertySafe(JsonElement element, string propertyName, string defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        private double GetDoublePropertySafe(JsonElement element, string propertyName, double defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var property) && 
                (property.ValueKind == JsonValueKind.Number || property.ValueKind == JsonValueKind.String))
            {
                // Try to handle both numeric and string representations
                if (property.ValueKind == JsonValueKind.Number)
                {
                    return property.GetDouble();
                }
                else if (double.TryParse(property.GetString(), out double value))
                {
                    return value;
                }
            }
            return defaultValue;
        }

        private bool GetBoolPropertySafe(JsonElement element, string propertyName, bool defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True || 
                property.ValueKind == JsonValueKind.False)
            {
                return property.GetBoolean();
            }
            return defaultValue;
        }

        private int GetIntPropertySafe(JsonElement element, string propertyName, int defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt32();
            }
            return defaultValue;
        }

        #endregion

        #endregion
    }
} 