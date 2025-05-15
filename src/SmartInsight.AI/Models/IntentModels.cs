using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartInsight.AI.Models
{
    /// <summary>
    /// Represents a conversation message for context in intent detection.
    /// </summary>
    public class ConversationMessage
    {
        /// <summary>
        /// The role of the message sender (system, user, assistant).
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Content { get; set; } = null!;

        /// <summary>
        /// The timestamp when the message was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents the result of intent detection.
    /// </summary>
    public class IntentDetectionResult
    {
        /// <summary>
        /// The detected intent name.
        /// </summary>
        public string Intent { get; set; } = null!;

        /// <summary>
        /// The top intent detected (same as Intent, provided for naming consistency).
        /// </summary>
        public string TopIntent => Intent;

        /// <summary>
        /// The original query that was processed.
        /// </summary>
        public string Query { get; set; } = null!;

        /// <summary>
        /// The confidence score of the intent detection (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Optional entities extracted from the query.
        /// </summary>
        public List<Entity> Entities { get; set; } = new List<Entity>();

        /// <summary>
        /// Optional explanation of the intent detection reasoning.
        /// </summary>
        public string? Explanation { get; set; }
    }

    /// <summary>
    /// Represents a hierarchical intent classification result.
    /// </summary>
    public class HierarchicalIntentResult
    {
        /// <summary>
        /// The top-level intent detected.
        /// </summary>
        public IntentDetectionResult TopLevelIntent { get; set; } = null!;

        /// <summary>
        /// Sub-intents detected within the top-level intent.
        /// </summary>
        public List<IntentDetectionResult> SubIntents { get; set; } = new List<IntentDetectionResult>();

        /// <summary>
        /// Whether the query contains multiple intents.
        /// </summary>
        public bool HasMultipleIntents => SubIntents.Count > 0;
    }

    /// <summary>
    /// Represents an entity extracted from a query.
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// The type of entity (e.g., person, location, date).
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// The value of the entity.
        /// </summary>
        public string Value { get; set; } = null!;

        /// <summary>
        /// The original text from which the entity was extracted
        /// </summary>
        public string Text { get; set; } = null!;

        /// <summary>
        /// The confidence score of the entity extraction (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The start position of the entity in the original text.
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// The end position of the entity in the original text.
        /// </summary>
        public int EndPosition { get; set; }
    }

    /// <summary>
    /// Represents the result of a reasoning process.
    /// </summary>
    public class ReasoningResult
    {
        /// <summary>
        /// The detected intent from reasoning.
        /// </summary>
        public IntentDetectionResult DetectedIntent { get; set; } = null!;

        /// <summary>
        /// The reasoning steps performed to determine the intent.
        /// </summary>
        public List<ReasoningStep> ReasoningSteps { get; set; } = new List<ReasoningStep>();

        /// <summary>
        /// Extracted entities from the reasoning process.
        /// </summary>
        public List<Entity> ExtractedEntities { get; set; } = new List<Entity>();

        /// <summary>
        /// Alias for ExtractedEntities for API consistency.
        /// </summary>
        public List<Entity> Entities => ExtractedEntities;

        /// <summary>
        /// The original query that was processed.
        /// </summary>
        public string Query { get; set; } = null!;

        /// <summary>
        /// Dictionary for structured data extracted during reasoning.
        /// </summary>
        public Dictionary<string, object> StructuredData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether the reasoning was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Any error message if reasoning was not successful.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a single step in the reasoning process.
    /// </summary>
    public class ReasoningStep
    {
        /// <summary>
        /// The step number in the reasoning process.
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// The description of the reasoning step.
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// The outcome or conclusion from this reasoning step.
        /// </summary>
        public string Outcome { get; set; } = null!;

        /// <summary>
        /// Confidence in this reasoning step (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Options for configuring intent detection behavior.
    /// </summary>
    public class IntentDetectionOptions
    {
        /// <summary>
        /// The confidence threshold for accepting an intent classification (0.0 to 1.0).
        /// </summary>
        public double ConfidenceThreshold { get; set; } = 0.7;

        /// <summary>
        /// The maximum number of intents to return.
        /// </summary>
        public int MaxIntents { get; set; } = 3;

        /// <summary>
        /// Whether to include detailed explanations in the results.
        /// </summary>
        public bool IncludeExplanations { get; set; } = false;

        /// <summary>
        /// Whether to extract entities during intent detection.
        /// </summary>
        public bool ExtractEntities { get; set; } = true;

        /// <summary>
        /// Whether to use hierarchical classification.
        /// </summary>
        public bool UseHierarchicalClassification { get; set; } = true;

        /// <summary>
        /// The model to use for intent detection.
        /// </summary>
        public string ModelName { get; set; } = "llama3";

        /// <summary>
        /// Whether to enable self-verification of intent classifications.
        /// </summary>
        public bool EnableSelfVerification { get; set; } = true;
        
        /// <summary>
        /// Maximum number of conversation messages to include in the context window.
        /// Set to 0 to include all messages.
        /// </summary>
        public int MaxContextWindowMessages { get; set; } = 10;
    }
} 