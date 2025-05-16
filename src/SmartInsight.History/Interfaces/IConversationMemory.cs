using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Core.Entities;
using SmartInsight.History.Models;

namespace SmartInsight.History.Interfaces;

/// <summary>
/// Interface for managing persistent conversation memory
/// </summary>
public interface IConversationMemory
{
    /// <summary>
    /// Creates a new conversation session
    /// </summary>
    /// <param name="userId">User ID who owns the session</param>
    /// <param name="initialContext">Optional initial context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New session ID</returns>
    Task<Guid> CreateSessionAsync(Guid userId, string? initialContext = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a user message to the conversation history
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="query">User's query/message</param>
    /// <param name="conversationContext">Optional context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created conversation log entry</returns>
    Task<ConversationLog> AddUserMessageAsync(
        Guid sessionId, 
        Guid userId, 
        string query,
        string? conversationContext = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a system response to an existing conversation message
    /// </summary>
    /// <param name="conversationLogId">ID of the conversation log to update</param>
    /// <param name="response">System response</param>
    /// <param name="isSuccessful">Whether the response was successful</param>
    /// <param name="errorMessage">Optional error message</param>
    /// <param name="generatedSql">Optional generated SQL</param>
    /// <param name="knowledgeSource">Optional knowledge source</param>
    /// <param name="modelUsed">Optional model information</param>
    /// <param name="tokensUsed">Optional token usage</param>
    /// <param name="processingTimeMs">Processing time in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated conversation log</returns>
    Task<ConversationLog> AddSystemResponseAsync(
        Guid conversationLogId,
        string response,
        bool isSuccessful = true,
        string? errorMessage = null,
        string? generatedSql = null,
        string? knowledgeSource = null,
        string? modelUsed = null,
        int? tokensUsed = null,
        int processingTimeMs = 0,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds user feedback to a conversation message
    /// </summary>
    /// <param name="conversationLogId">ID of the conversation log to update</param>
    /// <param name="rating">User rating (typically 1-5)</param>
    /// <param name="comments">Optional feedback comments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if feedback was added, false if log not found</returns>
    Task<bool> AddUserFeedbackAsync(
        Guid conversationLogId,
        int rating,
        string? comments = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the conversation history for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="limit">Max number of records to return</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="includeContext">Whether to include context data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conversation logs</returns>
    Task<IReadOnlyList<ConversationLog>> GetSessionHistoryAsync(
        Guid sessionId,
        int? limit = null,
        int? skip = null,
        bool includeContext = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a summary of a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session summary or null if not found</returns>
    Task<ConversationSummary?> GetSessionSummaryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a list of session summaries for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Max number of records to return</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of session summaries</returns>
    Task<IReadOnlyList<ConversationSummary>> GetUserSessionsAsync(
        Guid userId,
        int limit = 10,
        int skip = 0,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a summary of a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary text</returns>
    Task<string> SummarizeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Prunes old messages from a conversation session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="maxMessages">Max messages to keep</param>
    /// <param name="summarizePruned">Whether to add a summary of pruned messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of messages pruned</returns>
    Task<int> PruneSessionHistoryAsync(
        Guid sessionId,
        int maxMessages = 100,
        bool summarizePruned = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active conversation sessions
    /// </summary>
    /// <param name="inactivityThreshold">Time threshold for considering a session inactive</param>
    /// <param name="limit">Max number of records to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active session IDs</returns>
    Task<IReadOnlyList<Guid>> GetActiveSessionsAsync(
        TimeSpan inactivityThreshold,
        int limit = 100,
        CancellationToken cancellationToken = default);
} 