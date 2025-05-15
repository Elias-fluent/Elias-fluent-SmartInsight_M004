using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for managing conversation contexts
    /// </summary>
    public interface IContextManager
    {
        /// <summary>
        /// Gets an existing conversation context or creates a new one if it doesn't exist
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="userId">The user ID</param>
        /// <returns>The conversation context</returns>
        Task<ConversationContext> GetOrCreateContextAsync(string conversationId, string userId);
        
        /// <summary>
        /// Adds a user message to the conversation context
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="content">The message content</param>
        /// <returns>The updated conversation context</returns>
        Task<ConversationContext> AddUserMessageAsync(string conversationId, string content);
        
        /// <summary>
        /// Adds an assistant message to the conversation context
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="content">The message content</param>
        /// <returns>The updated conversation context</returns>
        Task<ConversationContext> AddAssistantMessageAsync(string conversationId, string content);
        
        /// <summary>
        /// Updates the conversation context with an intent detection result
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="result">The intent detection result</param>
        /// <returns>The updated conversation context</returns>
        Task<ConversationContext> AddIntentDetectionResultAsync(string conversationId, IntentDetectionResult result);
        
        /// <summary>
        /// Saves the conversation context to persistent storage
        /// </summary>
        /// <param name="context">The conversation context to save</param>
        /// <returns>The saved conversation context</returns>
        Task<ConversationContext> SaveContextAsync(ConversationContext context);
        
        /// <summary>
        /// Prunes old messages from the conversation context if it exceeds the configured maximum
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The pruned conversation context</returns>
        Task<ConversationContext> PruneContextWindowAsync(string conversationId);
        
        /// <summary>
        /// Deletes a conversation context
        /// </summary>
        /// <param name="conversationId">The conversation ID to delete</param>
        /// <returns>True if the context was deleted, false otherwise</returns>
        Task<bool> DeleteContextAsync(string conversationId);
        
        /// <summary>
        /// Generates a summary of the conversation context for use in intent detection and reasoning
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="maxMessageCount">Maximum number of messages to include (newest first). If null, uses the configured default.</param>
        /// <returns>A string summarizing the important context elements</returns>
        Task<string> GenerateContextSummaryAsync(string conversationId, int? maxMessageCount = null);
        
        /// <summary>
        /// Gets recent entities of a specific type from the conversation context
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="entityType">The type of entity to retrieve, or null for all entity types</param>
        /// <param name="maxCount">Maximum number of entities to return (most recent first). Default is 5.</param>
        /// <returns>A list of recent entities</returns>
        Task<List<Entity>> GetRecentEntitiesAsync(string conversationId, string entityType = null, int maxCount = 5);
    }
} 