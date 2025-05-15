using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.Options;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace SmartInsight.AI.Intent
{
    /// <summary>
    /// Service for managing conversation contexts
    /// </summary>
    public class ContextManager : IContextManager, IDisposable
    {
        private readonly ILogger<ContextManager> _logger;
        private readonly ContextManagerOptions _options;
        private readonly ConcurrentDictionary<string, ConversationContext> _contextCache;
        private readonly IContextRepository _repository;
        private readonly Timer _cleanupTimer;
        private bool _isDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextManager"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="options">The context manager options</param>
        /// <param name="repository">The context repository for persistence</param>
        public ContextManager(
            ILogger<ContextManager> logger,
            IOptions<ContextManagerOptions> options,
            IContextRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _contextCache = new ConcurrentDictionary<string, ConversationContext>();
            
            // Set up periodic cleanup timer if persistence is enabled
            if (_options.PersistConversations)
            {
                // Run cleanup every 4 hours
                TimeSpan cleanupInterval = TimeSpan.FromHours(4);
                _cleanupTimer = new Timer(CleanupOldContexts, null, cleanupInterval, cleanupInterval);
                _logger.LogInformation("Scheduled context cleanup to run every {Hours} hours", cleanupInterval.TotalHours);
            }
            else
            {
                _cleanupTimer = null;
            }
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
            
            // Try to get from persistent storage if enabled
            if (_options.PersistConversations)
            {
                var storedContext = await _repository.GetContextAsync(conversationId);
                if (storedContext != null)
                {
                    // Add to cache
                    _contextCache.TryAdd(conversationId, storedContext);
                    _logger.LogDebug("Retrieved conversation context {ConversationId} from storage", conversationId);
                    return storedContext;
                }
            }
            
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
            
            // Persist immediately if enabled
            if (_options.PersistConversations)
            {
                await _repository.SaveContextAsync(newContext);
            }
            
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
            
            // Save to persistent storage if enabled
            if (_options.PersistConversations)
            {
                await _repository.SaveContextAsync(context);
                _logger.LogDebug("Saved conversation context {ConversationId} to storage", context.Id);
            }
            
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
            
            // Remove from cache
            bool removedFromCache = _contextCache.TryRemove(conversationId, out _);
            
            // Remove from persistent storage if enabled
            bool removedFromStorage = false;
            if (_options.PersistConversations)
            {
                removedFromStorage = await _repository.DeleteContextAsync(conversationId);
            }
            
            _logger.LogInformation("Deleted conversation context {ConversationId}", conversationId);
            
            // Return true if removed from either cache or storage
            return removedFromCache || removedFromStorage;
        }
        
        /// <inheritdoc />
        public async Task<ConversationContext> GetContextAsync(string conversationId)
        {
            // Check in-memory cache first
            if (_contextCache.TryGetValue(conversationId, out var cachedContext))
            {
                return cachedContext;
            }
            
            // Try to get from persistent storage if enabled
            if (_options.PersistConversations)
            {
                var storedContext = await _repository.GetContextAsync(conversationId);
                if (storedContext != null)
                {
                    _contextCache.TryAdd(conversationId, storedContext);
                    return storedContext;
                }
            }
            
            throw new InvalidOperationException($"No conversation context found for ID {conversationId}");
        }
        
        /// <summary>
        /// Cleanup old contexts from storage
        /// </summary>
        private async void CleanupOldContexts(object state)
        {
            try
            {
                if (!_options.PersistConversations)
                {
                    return;
                }
                
                _logger.LogInformation("Running scheduled cleanup of old conversation contexts");
                
                int count = await _repository.CleanupOldContextsAsync(_options.MaxConversationAgeDays);
                
                if (count > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old contexts during scheduled cleanup", count);
                }
                else
                {
                    _logger.LogDebug("No old contexts found during scheduled cleanup");
                }
                
                // Also remove old contexts from memory cache
                DateTime cutoffDate = DateTime.UtcNow.AddHours(-_options.ContextCacheTimeHours);
                
                foreach (var kv in _contextCache)
                {
                    if (kv.Value.LastUpdatedAt < cutoffDate)
                    {
                        _contextCache.TryRemove(kv.Key, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled cleanup of old contexts");
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the context manager
        /// </summary>
        /// <param name="disposing">Whether this is being called from Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            
            if (disposing)
            {
                _cleanupTimer?.Dispose();
            }
            
            _isDisposed = true;
        }
        
        /// <inheritdoc />
        public async Task<string> GenerateContextSummaryAsync(string conversationId, int? maxMessageCount = null)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            var context = await GetContextAsync(conversationId);
            
            // Determine how many messages to include
            int messagesToInclude = maxMessageCount ?? _options.MaxMessageHistory;
            
            // Get messages, most recent first, up to the limit
            var messages = context.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(messagesToInclude)
                .OrderBy(m => m.Timestamp) // Re-order to chronological for the output
                .ToList();
            
            // Get key entities
            var keyEntities = context.TrackedEntities
                .Where(e => e.IsKeyEntity)
                .OrderByDescending(e => e.RelevanceScore)
                .Take(5)
                .ToList();
            
            // Get recent intents
            var recentIntents = context.DetectedIntents
                .OrderByDescending(i => i.DetectedAt)
                .Take(3)
                .ToList();

            // Build the context summary
            var summary = new System.Text.StringBuilder();
            
            // Add conversation history
            summary.AppendLine("Conversation History:");
            foreach (var message in messages)
            {
                summary.AppendLine($"{message.Role}: {message.Content}");
            }
            
            // Add key entities if available
            if (keyEntities.Any())
            {
                summary.AppendLine("\nKey Entities in Conversation:");
                foreach (var entity in keyEntities)
                {
                    summary.AppendLine($"- {entity.Entity.Type}: {entity.Entity.Value} (Mentioned {entity.MentionCount} times)");
                }
            }
            
            // Add recent intents if available
            if (recentIntents.Any())
            {
                summary.AppendLine("\nRecent Intents:");
                foreach (var intent in recentIntents)
                {
                    summary.AppendLine($"- {intent.Intent} (Confidence: {intent.Confidence:F2})");
                }
            }
            
            _logger.LogDebug("Generated context summary for conversation {ConversationId} with {MessageCount} messages", 
                conversationId, messages.Count);
            
            return summary.ToString();
        }
        
        /// <inheritdoc />
        public async Task<List<Entity>> GetRecentEntitiesAsync(string conversationId, string entityType = null, int maxCount = 5)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            if (maxCount <= 0)
            {
                throw new ArgumentException("Max count must be greater than zero", nameof(maxCount));
            }
            
            var context = await GetContextAsync(conversationId);
            
            // Filter entities
            var filteredEntities = context.TrackedEntities
                .Where(e => string.IsNullOrEmpty(entityType) || e.Entity.Type.Equals(entityType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.LastMentionedAt)
                .Take(maxCount)
                .Select(e => e.Entity)
                .ToList();
            
            _logger.LogDebug("Retrieved {Count} recent entities of type {EntityType} for conversation {ConversationId}", 
                filteredEntities.Count, entityType ?? "all", conversationId);
            
            return filteredEntities;
        }
    }
} 