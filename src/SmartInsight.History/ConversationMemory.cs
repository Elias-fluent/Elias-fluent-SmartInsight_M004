using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using SmartInsight.History.Interfaces;
using SmartInsight.History.Models;

namespace SmartInsight.History;

/// <summary>
/// Manages persistent conversation memory with support for
/// storage, retrieval, and pruning of conversation history
/// </summary>
public class ConversationMemory : IConversationMemory
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly ILogger<ConversationMemory> _logger;
    
    /// <summary>
    /// Creates a new conversation memory manager
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="tenantAccessor">Tenant accessor for tenant isolation</param>
    /// <param name="logger">Logger</param>
    public ConversationMemory(
        ApplicationDbContext dbContext,
        ITenantAccessor tenantAccessor,
        ILogger<ConversationMemory> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc />
    public async Task<Guid> CreateSessionAsync(Guid userId, string? initialContext = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId() ?? 
            throw new InvalidOperationException("Tenant ID is required for conversation operations");
            
        var sessionId = Guid.NewGuid();
        
        _logger.LogInformation("Creating new conversation session {SessionId} for user {UserId}", sessionId, userId);
        
        // We don't need to add any messages yet - just return the new session ID
        // The first message will be added when the user sends their first query
        
        if (!string.IsNullOrEmpty(initialContext))
        {
            // If initial context was provided, create a system context message
            await AddSystemContextAsync(sessionId, userId, initialContext, cancellationToken);
        }
        
        return sessionId;
    }
    
    /// <inheritdoc />
    public async Task<ConversationLog> AddUserMessageAsync(
        Guid sessionId, 
        Guid userId, 
        string query, 
        string? conversationContext = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId() ?? 
            throw new InvalidOperationException("Tenant ID is required for conversation operations");
            
        // Get the next sequence number for this session
        var sequenceNumber = await GetNextSequenceNumberAsync(sessionId, cancellationToken);
        
        var conversationLog = new ConversationLog
        {
            SessionId = sessionId,
            UserId = userId,
            TenantId = tenantId,
            Query = query,
            ConversationContext = conversationContext,
            QueryTimestamp = DateTime.UtcNow,
            SequenceNumber = sequenceNumber
        };
        
        _dbContext.ConversationLogs.Add(conversationLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug(
            "Added user message to conversation {SessionId} with sequence number {SequenceNumber}", 
            sessionId, sequenceNumber);
            
        return conversationLog;
    }
    
    /// <inheritdoc />
    public async Task<ConversationLog> AddSystemResponseAsync(
        Guid conversationLogId,
        string response,
        bool isSuccessful = true,
        string? errorMessage = null,
        string? generatedSql = null,
        string? knowledgeSource = null,
        string? modelUsed = null,
        int? tokensUsed = null,
        int processingTimeMs = 0,
        CancellationToken cancellationToken = default)
    {
        var conversationLog = await _dbContext.ConversationLogs.FindAsync(
            new object[] { conversationLogId }, 
            cancellationToken);
            
        if (conversationLog == null)
        {
            throw new KeyNotFoundException($"Conversation log with ID {conversationLogId} not found");
        }
        
        conversationLog.Response = response;
        conversationLog.IsSuccessful = isSuccessful;
        conversationLog.ErrorMessage = errorMessage;
        conversationLog.GeneratedSql = generatedSql;
        conversationLog.KnowledgeSource = knowledgeSource;
        conversationLog.ModelUsed = modelUsed;
        conversationLog.TokensUsed = tokensUsed;
        conversationLog.ProcessingTimeMs = processingTimeMs;
        conversationLog.ResponseTimestamp = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug(
            "Added system response to conversation log {ConversationLogId} for session {SessionId}", 
            conversationLogId, conversationLog.SessionId);
            
        return conversationLog;
    }
    
    /// <inheritdoc />
    public async Task<bool> AddUserFeedbackAsync(
        Guid conversationLogId,
        int rating,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        var conversationLog = await _dbContext.ConversationLogs.FindAsync(
            new object[] { conversationLogId }, 
            cancellationToken);
            
        if (conversationLog == null)
        {
            _logger.LogWarning("Failed to add feedback: conversation log {ConversationLogId} not found", conversationLogId);
            return false;
        }
        
        conversationLog.FeedbackRating = rating;
        conversationLog.FeedbackComments = comments;
        conversationLog.FeedbackTimestamp = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug(
            "Added user feedback (rating: {Rating}) to conversation log {ConversationLogId}", 
            rating, conversationLogId);
            
        return true;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationLog>> GetSessionHistoryAsync(
        Guid sessionId,
        int? limit = null,
        int? skip = null,
        bool includeContext = false,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        IQueryable<ConversationLog> query = _dbContext.ConversationLogs
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.SequenceNumber);
            
        // Apply tenant filter if in a multi-tenant context
        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }
        
        // Skip records if specified
        if (skip.HasValue && skip.Value > 0)
        {
            query = query.Skip(skip.Value);
        }
        
        // Limit the number of records if specified
        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }
        
        // Exclude context if not requested (to reduce payload size)
        if (!includeContext)
        {
            query = query.Select(c => new ConversationLog
            {
                Id = c.Id,
                SessionId = c.SessionId,
                UserId = c.UserId,
                TenantId = c.TenantId,
                Query = c.Query,
                Response = c.Response,
                QueryTimestamp = c.QueryTimestamp,
                ResponseTimestamp = c.ResponseTimestamp,
                ProcessingTimeMs = c.ProcessingTimeMs,
                FeedbackRating = c.FeedbackRating,
                FeedbackComments = c.FeedbackComments,
                FeedbackTimestamp = c.FeedbackTimestamp,
                IsSuccessful = c.IsSuccessful,
                ErrorMessage = c.ErrorMessage,
                SequenceNumber = c.SequenceNumber,
                KnowledgeSource = c.KnowledgeSource,
                ModelUsed = c.ModelUsed,
                TokensUsed = c.TokensUsed,
                Tags = c.Tags
            });
        }
        
        var result = await query.ToListAsync(cancellationToken);
        
        _logger.LogDebug(
            "Retrieved {Count} conversation logs for session {SessionId}", 
            result.Count, sessionId);
            
        return result;
    }
    
    /// <inheritdoc />
    public async Task<ConversationSummary?> GetSessionSummaryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        var query = _dbContext.ConversationLogs
            .Where(c => c.SessionId == sessionId);
            
        // Apply tenant filter if in a multi-tenant context
        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }
        
        // Ensure we have at least one message in the conversation
        if (!await query.AnyAsync(cancellationToken))
        {
            return null;
        }
        
        // Get the first and last message timestamps
        var firstMessage = await query
            .OrderBy(c => c.QueryTimestamp)
            .FirstAsync(cancellationToken);
            
        var lastMessage = await query
            .OrderByDescending(c => c.QueryTimestamp)
            .FirstAsync(cancellationToken);
            
        // Count messages, successful and failed responses
        var messageCount = await query.CountAsync(cancellationToken);
        var successfulResponses = await query.CountAsync(c => c.IsSuccessful, cancellationToken);
        var failedResponses = await query.CountAsync(c => !c.IsSuccessful, cancellationToken);
        
        // Get average feedback rating if available
        var averageFeedback = await query
            .Where(c => c.FeedbackRating != null)
            .Select(c => c.FeedbackRating!.Value)
            .DefaultIfEmpty()
            .AverageAsync(cancellationToken);
            
        // Get user info
        var userId = firstMessage.UserId;
        var user = await _dbContext.LegacyUsers
            .Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);
            
        var summary = new ConversationSummary
        {
            SessionId = sessionId,
            UserId = userId,
            UserName = user?.Username ?? "Unknown",
            UserDisplayName = user?.DisplayName,
            StartTime = firstMessage.QueryTimestamp,
            EndTime = lastMessage.ResponseTimestamp ?? lastMessage.QueryTimestamp,
            MessageCount = messageCount,
            SuccessfulResponses = successfulResponses,
            FailedResponses = failedResponses,
            AverageFeedbackRating = averageFeedback > 0 ? averageFeedback : null,
            FirstQuery = firstMessage.Query
        };
        
        return summary;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationSummary>> GetUserSessionsAsync(
        Guid userId,
        int limit = 10,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        // Get distinct session IDs for the user, ordered by most recent activity
        var sessionIdsQuery = _dbContext.ConversationLogs
            .Where(c => c.UserId == userId)
            .GroupBy(c => c.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                LastActivity = g.Max(c => c.QueryTimestamp)
            })
            .OrderByDescending(x => x.LastActivity)
            .Skip(skip)
            .Take(limit);
            
        // Apply tenant filter if in a multi-tenant context
        if (tenantId.HasValue)
        {
            sessionIdsQuery = sessionIdsQuery.Where(x => 
                _dbContext.ConversationLogs
                    .Any(c => c.SessionId == x.SessionId && c.TenantId == tenantId.Value));
        }
        
        var sessionIds = await sessionIdsQuery
            .Select(x => x.SessionId)
            .ToListAsync(cancellationToken);
            
        var result = new List<ConversationSummary>();
        
        // Get summary for each session
        foreach (var sessionId in sessionIds)
        {
            var summary = await GetSessionSummaryAsync(sessionId, cancellationToken);
            if (summary != null)
            {
                result.Add(summary);
            }
        }
        
        return result;
    }
    
    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        var query = _dbContext.ConversationLogs
            .Where(c => c.SessionId == sessionId);
            
        // Apply tenant filter if in a multi-tenant context
        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }
        
        var messages = await query.ToListAsync(cancellationToken);
        
        if (!messages.Any())
        {
            _logger.LogWarning("No messages found for session {SessionId} to delete", sessionId);
            return false;
        }
        
        _dbContext.ConversationLogs.RemoveRange(messages);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Deleted {Count} messages for conversation session {SessionId}", 
            messages.Count, sessionId);
            
        return true;
    }
    
    /// <inheritdoc />
    public async Task<string> SummarizeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        // This is a placeholder. In a real implementation, you would:
        // 1. Retrieve the conversation history
        // 2. Use an LLM to generate a summary
        // 3. Update the conversation context with the summary
        
        var history = await GetSessionHistoryAsync(sessionId, includeContext: true, cancellationToken: cancellationToken);
        
        if (history.Count == 0)
        {
            return "No conversation history found.";
        }
        
        // For now, we'll just return a simple summary based on the data we have
        var duration = history.Last().QueryTimestamp - history.First().QueryTimestamp;
        var userQueries = history.Count;
        var systemResponses = history.Count(m => m.Response != null);
        
        return $"Conversation with {userQueries} messages over {duration.TotalMinutes:F1} minutes. {systemResponses} system responses.";
    }
    
    /// <inheritdoc />
    public async Task<int> PruneSessionHistoryAsync(
        Guid sessionId,
        int maxMessages = 100,
        bool summarizePruned = true,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        IQueryable<ConversationLog> query = _dbContext.ConversationLogs
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.SequenceNumber);
            
        // Apply tenant filter if in a multi-tenant context
        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }
        
        var totalMessages = await query.CountAsync(cancellationToken);
        
        // If we don't have too many messages, no need to prune
        if (totalMessages <= maxMessages)
        {
            return 0;
        }
        
        // Calculate how many messages to keep and remove
        var messagesToRemove = totalMessages - maxMessages;
        
        if (summarizePruned)
        {
            // Get the oldest messages that will be pruned
            var oldestMessages = await query
                .Take(messagesToRemove)
                .ToListAsync(cancellationToken);
                
            // Create a simple summary of the pruned messages
            var firstTimestamp = oldestMessages.First().QueryTimestamp;
            var lastTimestamp = oldestMessages.Last().QueryTimestamp;
            var duration = lastTimestamp - firstTimestamp;
            
            var summary = $"[Pruned {messagesToRemove} older messages from {firstTimestamp:g} to {lastTimestamp:g}, spanning {duration.TotalMinutes:F1} minutes]";
            
            // Add the summary as a system context message
            var user = await _dbContext.LegacyUsers
                .FirstOrDefaultAsync(u => u.Id == oldestMessages.First().UserId, cancellationToken);
                
            if (user != null)
            {
                await AddSystemContextAsync(sessionId, user.Id, summary, cancellationToken);
            }
        }
        
        // Delete the oldest messages
        var messagesToDelete = await query
            .Take(messagesToRemove)
            .ToListAsync(cancellationToken);
            
        _dbContext.ConversationLogs.RemoveRange(messagesToDelete);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Pruned {Count} oldest messages from conversation session {SessionId}", 
            messagesToDelete.Count, sessionId);
            
        return messagesToDelete.Count;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveSessionsAsync(
        TimeSpan inactivityThreshold,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        var cutoffTime = DateTime.UtcNow.Subtract(inactivityThreshold);
        
        var query = _dbContext.ConversationLogs
            .Where(c => c.QueryTimestamp >= cutoffTime)
            .GroupBy(c => c.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                LastActivity = g.Max(c => c.QueryTimestamp)
            })
            .OrderByDescending(x => x.LastActivity)
            .Take(limit);
            
        // Apply tenant filter if in a multi-tenant context
        if (tenantId.HasValue)
        {
            query = query.Where(x => 
                _dbContext.ConversationLogs
                    .Any(c => c.SessionId == x.SessionId && c.TenantId == tenantId.Value));
        }
        
        var activeSessions = await query
            .Select(x => x.SessionId)
            .ToListAsync(cancellationToken);
            
        return activeSessions;
    }
    
    /// <summary>
    /// Gets the next sequence number for a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next sequence number</returns>
    private async Task<int> GetNextSequenceNumberAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var maxSequence = await _dbContext.ConversationLogs
            .Where(c => c.SessionId == sessionId)
            .Select(c => (int?)c.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;
            
        return maxSequence + 1;
    }
    
    /// <summary>
    /// Adds a system context message to the conversation history
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="contextMessage">Context message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created conversation log</returns>
    private async Task<ConversationLog> AddSystemContextAsync(
        Guid sessionId, 
        Guid userId, 
        string contextMessage,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId() ?? 
            throw new InvalidOperationException("Tenant ID is required for conversation operations");
            
        // Get the next sequence number for this session
        var sequenceNumber = await GetNextSequenceNumberAsync(sessionId, cancellationToken);
        
        var conversationLog = new ConversationLog
        {
            SessionId = sessionId,
            UserId = userId,
            TenantId = tenantId,
            Query = "[SYSTEM CONTEXT]",
            Response = contextMessage,
            ConversationContext = JsonSerializer.Serialize(new { type = "system_context" }),
            QueryTimestamp = DateTime.UtcNow,
            ResponseTimestamp = DateTime.UtcNow,
            SequenceNumber = sequenceNumber,
            IsSuccessful = true,
            Tags = "system,context"
        };
        
        _dbContext.ConversationLogs.Add(conversationLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug(
            "Added system context to conversation {SessionId} with sequence number {SequenceNumber}", 
            sessionId, sequenceNumber);
            
        return conversationLog;
    }
} 