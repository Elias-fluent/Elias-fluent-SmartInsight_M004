using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Intent
{
    /// <summary>
    /// In-memory implementation of IContextRepository for development and testing
    /// </summary>
    public class InMemoryContextRepository : IContextRepository
    {
        private readonly ConcurrentDictionary<string, ConversationContext> _contexts = new();
        private readonly ILogger<InMemoryContextRepository> _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryContextRepository"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public InMemoryContextRepository(ILogger<InMemoryContextRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc />
        public Task<ConversationContext?> GetContextAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            _contexts.TryGetValue(conversationId, out var context);
            return Task.FromResult(context as ConversationContext);
        }
        
        /// <inheritdoc />
        public Task<bool> SaveContextAsync(ConversationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            _contexts[context.Id] = context;
            _logger.LogDebug("Saved conversation context {ConversationId} to in-memory storage", context.Id);
            
            return Task.FromResult(true);
        }
        
        /// <inheritdoc />
        public Task<bool> DeleteContextAsync(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
            }
            
            bool removed = _contexts.TryRemove(conversationId, out _);
            
            if (removed)
            {
                _logger.LogDebug("Removed conversation context {ConversationId} from in-memory storage", conversationId);
            }
            
            return Task.FromResult(removed);
        }
        
        /// <inheritdoc />
        public Task<int> CleanupOldContextsAsync(int maxAgeDays)
        {
            if (maxAgeDays <= 0)
            {
                throw new ArgumentException("Max age must be greater than zero", nameof(maxAgeDays));
            }
            
            DateTime cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
            
            // Find old contexts
            var oldContextIds = _contexts
                .Where(kv => kv.Value.LastUpdatedAt < cutoffDate)
                .Select(kv => kv.Key)
                .ToList();
            
            // Remove old contexts
            int count = 0;
            foreach (var id in oldContextIds)
            {
                if (_contexts.TryRemove(id, out _))
                {
                    count++;
                }
            }
            
            if (count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old conversation contexts", count);
            }
            
            return Task.FromResult(count);
        }
    }
} 