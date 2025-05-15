using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartInsight.AI.Models
{
    /// <summary>
    /// Represents the fallback level used in the tiered fallback system
    /// </summary>
    public enum FallbackLevel
    {
        /// <summary>
        /// No fallback needed, primary processing successful
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Request clarification from user for ambiguous queries
        /// </summary>
        RequestClarification = 1,
        
        /// <summary>
        /// Use generalized intent matching with reduced confidence thresholds
        /// </summary>
        GeneralizedIntent = 2,
        
        /// <summary>
        /// Attempt to extract partial intents or entities to salvage the query
        /// </summary>
        PartialIntentExtraction = 3,
        
        /// <summary>
        /// Explicit handoff to a human or another system
        /// </summary>
        ExplicitHandoff = 4
    }
    
    /// <summary>
    /// Contains the result of a fallback process
    /// </summary>
    public class FallbackResult
    {
        /// <summary>
        /// The level of fallback that was applied
        /// </summary>
        public FallbackLevel FallbackLevel { get; set; }
        
        /// <summary>
        /// The original intent detection result that triggered the fallback
        /// </summary>
        public IntentDetectionResult OriginalResult { get; set; }
        
        /// <summary>
        /// The final intent detection result after fallback strategies were applied
        /// </summary>
        public IntentDetectionResult FinalResult { get; set; }
        
        /// <summary>
        /// Clarification questions to present to the user (if applicable)
        /// </summary>
        public List<string> ClarificationQuestions { get; set; } = new List<string>();
        
        /// <summary>
        /// Possible intent alternatives that were considered during fallback
        /// </summary>
        public List<IntentDetectionResult> Alternatives { get; set; } = new List<IntentDetectionResult>();
        
        /// <summary>
        /// Whether the fallback strategy was successful in recovering from the misclassification
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// The reason for the fallback
        /// </summary>
        public string FallbackReason { get; set; }
        
        /// <summary>
        /// Whether the result requires user interaction to proceed
        /// </summary>
        public bool RequiresUserInteraction { get; set; }
        
        /// <summary>
        /// Tracking information for misclassification learning
        /// </summary>
        public MisclassificationData MisclassificationData { get; set; }
    }
    
    /// <summary>
    /// Data collected about a misclassification for learning and improvement
    /// </summary>
    public class MisclassificationData
    {
        /// <summary>
        /// Unique identifier for this misclassification instance
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The original query that was misclassified
        /// </summary>
        public string OriginalQuery { get; set; }
        
        /// <summary>
        /// The timestamp when the misclassification occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// The expected intent (if known)
        /// </summary>
        public string ExpectedIntent { get; set; }
        
        /// <summary>
        /// The actual intent that was detected
        /// </summary>
        public string ActualIntent { get; set; }
        
        /// <summary>
        /// The confidence score of the misclassification
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// The fallback level that was applied
        /// </summary>
        public FallbackLevel FallbackApplied { get; set; }
        
        /// <summary>
        /// Whether the fallback was successful
        /// </summary>
        public bool FallbackSuccessful { get; set; }
        
        /// <summary>
        /// User feedback about the fallback (if provided)
        /// </summary>
        public string UserFeedback { get; set; }
        
        /// <summary>
        /// Additional details about the misclassification
        /// </summary>
        public Dictionary<string, string> AdditionalDetails { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Options for configuring fallback behavior
    /// </summary>
    public class FallbackOptions
    {
        /// <summary>
        /// Whether fallback strategies are enabled
        /// </summary>
        public bool EnableFallbackStrategies { get; set; } = true;
        
        /// <summary>
        /// The minimum confidence threshold that triggers the fallback system
        /// </summary>
        public double FallbackThreshold { get; set; } = 0.5;
        
        /// <summary>
        /// The confidence threshold for generalized intent matching (used in level 2 fallback)
        /// </summary>
        public double GeneralizedIntentThreshold { get; set; } = 0.4;
        
        /// <summary>
        /// The confidence threshold for partial intent extraction (used in level 3 fallback)
        /// </summary>
        public double PartialIntentThreshold { get; set; } = 0.3;
        
        /// <summary>
        /// Maximum number of clarification questions to generate
        /// </summary>
        public int MaxClarificationQuestions { get; set; } = 3;
        
        /// <summary>
        /// Maximum number of alternative intents to consider
        /// </summary>
        public int MaxAlternatives { get; set; } = 5;
        
        /// <summary>
        /// Whether to automatically learn from misclassifications
        /// </summary>
        public bool LearnFromMisclassifications { get; set; } = true;
        
        /// <summary>
        /// Custom fallback prompt template for clarification requests
        /// </summary>
        public string ClarificationPromptTemplate { get; set; } = 
            "I'm not completely sure I understand. Are you asking about: {0}? Or did you mean something else?";
        
        /// <summary>
        /// The model to use for fallback operations
        /// </summary>
        public string FallbackModelName { get; set; } = "llama3";
    }
} 