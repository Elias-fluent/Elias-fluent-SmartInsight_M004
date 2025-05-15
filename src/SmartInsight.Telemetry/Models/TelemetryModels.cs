using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartInsight.Telemetry.Models
{
    /// <summary>
    /// Base class for all telemetry events
    /// </summary>
    public abstract class TelemetryEvent
    {
        /// <summary>
        /// Unique identifier for the event
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Type of the event
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// User identifier (anonymized if configured)
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Conversation or session identifier
        /// </summary>
        public string ConversationId { get; set; }
        
        /// <summary>
        /// Application version
        /// </summary>
        public string AppVersion { get; set; }
        
        /// <summary>
        /// Source component that generated the event
        /// </summary>
        public string SourceComponent { get; set; }
    }
    
    /// <summary>
    /// Event for intent detection operations
    /// </summary>
    public class IntentDetectionEvent : TelemetryEvent
    {
        /// <summary>
        /// The original user query
        /// </summary>
        public string Query { get; set; }
        
        /// <summary>
        /// The detected intent
        /// </summary>
        public string Intent { get; set; }
        
        /// <summary>
        /// Confidence score (0-1)
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// Whether context was used for detection
        /// </summary>
        public bool UsedContext { get; set; }
        
        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Name/ID of the model used
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Whether intent was self-verified by the system
        /// </summary>
        public bool SelfVerified { get; set; }
        
        /// <summary>
        /// Extracted entities
        /// </summary>
        public List<EntityInfo> ExtractedEntities { get; set; } = new List<EntityInfo>();
        
        /// <summary>
        /// Reasoning steps (if using reasoning engine)
        /// </summary>
        public List<ReasoningStepInfo> ReasoningSteps { get; set; } = new List<ReasoningStepInfo>();
        
        /// <summary>
        /// Fallback information (if fallback was used)
        /// </summary>
        public FallbackInfo Fallback { get; set; }
    }
    
    /// <summary>
    /// Event for fallback processing operations
    /// </summary>
    public class FallbackEvent : TelemetryEvent
    {
        /// <summary>
        /// The original user query
        /// </summary>
        public string Query { get; set; }
        
        /// <summary>
        /// The original intent before fallback
        /// </summary>
        public string OriginalIntent { get; set; }
        
        /// <summary>
        /// The original confidence before fallback
        /// </summary>
        public double OriginalConfidence { get; set; }
        
        /// <summary>
        /// The fallback level that was used
        /// </summary>
        public string FallbackLevel { get; set; }
        
        /// <summary>
        /// Whether the fallback was successful
        /// </summary>
        public bool WasSuccessful { get; set; }
        
        /// <summary>
        /// The reason for fallback
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// The final intent after fallback
        /// </summary>
        public string FinalIntent { get; set; }
        
        /// <summary>
        /// The final confidence after fallback
        /// </summary>
        public double FinalConfidence { get; set; }
        
        /// <summary>
        /// Whether user interaction was required
        /// </summary>
        public bool RequiredUserInteraction { get; set; }
        
        /// <summary>
        /// Number of clarification questions asked
        /// </summary>
        public int ClarificationQuestionCount { get; set; }
        
        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }
    
    /// <summary>
    /// Event for user feedback on intent detection
    /// </summary>
    public class UserFeedbackEvent : TelemetryEvent
    {
        /// <summary>
        /// The original user query
        /// </summary>
        public string Query { get; set; }
        
        /// <summary>
        /// The intent that was detected
        /// </summary>
        public string DetectedIntent { get; set; }
        
        /// <summary>
        /// The intent that was expected by the user
        /// </summary>
        public string ExpectedIntent { get; set; }
        
        /// <summary>
        /// Whether the detection was correct
        /// </summary>
        public bool WasCorrect { get; set; }
        
        /// <summary>
        /// User rating (1-5)
        /// </summary>
        public int? Rating { get; set; }
        
        /// <summary>
        /// User comments
        /// </summary>
        public string Comments { get; set; }
        
        /// <summary>
        /// Related event ID (e.g., intent detection event ID)
        /// </summary>
        public string RelatedEventId { get; set; }
    }
    
    /// <summary>
    /// Event for performance metrics
    /// </summary>
    public class PerformanceMetricEvent : TelemetryEvent
    {
        /// <summary>
        /// The operation being performed
        /// </summary>
        public string Operation { get; set; }
        
        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public long DurationMs { get; set; }
        
        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool Succeeded { get; set; }
        
        /// <summary>
        /// Error message if not successful
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Number of input tokens (for LLM operations)
        /// </summary>
        public int? InputTokens { get; set; }
        
        /// <summary>
        /// Number of output tokens (for LLM operations)
        /// </summary>
        public int? OutputTokens { get; set; }
        
        /// <summary>
        /// Cost of the operation in USD
        /// </summary>
        public decimal? Cost { get; set; }
    }
    
    /// <summary>
    /// Information about an extracted entity
    /// </summary>
    public class EntityInfo
    {
        /// <summary>
        /// Entity type
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Entity value
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Confidence score (0-1)
        /// </summary>
        public double Confidence { get; set; }
    }
    
    /// <summary>
    /// Information about a reasoning step
    /// </summary>
    public class ReasoningStepInfo
    {
        /// <summary>
        /// Step number
        /// </summary>
        public int StepNumber { get; set; }
        
        /// <summary>
        /// Description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Outcome of the step
        /// </summary>
        public string Outcome { get; set; }
        
        /// <summary>
        /// Confidence for this step (0-1)
        /// </summary>
        public double Confidence { get; set; }
    }
    
    /// <summary>
    /// Information about fallback processing
    /// </summary>
    public class FallbackInfo
    {
        /// <summary>
        /// Fallback level that was used
        /// </summary>
        public string Level { get; set; }
        
        /// <summary>
        /// Whether fallback was needed
        /// </summary>
        public bool WasNeeded { get; set; }
        
        /// <summary>
        /// Whether fallback was successful
        /// </summary>
        public bool WasSuccessful { get; set; }
        
        /// <summary>
        /// Reason for fallback
        /// </summary>
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Structure for the intent prediction model output
    /// </summary>
    public class ReasoningResult
    {
        /// <summary>
        /// Whether the reasoning was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Error message if not successful
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Intent detection result
        /// </summary>
        public SmartInsight.AI.Models.IntentDetectionResult DetectedIntent { get; set; }
        
        /// <summary>
        /// Extracted entities
        /// </summary>
        public List<SmartInsight.AI.Models.Entity> ExtractedEntities { get; set; } = new List<SmartInsight.AI.Models.Entity>();
        
        /// <summary>
        /// Reasoning steps
        /// </summary>
        public List<ReasoningStep> ReasoningSteps { get; set; } = new List<ReasoningStep>();
    }
    
    /// <summary>
    /// Structure for a reasoning step
    /// </summary>
    public class ReasoningStep
    {
        /// <summary>
        /// Step number
        /// </summary>
        public int StepNumber { get; set; }
        
        /// <summary>
        /// Description of the step
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Outcome of the step
        /// </summary>
        public string Outcome { get; set; }
        
        /// <summary>
        /// Confidence for this step (0-1)
        /// </summary>
        public double Confidence { get; set; }
    }
}