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
    /// Implements tiered fallback strategies for handling misclassifications and uncertainty
    /// </summary>
    public class FallbackManager : IFallbackManager
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly IContextManager _contextManager;
        private readonly FallbackOptions _options;
        private readonly ILogger<FallbackManager> _logger;
        
        // Prompt templates for different fallback operations
        private const string ALTERNATIVE_INTENTS_PROMPT_TEMPLATE = @"
You are an advanced intent classification assistant. The system initially classified the user's query but with low confidence. 
Please analyze the query and suggest alternative intents.

User Query: ""{0}""

Initial Classification:
- Intent: {1}
- Confidence: {2}
- Explanation: {3}

Respond with a JSON array of alternative intents, each with a name, confidence score, and explanation. 
Use this format:
[
  {{
    ""intent"": ""<alternative_intent_name>"",
    ""confidence"": <0.0 to 1.0>,
    ""explanation"": ""<why this might be the correct intent>""
  }},
  ...
]

Only return the JSON array, with no additional text.";

        private const string CLARIFICATION_QUESTIONS_PROMPT_TEMPLATE = @"
You are an advanced AI assistant helping to disambiguate user queries. The system is uncertain about the user's intent.

User Query: ""{0}""

Possible Intents:
{1}

Please generate {2} clear and concise questions that would help clarify the user's intent. 
Each question should be designed to distinguish between the possible intents.
Use natural, conversational language that sounds helpful rather than technical.

Respond with a JSON array of questions. Use this format:
[
  ""<question 1>"",
  ""<question 2>"",
  ...
]

Only return the JSON array, with no additional text.";

        private const string GENERALIZED_INTENT_PROMPT_TEMPLATE = @"
You are an advanced intent classification assistant. Please analyze the following user query with a more generalized approach.
Instead of looking for specific intents, consider broader categories of what the user might be asking for.

User Query: ""{0}""

{1}

Respond in JSON format with the following structure:
{{
  ""intent"": ""<broader_intent_category>"",
  ""confidence"": <0.0 to 1.0>,
  ""explanation"": ""<brief explanation of your generalized classification>"",
  ""suggestedNextStep"": ""<what the system should do next>""
}}

Only return the JSON object, no additional text.";

        private const string PARTIAL_INTENT_PROMPT_TEMPLATE = @"
You are an advanced intent analysis system. The user's query is ambiguous, but we need to extract any partial information we can.

User Query: ""{0}""

{1}

Please extract any identifiable intents or entities, even if partial or incomplete.
Respond in JSON format with the following structure:
{{
  ""partialIntent"": ""<what can be determined about the intent>"",
  ""confidence"": <0.0 to 1.0>,
  ""extractedEntities"": [
    {{
      ""type"": ""<entity_type>"",
      ""value"": ""<entity_value>"",
      ""confidence"": <0.0 to 1.0>
    }}
  ],
  ""missingInformation"": ""<what additional information would be needed to fully understand the query>""
}}

