using System;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;
using SmartInsight.Telemetry.Models;

namespace SmartInsight.Telemetry.Interfaces
{
    /// <summary>
    /// Interface for the telemetry service that captures AI processing events for training and improvement
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// Logs an intent detection event
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="result">The intent detection result</param>
        /// <param name="usedContext">Whether context was used</param>
        /// <param name="processingTimeMs">The processing time in milliseconds</param>
        /// <param name="userId">The user ID</param>
        /// <param name="conversationId">The conversation or session ID</param>
        /// <param name="modelName">The name of the model used</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The event ID</returns>
        Task<string> LogIntentDetectionAsync(
            string query,
            IntentDetectionResult result,
            bool usedContext,
            long processingTimeMs,
            string userId,
            string conversationId,
            string modelName,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Logs a fallback processing event
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="fallbackResult">The fallback result</param>
        /// <param name="processingTimeMs">The processing time in milliseconds</param>
        /// <param name="userId">The user ID</param>
        /// <param name="conversationId">The conversation or session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The event ID</returns>
        Task<string> LogFallbackProcessingAsync(
            string query,
            FallbackResult fallbackResult,
            long processingTimeMs,
            string userId,
            string conversationId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Logs user feedback on intent detection
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="detectedIntent">The intent that was detected</param>
        /// <param name="expectedIntent">The intent that was expected</param>
        /// <param name="wasCorrect">Whether the detection was correct</param>
        /// <param name="rating">User rating (1-5)</param>
        /// <param name="comments">User comments</param>
        /// <param name="relatedEventId">Related event ID</param>
        /// <param name="userId">The user ID</param>
        /// <param name="conversationId">The conversation or session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The event ID</returns>
        Task<string> LogUserFeedbackAsync(
            string query,
            string detectedIntent,
            string expectedIntent,
            bool wasCorrect,
            int? rating,
            string comments,
            string relatedEventId,
            string userId,
            string conversationId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Logs a performance metric
        /// </summary>
        /// <param name="operation">The operation name</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <param name="succeeded">Whether the operation succeeded</param>
        /// <param name="errorMessage">Error message if not successful</param>
        /// <param name="inputTokens">Number of input tokens</param>
        /// <param name="outputTokens">Number of output tokens</param>
        /// <param name="cost">Operation cost</param>
        /// <param name="userId">The user ID</param>
        /// <param name="conversationId">The conversation or session ID</param>
        /// <param name="sourceComponent">The component generating the metric</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The event ID</returns>
        Task<string> LogPerformanceMetricAsync(
            string operation,
            long durationMs,
            bool succeeded,
            string errorMessage,
            int? inputTokens,
            int? outputTokens,
            decimal? cost,
            string userId,
            string conversationId,
            string sourceComponent,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Logs a reasoning operation
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="reasoningResult">The reasoning result</param>
        /// <param name="processingTimeMs">The processing time in milliseconds</param>
        /// <param name="userId">The user ID</param>
        /// <param name="conversationId">The conversation or session ID</param>
        /// <param name="modelName">The name of the model used</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The event ID</returns>
        Task<string> LogReasoningOperationAsync(
            string query,
            SmartInsight.Telemetry.Models.ReasoningResult reasoningResult,
            long processingTimeMs,
            string userId,
            string conversationId,
            string modelName,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets anonymized metrics for analysis
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="metricType">Metric type (intent_detection, fallback, performance)</param>
        /// <param name="limit">Maximum number of events to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Anonymized metrics object</returns>
        Task<object> GetAnonymizedMetricsAsync(
            DateTime startTime,
            DateTime endTime,
            string metricType,
            int limit = 100,
            CancellationToken cancellationToken = default);
    }
} 