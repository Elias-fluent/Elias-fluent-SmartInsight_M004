using System;

namespace SmartInsight.AI.Options
{
    /// <summary>
    /// Options for configuring the ContextManager service
    /// </summary>
    public class ContextManagerOptions
    {
        /// <summary>
        /// Maximum number of messages to keep in the conversation history
        /// </summary>
        public int MaxMessageHistory { get; set; } = 50;
        
        /// <summary>
        /// Maximum number of entities to track per conversation
        /// </summary>
        public int MaxTrackedEntities { get; set; } = 30;
        
        /// <summary>
        /// Maximum number of intents to store per conversation
        /// </summary>
        public int MaxStoredIntents { get; set; } = 20;
        
        /// <summary>
        /// Whether to automatically prune messages when adding new ones
        /// </summary>
        public bool AutoPruneMessages { get; set; } = true;
        
        /// <summary>
        /// Maximum age of conversation contexts to keep in memory cache (in hours)
        /// </summary>
        public double ContextCacheTimeHours { get; set; } = 2.0;
        
        /// <summary>
        /// Whether to persist conversations to storage
        /// </summary>
        public bool PersistConversations { get; set; } = true;
        
        /// <summary>
        /// Maximum age of conversations to keep in storage (in days)
        /// </summary>
        public int MaxConversationAgeDays { get; set; } = 30;
    }
} 