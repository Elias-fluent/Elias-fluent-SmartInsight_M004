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
    /// Implements a Chain-of-Thought reasoning engine for processing complex queries.
    /// </summary>
    public class ReasoningEngine : IReasoningEngine
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly IntentDetectionOptions _options;
        private readonly ILogger<ReasoningEngine> _logger;

        // Predefined prompt templates for chain-of-thought reasoning
        private const string CHAIN_OF_THOUGHT_PROMPT = @"
You are an advanced reasoning system using chain-of-thought methodology. Think step-by-step to solve the following problem:

{0}

Previous context:
{1}

Follow this reasoning process:
1. Parse and clarify the request
2. Identify key entities and their relationships
3. Consider potential approaches to the request
4. Analyze constraints and limitations
5. Develop a multi-step solution plan
6. Refine and validate the solution

Provide your reasoning in the following JSON format:
{{
  ""reasoning"": [
    {{
      ""step"": 1,
      ""thought"": ""<your thought process for this step>"",
      ""conclusion"": ""<what you concluded in this step>""
    }},
    ...
  ],
  ""finalConclusion"": ""<your final conclusion after reasoning>"",
  ""confidenceScore"": <0.0 to 1.0>,
  ""entities"": [
    {{
      ""type"": ""<entity_type>"",
      ""value"": ""<entity_value>"",
      ""importance"": <0 to 10>
    }},
    ...
  ],
  ""suggestedActions"": [
    ""<action1>"",
    ""<action2>"",
    ...
  ]
}}";

        private const string SELF_VERIFICATION_PROMPT = @"
Review the following chain-of-thought reasoning for logical consistency, factual accuracy, and sound conclusions:

{0}

Verify:
1. Are all steps logically connected?
2. Are there any factual errors?
3. Is the conclusion supported by the reasoning steps?
4. Are there any incorrect assumptions?
5. Could the reasoning be improved?

