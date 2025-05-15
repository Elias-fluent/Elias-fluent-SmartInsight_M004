using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for persistent storage of conversation contexts
    /// </summary>
    public interface IContextRepository
    {
        /// <summary>
        /// Gets a conversation context by its ID
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation context, or null if not found</returns>
        Task<ConversationContext?> GetContextAsync(string conversationId);
        
        /// <summary>
        /// Saves a conversation context to storage
        /// </summary>
        /// <param name="context">The conversation context to save</param>
        /// <returns>True if the save was successful</returns>
        Task<bool> SaveContextAsync(ConversationContext context);
        
        /// <summary>
        /// Deletes a conversation context from storage
        /// </summary>
        /// <param name="conversationId">The conversation ID to delete</param>
        /// <returns>True if the deletion was successful, false if the context wasn't found</returns>
        Task<bool> DeleteContextAsync(string conversationId);
        
        /// <summary>
        /// Cleans up old conversation contexts based on age
        /// </summary>
        /// <param name="maxAgeDays">Maximum age in days to keep conversations</param>
        /// <returns>The number of contexts deleted</returns>
        Task<int> CleanupOldContextsAsync(int maxAgeDays);
    }
} 