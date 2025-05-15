using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for the chain-of-thought reasoning engine.
    /// </summary>
    public interface IReasoningEngine
    {
        /// <summary>
        /// Performs chain-of-thought reasoning on a complex query.
        /// </summary>
        /// <param name="query">The user query to analyze.</param>
        /// <param name="conversationContext">Previous conversation messages for context.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A reasoning result with detailed chain-of-thought steps.</returns>
        Task<ChainOfThoughtResult> PerformChainOfThoughtReasoningAsync(
            string query,
            IEnumerable<ConversationMessage> conversationContext,
            CancellationToken cancellationToken = default);
    }
} 