Provide your assessment in JSON format:
{{
  ""isValid"": <true/false>,
  ""confidenceScore"": <0.0 to 1.0>,
  ""issues"": [
    {{
      ""step"": <step_number>,
      ""issue"": ""<description of issue>"",
      ""correction"": ""<suggested correction>""
    }},
    ...
  ],
  ""improvedConclusion"": ""<improved conclusion if applicable>""
}}";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReasoningEngine"/> class.
        /// </summary>
        /// <param name="ollamaClient">The Ollama client for LLM access.</param>
        /// <param name="options">The options for intent detection.</param>
        /// <param name="logger">The logger instance.</param>
        public ReasoningEngine(
            IOllamaClient ollamaClient,
            IOptions<IntentDetectionOptions> options,
            ILogger<ReasoningEngine> logger)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs chain-of-thought reasoning on a complex query.
        /// </summary>
        /// <param name="query">The user query to analyze.</param>
        /// <param name="conversationContext">Previous conversation messages for context.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A reasoning result with detailed chain-of-thought steps.</returns>
        public async Task<ChainOfThoughtResult> PerformChainOfThoughtReasoningAsync(
            string query,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            try
            {
                _logger.LogDebug("Performing chain-of-thought reasoning for query: {Query}", query);

                // Format conversation context
                string contextString = FormatConversationContext(conversationContext);

                // Format the prompt with the user query and conversation context
                string prompt = string.Format(CHAIN_OF_THOUGHT_PROMPT, query, contextString);

                // Request completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.ModelName,
                    prompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.2 }, // Low temperature for more deterministic reasoning
                        { "top_p", 0.95 },
                        { "max_tokens", 2048 } // Ensure enough tokens for detailed reasoning
                    },
                    cancellationToken);

                // Parse the JSON response
                var result = ParseChainOfThoughtResponse(response.Response);

                // Perform self-verification if enabled and the result is not null
                if (_options.EnableSelfVerification && result != null)
                {
                    result = await VerifyChainOfThoughtAsync(result, cancellationToken);
                }

                _logger.LogInformation("Chain-of-thought reasoning completed with {StepCount} steps and confidence {Confidence}",
                    result?.ReasoningSteps.Count ?? 0, result?.ConfidenceScore ?? 0);

                return result ?? CreateErrorResult("Failed to parse reasoning response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing chain-of-thought reasoning for query: {Query}", query);
                return CreateErrorResult($"Error during reasoning: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies the chain-of-thought reasoning for logical consistency and accuracy.
        /// </summary>
        /// <param name="initialResult">The initial reasoning result to verify.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The verified or corrected reasoning result.</returns>
        private async Task<ChainOfThoughtResult> VerifyChainOfThoughtAsync(
            ChainOfThoughtResult initialResult,
            CancellationToken cancellationToken)
        {
            try
            {
                // Serialize the result to provide to the verification LLM
                string resultJson = JsonSerializer.Serialize(initialResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Format the verification prompt
                string verificationPrompt = string.Format(SELF_VERIFICATION_PROMPT, resultJson);

                // Request completion from the LLM
                var response = await _ollamaClient.GenerateCompletionWithModelAsync(
                    _options.ModelName,
                    verificationPrompt,
                    new Dictionary<string, object>
                    {
                        { "temperature", 0.1 }, // Very low temperature for critical verification
                        { "top_p", 0.95 },
                        { "max_tokens", 1024 }
                    },
                    cancellationToken);

                try
                {
                    // Parse the verification response
                    var verificationJson = JsonSerializer.Deserialize<JsonElement>(response.Response);
                    bool isValid = GetBoolPropertySafe(verificationJson, "isValid", true);

                    if (isValid)
                    {
                        // The reasoning was valid, return the original result with possibly improved confidence
                        double verifiedConfidence = GetDoublePropertySafe(verificationJson, "confidenceScore", initialResult.ConfidenceScore);
                        initialResult.ConfidenceScore = Math.Max(initialResult.ConfidenceScore, verifiedConfidence);
                        initialResult.IsVerified = true;
                        return initialResult;
                    }
                    else
                    {
                        // The reasoning had issues, incorporate corrections
                        var correctedResult = new ChainOfThoughtResult
                        {
                            ReasoningSteps = new List<ChainOfThoughtStep>(initialResult.ReasoningSteps),
                            FinalConclusion = GetStringPropertySafe(verificationJson, "improvedConclusion", initialResult.FinalConclusion),
                            ExtractedEntities = initialResult.ExtractedEntities,
                            SuggestedActions = initialResult.SuggestedActions,
                            ConfidenceScore = GetDoublePropertySafe(verificationJson, "confidenceScore", initialResult.ConfidenceScore),
                            IsVerified = true
                        };

                        // Apply corrections to specific steps if provided
                        if (verificationJson.TryGetProperty("issues", out var issuesElement) && 
                            issuesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var issueElement in issuesElement.EnumerateArray())
                            {
                                int stepIndex = GetIntPropertySafe(issueElement, "step", 0) - 1; // Convert from 1-based to 0-based
                                if (stepIndex >= 0 && stepIndex < correctedResult.ReasoningSteps.Count)
                                {
                                    var step = correctedResult.ReasoningSteps[stepIndex];
                                    string correction = GetStringPropertySafe(issueElement, "correction", null);
                                    if (!string.IsNullOrEmpty(correction))
                                    {
                                        step.Conclusion = correction;
                                        step.IsRevised = true;
                                    }
                                }
                            }
                        }

                        return correctedResult;
                    }
                }
                catch (JsonException)
                {
                    // If we can't parse the verification response, return the original result
                    _logger.LogWarning("Failed to parse verification response: {Response}", response.Response);
                    return initialResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reasoning verification");
                return initialResult; // Return the original result on error
            }
        }

        /// <summary>
        /// Parses the LLM response into a ChainOfThoughtResult object.
        /// </summary>
        /// <param name="response">The JSON response from the LLM.</param>
        /// <returns>A ChainOfThoughtResult object, or null if parsing fails.</returns>
        private ChainOfThoughtResult ParseChainOfThoughtResponse(string response)
        {
            try
            {
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(response);
                var result = new ChainOfThoughtResult
                {
                    ReasoningSteps = new List<ChainOfThoughtStep>(),
                    ExtractedEntities = new List<Entity>(),
                    SuggestedActions = new List<string>(),
                    FinalConclusion = GetStringPropertySafe(jsonResult, "finalConclusion", "No conclusion provided"),
                    ConfidenceScore = GetDoublePropertySafe(jsonResult, "confidenceScore", 0.5)
                };

                // Extract reasoning steps
                if (jsonResult.TryGetProperty("reasoning", out var reasoningElement) && 
                    reasoningElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var stepElement in reasoningElement.EnumerateArray())
                    {
                        var step = new ChainOfThoughtStep
                        {
                            StepNumber = GetIntPropertySafe(stepElement, "step", 0),
                            Thought = GetStringPropertySafe(stepElement, "thought", ""),
                            Conclusion = GetStringPropertySafe(stepElement, "conclusion", "")
                        };
                        result.ReasoningSteps.Add(step);
                    }
                }

                // Extract entities
                if (jsonResult.TryGetProperty("entities", out var entitiesElement) && 
                    entitiesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entityElement in entitiesElement.EnumerateArray())
                    {
                        var entity = new Entity
                        {
                            Type = GetStringPropertySafe(entityElement, "type", "unknown"),
                            Value = GetStringPropertySafe(entityElement, "value", ""),
                            Confidence = GetDoublePropertySafe(entityElement, "confidence", 0.5)
                        };

                        // If importance is provided instead of confidence, convert it
                        if (entityElement.TryGetProperty("importance", out var importanceElement))
                        {
                            int importance = importanceElement.GetInt32();
                            entity.Confidence = Math.Min(1.0, importance / 10.0); // Convert 0-10 scale to 0-1
                        }

                        result.ExtractedEntities.Add(entity);
                    }
                }

                // Extract suggested actions
                if (jsonResult.TryGetProperty("suggestedActions", out var actionsElement) && 
                    actionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var actionElement in actionsElement.EnumerateArray())
                    {
                        if (actionElement.ValueKind == JsonValueKind.String)
                        {
                            result.SuggestedActions.Add(actionElement.GetString());
                        }
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse chain-of-thought response: {Response}", response);
                return null;
            }
        }

        /// <summary>
        /// Creates an error result for chain-of-thought reasoning.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A ChainOfThoughtResult indicating an error.</returns>
        private ChainOfThoughtResult CreateErrorResult(string errorMessage)
        {
            return new ChainOfThoughtResult
            {
                ReasoningSteps = new List<ChainOfThoughtStep>
                {
                    new ChainOfThoughtStep
                    {
                        StepNumber = 1,
                        Thought = "Error occurred during reasoning",
                        Conclusion = errorMessage
                    }
                },
                FinalConclusion = "Unable to provide reasoning due to an error",
                ConfidenceScore = 0,
                ExtractedEntities = new List<Entity>(),
                SuggestedActions = new List<string>(),
                HasError = true,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Formats the conversation context into a string.
        /// </summary>
        /// <param name="conversationContext">The conversation messages.</param>
        /// <returns>A formatted string of conversation context.</returns>
        private string FormatConversationContext(IEnumerable<ConversationMessage> conversationContext)
        {
            if (conversationContext == null || !conversationContext.Any())
            {
                return "No previous context available.";
            }

            var contextBuilder = new System.Text.StringBuilder();
            foreach (var message in conversationContext.OrderBy(m => m.Timestamp))
            {
                contextBuilder.AppendLine($"{message.Role}: {message.Content}");
            }

            return contextBuilder.ToString();
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
            if (element.TryGetProperty(propertyName, out var property) && 
                (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False))
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
    }
} 