Only return the JSON object, no additional text.";

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackManager"/> class
        /// </summary>
        /// <param name="ollamaClient">The Ollama client for LLM access</param>
        /// <param name="contextManager">The context manager for conversation context</param>
        /// <param name="options">Options for configuring fallback behavior</param>
        /// <param name="logger">Logger instance</param>
        public FallbackManager(
            IOllamaClient ollamaClient,
            IContextManager contextManager,
            IOptions<FallbackOptions> options,
            ILogger<FallbackManager> logger)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            _options = options?.Value ?? new FallbackOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<FallbackResult> ApplyFallbackAsync(
            string query,
            IntentDetectionResult result,
            string conversationId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // Check if fallback is needed
            if (!NeedsFallback(result))
            {
                return new FallbackResult
                {
                    FallbackLevel = FallbackLevel.None,
                    OriginalResult = result,
                    FinalResult = result,
                    IsSuccessful = true,
                    FallbackReason = "No fallback needed",
                    RequiresUserInteraction = false
                };
            }

            try
            {
                _logger.LogInformation("Applying fallback strategies for query: {Query} with intent {Intent} and confidence {Confidence}", 
                    query, result.Intent, result.Confidence);

                // If conversation ID provided, get context and use it for fallback
                if (!string.IsNullOrEmpty(conversationId))
                {
                    try
                    {
                        var contextSummary = await _contextManager.GenerateContextSummaryAsync(
                            conversationId, 
                            _options.MaxClarificationQuestions);
                            
                        // Get conversation messages and apply fallback with context
                        var context = await _contextManager.GetContextAsync(conversationId);
                        return await ApplyTieredFallbackAsync(query, result, context.Messages, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error using conversation context for fallback, continuing without context");
                    }
                }

                // Apply fallback without context
                return await ApplyTieredFallbackAsync(query, result, null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying fallback strategies");
                
                // Return a fallback result indicating failure
                return new FallbackResult
                {
                    FallbackLevel = FallbackLevel.ExplicitHandoff,
                    OriginalResult = result,
                    FinalResult = result,
                    IsSuccessful = false,
                    FallbackReason = $"Error in fallback processing: {ex.Message}",
                    RequiresUserInteraction = true,
                    MisclassificationData = new MisclassificationData
                    {
                        OriginalQuery = query,
                        ActualIntent = result.Intent,
                        Confidence = result.Confidence,
                        FallbackApplied = FallbackLevel.ExplicitHandoff,
                        FallbackSuccessful = false,
                        AdditionalDetails = new Dictionary<string, string>
                        {
                            { "Error", ex.Message },
                            { "StackTrace", ex.StackTrace }
                        }
                    }
                };
            }
        }

        /// <inheritdoc />
        public async Task<FallbackResult> ApplyFallbackWithContextAsync(
            string query,
            IntentDetectionResult result,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // Apply tiered fallback with the provided context
            return await ApplyTieredFallbackAsync(query, result, conversationContext, cancellationToken);
        }

        /// <inheritdoc />
        public bool NeedsFallback(IntentDetectionResult result)
        {
            if (result == null)
            {
                return true; // Null result definitely needs fallback
            }

            // Check if confidence is below the threshold
            return result.Confidence < _options.FallbackThreshold;
        }

        /// <inheritdoc />
        public async Task<List<string>> GenerateClarificationQuestionsAsync(
            string query,
            List<IntentDetectionResult> alternatives,
            int maxQuestions = 3,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            if (alternatives == null || alternatives.Count == 0)
            {
                throw new ArgumentException("Alternatives cannot be null or empty", nameof(alternatives));
            }

            try
            {
                // Create a formatted list of alternative intents for the prompt
                var alternativesText = string.Join("\n", alternatives.Select((alt, index) => 
                    $"{index + 1}. Intent: {alt.Intent}, Confidence: {alt.Confidence:F2}, Explanation: {alt.Explanation}"));

                // Format the prompt
                string prompt = string.Format(
                    CLARIFICATION_QUESTIONS_PROMPT_TEMPLATE, 
                    query, 
                    alternativesText,
                    Math.Min(maxQuestions, _options.MaxClarificationQuestions));

                // Get completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.FallbackModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.7 }, // Higher temperature for more diverse questions
                        { "top_p", 0.95 }
                    },
                    cancellationToken);

                // Parse the JSON response
                try
                {
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var questions = JsonSerializer.Deserialize<List<string>>(response.Response, jsonOptions);
                    
                    // Return non-null questions trimmed and up to the max count
                    return questions
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .Select(q => q.Trim())
                        .Take(Math.Min(maxQuestions, _options.MaxClarificationQuestions))
                        .ToList();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse clarification questions JSON, using fallback method");
                    
                    // Fallback method if JSON parsing fails - try to extract questions by simple parsing
                    var parts = response.Response.Replace("[", "").Replace("]", "").Split("\",");
                    var questions = parts
                        .Select(p => p.Replace("\"", "").Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p) && p.EndsWith("?"))
                        .Take(Math.Min(maxQuestions, _options.MaxClarificationQuestions))
                        .ToList();
                        
                    if (questions.Any())
                    {
                        return questions;
                    }
                    
                    // If all else fails, generate a single generic question
                    return new List<string> { string.Format(_options.ClarificationPromptTemplate, alternatives[0].Intent) };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating clarification questions");
                
                // Return a generic question based on the first alternative
                return new List<string> { string.Format(_options.ClarificationPromptTemplate, alternatives[0].Intent) };
            }
        }

        /// <inheritdoc />
        public async Task<bool> RecordMisclassificationAsync(
            MisclassificationData misclassificationData,
            CancellationToken cancellationToken = default)
        {
            if (misclassificationData == null)
            {
                throw new ArgumentNullException(nameof(misclassificationData));
            }

            try
            {
                // Log the misclassification details
                _logger.LogInformation(
                    "Recorded misclassification: Query: {Query}, Expected: {Expected}, Actual: {Actual}, Confidence: {Confidence}, Fallback: {Fallback}, Success: {Success}",
                    misclassificationData.OriginalQuery,
                    misclassificationData.ExpectedIntent,
                    misclassificationData.ActualIntent,
                    misclassificationData.Confidence,
                    misclassificationData.FallbackApplied,
                    misclassificationData.FallbackSuccessful);

                // TODO: Implement persistence or analytics integration for misclassification learning
                // This could involve saving to a database, sending to a queue for later processing,
                // or integrating with a machine learning system for model improvement

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording misclassification data");
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// Applies a tiered fallback approach based on confidence and context
        /// </summary>
        private async Task<FallbackResult> ApplyTieredFallbackAsync(
            string query,
            IntentDetectionResult result,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken)
        {
            // Create misclassification data for tracking
            var misclassificationData = new MisclassificationData
            {
                OriginalQuery = query,
                ActualIntent = result.Intent,
                Confidence = result.Confidence
            };

            // Start with trying to find alternative intents
            var alternatives = await FindAlternativeIntentsAsync(query, result, conversationContext, cancellationToken);
            
            // If we have good alternatives, use clarification (Level 1)
            if (alternatives.Any(a => a.Confidence > result.Confidence))
            {
                _logger.LogDebug("Found higher confidence alternatives, using clarification fallback (Level 1)");
                
                // Sort by confidence descending
                alternatives = alternatives.OrderByDescending(a => a.Confidence).ToList();
                
                // Generate clarification questions
                var questions = await GenerateClarificationQuestionsAsync(
                    query, 
                    alternatives.Take(_options.MaxAlternatives).ToList(), 
                    _options.MaxClarificationQuestions,
                    cancellationToken);
                
                // Update misclassification data
                misclassificationData.FallbackApplied = FallbackLevel.RequestClarification;
                misclassificationData.FallbackSuccessful = questions.Any();
                
                // Return the result with clarification questions
                return new FallbackResult
                {
                    FallbackLevel = FallbackLevel.RequestClarification,
                    OriginalResult = result,
                    FinalResult = alternatives.First(), // Highest confidence alternative
                    Alternatives = alternatives,
                    ClarificationQuestions = questions,
                    IsSuccessful = questions.Any(),
                    FallbackReason = "Low confidence classification, requesting clarification",
                    RequiresUserInteraction = true,
                    MisclassificationData = misclassificationData
                };
            }
            
            // Try generalized intent matching (Level 2)
            var generalizedResult = await GeneralizeIntentAsync(query, result, conversationContext, cancellationToken);
            if (generalizedResult != null && generalizedResult.Confidence >= _options.GeneralizedIntentThreshold)
            {
                _logger.LogDebug("Using generalized intent fallback (Level 2)");
                
                // Update misclassification data
                misclassificationData.FallbackApplied = FallbackLevel.GeneralizedIntent;
                misclassificationData.FallbackSuccessful = true;
                
                return new FallbackResult
                {
                    FallbackLevel = FallbackLevel.GeneralizedIntent,
                    OriginalResult = result,
                    FinalResult = generalizedResult,
                    Alternatives = alternatives,
                    IsSuccessful = true,
                    FallbackReason = "Using generalized intent approach",
                    RequiresUserInteraction = false,
                    MisclassificationData = misclassificationData
                };
            }
            
            // Try partial intent extraction (Level 3)
            var partialResult = await ExtractPartialIntentAsync(query, result, conversationContext, cancellationToken);
            if (partialResult != null && partialResult.Entities.Any(e => e.Confidence >= _options.PartialIntentThreshold))
            {
                _logger.LogDebug("Using partial intent extraction fallback (Level 3)");
                
                // Update misclassification data
                misclassificationData.FallbackApplied = FallbackLevel.PartialIntentExtraction;
                misclassificationData.FallbackSuccessful = true;
                
                return new FallbackResult
                {
                    FallbackLevel = FallbackLevel.PartialIntentExtraction,
                    OriginalResult = result,
                    FinalResult = partialResult,
                    Alternatives = alternatives,
                    IsSuccessful = true,
                    FallbackReason = "Extracted partial intent information",
                    RequiresUserInteraction = false,
                    MisclassificationData = misclassificationData
                };
            }
            
            // If all else fails, use explicit handoff (Level 4)
            _logger.LogDebug("Using explicit handoff fallback (Level 4)");
            
            // Update misclassification data
            misclassificationData.FallbackApplied = FallbackLevel.ExplicitHandoff;
            misclassificationData.FallbackSuccessful = false;
            
            return new FallbackResult
            {
                FallbackLevel = FallbackLevel.ExplicitHandoff,
                OriginalResult = result,
                FinalResult = result, // Original result since nothing better found
                Alternatives = alternatives,
                IsSuccessful = false,
                FallbackReason = "All fallback strategies failed",
                RequiresUserInteraction = true,
                MisclassificationData = misclassificationData
            };
        }

        /// <summary>
        /// Finds alternative intent classifications for a query
        /// </summary>
        private async Task<List<IntentDetectionResult>> FindAlternativeIntentsAsync(
            string query,
            IntentDetectionResult originalResult,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken)
        {
            try
            {
                // Format the prompt with query, original intent and confidence
                string prompt = string.Format(
                    ALTERNATIVE_INTENTS_PROMPT_TEMPLATE, 
                    query, 
                    originalResult.Intent,
                    originalResult.Confidence,
                    originalResult.Explanation ?? "No explanation provided");

                // Add context summary if available
                if (conversationContext != null && conversationContext.Any())
                {
                    prompt = $"Context Information:\n{FormatConversationContext(conversationContext)}\n\n{prompt}";
                }

                // Get completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.FallbackModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.7 }, // Higher temperature for diverse alternatives
                        { "top_p", 0.95 }
                    },
                    cancellationToken);

                // Parse the JSON response
                try
                {
                    var alternatives = new List<IntentDetectionResult>();
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    
                    using (JsonDocument document = JsonDocument.Parse(response.Response))
                    {
                        JsonElement root = document.RootElement;
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement element in root.EnumerateArray())
                            {
                                alternatives.Add(new IntentDetectionResult
                                {
                                    Intent = GetStringPropertySafe(element, "intent", "unknown"),
                                    Confidence = GetDoublePropertySafe(element, "confidence", 0.0),
                                    Explanation = GetStringPropertySafe(element, "explanation", null)
                                });
                            }
                        }
                    }
                    
                    // Filter out the original intent and any with very low confidence
                    return alternatives
                        .Where(a => a.Intent != originalResult.Intent && a.Confidence > 0.2)
                        .OrderByDescending(a => a.Confidence)
                        .Take(_options.MaxAlternatives)
                        .ToList();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse alternative intents JSON");
                    return new List<IntentDetectionResult>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding alternative intents");
                return new List<IntentDetectionResult>();
            }
        }

        /// <summary>
        /// Attempts to generalize the intent for a query
        /// </summary>
        private async Task<IntentDetectionResult> GeneralizeIntentAsync(
            string query,
            IntentDetectionResult originalResult,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken)
        {
            try
            {
                // Add context summary if available
                string contextInfo = "";
                if (conversationContext != null && conversationContext.Any())
                {
                    contextInfo = $"Context Information:\n{FormatConversationContext(conversationContext)}";
                }
                
                // Format the prompt
                string prompt = string.Format(GENERALIZED_INTENT_PROMPT_TEMPLATE, query, contextInfo);

                // Get completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.FallbackModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.4 }, // Lower temperature for more reliable generalization
                        { "top_p", 0.95 }
                    },
                    cancellationToken);

                // Parse the JSON response
                try
                {
                    using (JsonDocument document = JsonDocument.Parse(response.Response))
                    {
                        JsonElement root = document.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            var result = new IntentDetectionResult
                            {
                                Intent = GetStringPropertySafe(root, "intent", "general_query"),
                                Confidence = GetDoublePropertySafe(root, "confidence", 0.5),
                                Explanation = GetStringPropertySafe(root, "explanation", "Generalized intent")
                            };
                            
                            // Add suggested next step as an entity
                            string suggestedNextStep = GetStringPropertySafe(root, "suggestedNextStep", null);
                            if (!string.IsNullOrEmpty(suggestedNextStep))
                            {
                                result.Entities.Add(new Entity
                                {
                                    Type = "next_step",
                                    Value = suggestedNextStep,
                                    Confidence = 0.9
                                });
                            }
                            
                            return result;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse generalized intent JSON");
                }
                
                // Return a default generalized result if parsing failed
                return new IntentDetectionResult
                {
                    Intent = "general_query",
                    Confidence = 0.4,
                    Explanation = "Fallback to general query handling"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generalizing intent");
                return null;
            }
        }

        /// <summary>
        /// Attempts to extract partial intents or entities from a query
        /// </summary>
        private async Task<IntentDetectionResult> ExtractPartialIntentAsync(
            string query,
            IntentDetectionResult originalResult,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken)
        {
            try
            {
                // Add context summary if available
                string contextInfo = "";
                if (conversationContext != null && conversationContext.Any())
                {
                    contextInfo = $"Context Information:\n{FormatConversationContext(conversationContext)}";
                }
                
                // Format the prompt
                string prompt = string.Format(PARTIAL_INTENT_PROMPT_TEMPLATE, query, contextInfo);

                // Get completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.FallbackModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.3 }, // Lower temperature for more reliable extraction
                        { "top_p", 0.95 }
                    },
                    cancellationToken);

                // Parse the JSON response
                try
                {
                    using (JsonDocument document = JsonDocument.Parse(response.Response))
                    {
                        JsonElement root = document.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            var partialIntent = GetStringPropertySafe(root, "partialIntent", "unclear_intent");
                            var confidence = GetDoublePropertySafe(root, "confidence", 0.3);
                            var missingInfo = GetStringPropertySafe(root, "missingInformation", "Additional context needed");
                            
                            var result = new IntentDetectionResult
                            {
                                Intent = partialIntent,
                                Confidence = confidence,
                                Explanation = $"Partial intent extraction. Missing: {missingInfo}"
                            };
                            
                            // Extract entities if available
                            if (root.TryGetProperty("extractedEntities", out JsonElement entitiesElement) && 
                                entitiesElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement entityElement in entitiesElement.EnumerateArray())
                                {
                                    if (entityElement.ValueKind == JsonValueKind.Object)
                                    {
                                        result.Entities.Add(new Entity
                                        {
                                            Type = GetStringPropertySafe(entityElement, "type", "unknown"),
                                            Value = GetStringPropertySafe(entityElement, "value", ""),
                                            Confidence = GetDoublePropertySafe(entityElement, "confidence", 0.5)
                                        });
                                    }
                                }
                            }
                            
                            // Add missing information as a special entity
                            result.Entities.Add(new Entity
                            {
                                Type = "missing_information",
                                Value = missingInfo,
                                Confidence = 0.9
                            });
                            
                            return result;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse partial intent JSON");
                }
                
                // Return null if extraction failed
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting partial intent");
                return null;
            }
        }

        /// <summary>
        /// Formats conversation context for inclusion in prompts
        /// </summary>
        private string FormatConversationContext(IEnumerable<ConversationMessage> conversationContext)
        {
            if (conversationContext == null || !conversationContext.Any())
            {
                return string.Empty;
            }

            var formattedContext = new List<string>();
            foreach (var message in conversationContext.OrderBy(m => m.Timestamp).TakeLast(5))
            {
                var rolePrefix = message.Role.ToLowerInvariant() switch
                {
                    "user" => "User",
                    "assistant" => "Assistant",
                    "system" => "System",
                    _ => message.Role
                };

                formattedContext.Add($"{rolePrefix}: {message.Content}");
            }

            return string.Join("\n", formattedContext);
        }
        
        /// <summary>
        /// Safely extracts a string property from a JsonElement
        /// </summary>
        private string GetStringPropertySafe(JsonElement element, string propertyName, string defaultValue)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property) && 
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Safely extracts a double property from a JsonElement
        /// </summary>
        private double GetDoublePropertySafe(JsonElement element, string propertyName, double defaultValue)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                switch (property.ValueKind)
                {
                    case JsonValueKind.Number:
                        return property.GetDouble();
                    case JsonValueKind.String:
                        if (double.TryParse(property.GetString(), out double result))
                        {
                            return result;
                        }
                        break;
                }
            }
            
            return defaultValue;
        }
        
        #endregion
    }
} 