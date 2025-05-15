using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.Options;

namespace SmartInsight.AI.Intent
{
    /// <summary>
    /// Service for managing conversation contexts
    /// </summary>
    public class ContextManager : IContextManager
    {
        private readonly ILogger<ContextManager> _logger;
        private readonly ContextManagerOptions _options;
        private readonly ConcurrentDictionary<string, ConversationContext> _contextCache;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextManager"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="options">The context manager options</param>
        public ContextManager(
            ILogger<ContextManager> logger,
            IOptions<ContextManagerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _contextCache = new ConcurrentDictionary<string, ConversationContext>();
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> GetOrCreateContextAsync(string conversationId, string userId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }
            
            // Try to get from cache first
            if (_contextCache.TryGetValue(conversationId, out var cachedContext))
            {
                _logger.LogDebug("Retrieved conversation context {ConversationId} from cache", conversationId);
                return cachedContext;
            }
            
            // TODO: Try to get from persistent storage when implemented
            // var storedContext = await _repository.GetContextAsync(conversationId);
            // if (storedContext != null)
            // {
            //     _contextCache.TryAdd(conversationId, storedContext);
            //     _logger.LogDebug("Retrieved conversation context {ConversationId} from storage", conversationId);
            //     return storedContext;
            // }
            
            // Create new context if not found
            var newContext = new ConversationContext
            {
                Id = conversationId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            
            _contextCache.TryAdd(conversationId, newContext);
            _logger.LogInformation("Created new conversation context {ConversationId} for user {UserId}", conversationId, userId);
            
            return newContext;
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> AddUserMessageAsync(string conversationId, string content)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Message content cannot be null or empty", nameof(content));
            }
            
            var context = await GetContextAsync(conversationId);
            
            context.AddMessage("user", content);
            context.CurrentQuery = content;
            context.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogDebug("Added user message to conversation {ConversationId}", conversationId);
            
            if (_options.AutoPruneMessages)
            {
                await PruneContextWindowAsync(conversationId);
            }
            
            await SaveContextAsync(context);
            
            return context;
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> AddAssistantMessageAsync(string conversationId, string content)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Message content cannot be null or empty", nameof(content));
            }
            
            var context = await GetContextAsync(conversationId);
            
            context.AddMessage("assistant", content);
            context.LastUpdatedAt = DateTime.UtcNow;
            
            _logger.LogDebug("Added assistant message to conversation {ConversationId}", conversationId);
            
            if (_options.AutoPruneMessages)
            {
                await PruneContextWindowAsync(conversationId);
            }
            
            await SaveContextAsync(context);
            
            return context;
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> AddIntentDetectionResultAsync(string conversationId, IntentDetectionResult result)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            
            var context = await GetContextAsync(conversationId);
            
            // Add the detected intent
            var detectedIntent = new DetectedIntent
            {
                Intent = result.Intent,
                Confidence = result.Confidence,
                DetectedAt = DateTime.UtcNow,
                Query = context.CurrentQuery,
                Entities = result.Entities
            };
            
            context.AddDetectedIntent(detectedIntent);
            
            // Add any entities that were detected
            foreach (var entity in result.Entities)
            {
                var trackedEntity = new TrackedEntity
                {
                    Entity = entity,
                    FirstMentionedAt = DateTime.UtcNow,
                    LastMentionedAt = DateTime.UtcNow,
                    MentionCount = 1
                };
                
                context.AddTrackedEntity(trackedEntity);
            }
            
            _logger.LogDebug("Added intent detection result for intent {Intent} to conversation {ConversationId}", 
                result.Intent, conversationId);
            
            // Prune entities if we have too many
            if (context.TrackedEntities.Count > _options.MaxTrackedEntities)
            {
                // Remove the oldest and least mentioned entities
                context.TrackedEntities.Sort((a, b) => 
                {
                    // First compare by mention count
                    int mentionComparison = a.MentionCount.CompareTo(b.MentionCount);
                    if (mentionComparison != 0)
                    {
                        return mentionComparison;
                    }
                    
                    // If tie, compare by last mentioned time
                    return a.LastMentionedAt.CompareTo(b.LastMentionedAt);
                });
                
                while (context.TrackedEntities.Count > _options.MaxTrackedEntities)
                {
                    context.TrackedEntities.RemoveAt(0);
                }
                
                _logger.LogDebug("Pruned tracked entities for conversation {ConversationId}", conversationId);
            }
            
            // Prune intents if we have too many
            if (context.DetectedIntents.Count > _options.MaxStoredIntents)
            {
                // Keep only the most recent intents
                context.DetectedIntents.Sort((a, b) => b.DetectedAt.CompareTo(a.DetectedAt));
                context.DetectedIntents.RemoveRange(_options.MaxStoredIntents, 
                    context.DetectedIntents.Count - _options.MaxStoredIntents);
                
                _logger.LogDebug("Pruned detected intents for conversation {ConversationId}", conversationId);
            }
            
            await SaveContextAsync(context);
            
            return context;
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> SaveContextAsync(ConversationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            // Update in memory cache
            _contextCache.AddOrUpdate(context.Id, context, (key, oldValue) => context);
            
            // TODO: Save to persistent storage when implemented
            // if (_options.PersistConversations)
            // {
            //     await _repository.SaveContextAsync(context);
            //     _logger.LogDebug("Saved conversation context {ConversationId} to storage", context.Id);
            // }
            
            return context;
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> PruneContextWindowAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            var context = await GetContextAsync(conversationId);
            
            if (context.Messages.Count > _options.MaxMessageHistory)
            {
                int excessCount = context.Messages.Count - _options.MaxMessageHistory;
                context.Messages.RemoveRange(0, excessCount);
                
                _logger.LogDebug("Pruned {Count} messages from conversation {ConversationId}", 
                    excessCount, conversationId);
            }
            
            await SaveContextAsync(context);
            
            return context;
        }
        
        /// <inheritdoc />
        public async Task<bool> DeleteContextAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            bool removedFromCache = _contextCache.TryRemove(conversationId, out _);
            
            // TODO: Remove from persistent storage when implemented
            // bool removedFromStorage = false;
            // if (_options.PersistConversations)
            // {
            //     removedFromStorage = await _repository.DeleteContextAsync(conversationId);
            // }
            
            _logger.LogInformation("Deleted conversation context {ConversationId}", conversationId);
            
            // Return true if removed from either cache or storage
            return removedFromCache; // || removedFromStorage;
        }
        
        /// <summary>
        /// Gets an existing conversation context
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The conversation context</returns>
        /// <exception cref="InvalidOperationException">Thrown when the context does not exist</exception>
        private async Task<ConversationContext> GetContextAsync(string conversationId)
        {
            if (_contextCache.TryGetValue(conversationId, out var cachedContext))
            {
                return cachedContext;
            }
            
            // TODO: Try to get from persistent storage when implemented
            // var storedContext = await _repository.GetContextAsync(conversationId);
            // if (storedContext != null)
            // {
            //     _contextCache.TryAdd(conversationId, storedContext);
            //     return storedContext;
            // }
            
            throw new InvalidOperationException($"No conversation context found for ID {conversationId}");
        }
    }
} 