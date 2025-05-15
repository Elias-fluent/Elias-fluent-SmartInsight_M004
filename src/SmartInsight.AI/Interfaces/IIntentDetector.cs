using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for intent detection service.
    /// </summary>
    public interface IIntentDetector
    {
        /// <summary>
        /// Detects the intent from a query without any context.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detected intent result.</returns>
        Task<IntentDetectionResult> DetectIntentAsync(
            string query, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Detects the intent from a query with optional conversation ID for context.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="conversationId">Optional conversation ID for context retrieval.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detected intent result.</returns>
        Task<IntentDetectionResult> DetectIntentAsync(
            string query,
            string conversationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects the intent from a query with conversation context.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="conversationContext">Previous messages in the conversation for context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detected intent result.</returns>
        Task<IntentDetectionResult> DetectIntentWithContextAsync(
            string query, 
            IEnumerable<ConversationMessage> conversationContext, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Classifies the hierarchical intent from a query.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The hierarchical intent classification result.</returns>
        Task<HierarchicalIntentResult> ClassifyHierarchicalIntentAsync(
            string query, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Classifies the hierarchical intent from a query with optional conversation ID for context.
        /// </summary>
        /// <param name="query">The query to analyze.</param>
        /// <param name="conversationId">Optional conversation ID for context retrieval.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The hierarchical intent classification result.</returns>
        Task<HierarchicalIntentResult> ClassifyHierarchicalIntentAsync(
            string query,
            string conversationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs reasoning on a query using the chain-of-thought approach.
        /// </summary>
        /// <param name="query">The query to reason about.</param>
        /// <param name="conversationContext">Optional conversation context for reasoning.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The reasoning result with detected intent and reasoning steps.</returns>
        Task<ReasoningResult> PerformReasoningAsync(
            string query, 
            IEnumerable<ConversationMessage> conversationContext, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Performs reasoning on a query using the chain-of-thought approach with optional conversation ID.
        /// </summary>
        /// <param name="query">The query to reason about.</param>
        /// <param name="conversationContext">Optional conversation context for reasoning.</param>
        /// <param name="conversationId">Optional conversation ID for updating the context manager.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The reasoning result with detected intent and reasoning steps.</returns>
        Task<ReasoningResult> PerformReasoningAsync(
            string query,
            IEnumerable<ConversationMessage> conversationContext,
            string conversationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a confidence score is high enough to be considered reliable.
        /// </summary>
        /// <param name="confidenceScore">The confidence score to check.</param>
        /// <returns>True if the confidence is above the threshold, false otherwise.</returns>
        bool IsConfidentClassification(double confidenceScore);
    }
} 