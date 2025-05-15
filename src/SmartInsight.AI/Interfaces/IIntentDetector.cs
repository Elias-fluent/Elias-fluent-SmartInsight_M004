using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for intent detection and classification.
    /// </summary>
    public interface IIntentDetector
    {
        /// <summary>
        /// Detects the intent of a user query.
        /// </summary>
        /// <param name="query">The user query text.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The detected intent with confidence score.</returns>
        Task<IntentDetectionResult> DetectIntentAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects the intent of a user query with context from previous conversations.
        /// </summary>
        /// <param name="query">The user query text.</param>
        /// <param name="conversationContext">Previous messages in the conversation for context.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The detected intent with confidence score.</returns>
        Task<IntentDetectionResult> DetectIntentWithContextAsync(
            string query, 
            IEnumerable<ConversationMessage> conversationContext, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs hierarchical intent classification, detecting both top-level and sub-intents.
        /// </summary>
        /// <param name="query">The user query text.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The hierarchical intent classification result.</returns>
        Task<HierarchicalIntentResult> ClassifyHierarchicalIntentAsync(
            string query, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs reasoning on a query to extract relevant information and determine intent.
        /// </summary>
        /// <param name="query">The user query text.</param>
        /// <param name="conversationContext">Previous messages in the conversation for context.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The reasoning result with detected intent and extracted entities.</returns>
        Task<ReasoningResult> PerformReasoningAsync(
            string query,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the confidence score meets the threshold for a confident classification.
        /// </summary>
        /// <param name="confidenceScore">The confidence score to check.</param>
        /// <returns>True if the score meets the confidence threshold, false otherwise.</returns>
        bool IsConfidentClassification(double confidenceScore);
    }
} 