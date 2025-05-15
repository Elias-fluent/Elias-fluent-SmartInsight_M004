using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for handling fallback strategies when intent detection is uncertain or fails
    /// </summary>
    public interface IFallbackManager
    {
        /// <summary>
        /// Applies fallback strategies for a low-confidence intent detection result
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="result">The initial intent detection result</param>
        /// <param name="conversationId">Optional conversation ID for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The fallback result with improvement attempts</returns>
        Task<FallbackResult> ApplyFallbackAsync(
            string query,
            IntentDetectionResult result,
            string conversationId = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Applies fallback strategies for a low-confidence intent detection result with context
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="result">The initial intent detection result</param>
        /// <param name="conversationContext">Conversation context messages</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The fallback result with improvement attempts</returns>
        Task<FallbackResult> ApplyFallbackWithContextAsync(
            string query,
            IntentDetectionResult result,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Determines if the given intent detection result requires fallback processing
        /// </summary>
        /// <param name="result">The intent detection result to evaluate</param>
        /// <returns>True if fallback is needed, false otherwise</returns>
        bool NeedsFallback(IntentDetectionResult result);
        
        /// <summary>
        /// Generates clarification questions for an ambiguous query
        /// </summary>
        /// <param name="query">The original user query</param>
        /// <param name="alternatives">The potential intent alternatives</param>
        /// <param name="maxQuestions">Maximum number of questions to generate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clarification questions</returns>
        Task<List<string>> GenerateClarificationQuestionsAsync(
            string query,
            List<IntentDetectionResult> alternatives,
            int maxQuestions = 3,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Records a misclassification for learning and improvement
        /// </summary>
        /// <param name="misclassificationData">Data about the misclassification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully recorded, false otherwise</returns>
        Task<bool> RecordMisclassificationAsync(
            MisclassificationData misclassificationData,
            CancellationToken cancellationToken = default);
    }
} 