using System;

namespace SmartInsight.History.Models;

/// <summary>
/// Represents a summary of a conversation session
/// </summary>
public class ConversationSummary
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// ID of the user who participated in the conversation
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Username of the user
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the user
    /// </summary>
    public string? UserDisplayName { get; set; }
    
    /// <summary>
    /// When the conversation started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When the conversation ended (last message)
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Total number of messages in the conversation
    /// </summary>
    public int MessageCount { get; set; }
    
    /// <summary>
    /// Number of successful responses
    /// </summary>
    public int SuccessfulResponses { get; set; }
    
    /// <summary>
    /// Number of failed responses
    /// </summary>
    public int FailedResponses { get; set; }
    
    /// <summary>
    /// Average feedback rating (if available)
    /// </summary>
    public double? AverageFeedbackRating { get; set; }
    
    /// <summary>
    /// The first query in the conversation
    /// </summary>
    public string FirstQuery { get; set; } = string.Empty;
    
    /// <summary>
    /// Duration of the conversation
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
    
    /// <summary>
    /// Whether the conversation is active (recent activity)
    /// </summary>
    public bool IsActive => (DateTime.UtcNow - EndTime).TotalHours < 24;
} 