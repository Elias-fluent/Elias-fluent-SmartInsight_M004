using SmartInsight.Core.Enums;
using SmartInsight.Core.Models;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Service for sending notifications about job execution
/// </summary>
public interface IJobNotificationService
{
    /// <summary>
    /// Sends a notification about a job execution
    /// </summary>
    /// <param name="jobDefinition">The job that was executed</param>
    /// <param name="status">The current status of the job</param>
    /// <param name="message">Additional message details</param>
    /// <returns>Whether the notification was sent successfully</returns>
    Task<bool> SendNotificationAsync(IngestionJobDefinition jobDefinition, IngestionStatus status, string? message = null);
    
    /// <summary>
    /// Parse notification configuration from JSON
    /// </summary>
    /// <param name="jsonConfig">The JSON configuration</param>
    /// <returns>Parsed notification configuration</returns>
    JobNotificationConfig? ParseNotificationConfig(string? jsonConfig);
    
    /// <summary>
    /// Check if a notification should be sent based on job status and configuration
    /// </summary>
    /// <param name="jobDefinition">The job definition</param>
    /// <param name="status">The current status</param>
    /// <returns>Whether a notification should be sent</returns>
    bool ShouldSendNotification(IngestionJobDefinition jobDefinition, IngestionStatus status);
    
    /// <summary>
    /// Creates a formatted notification message
    /// </summary>
    /// <param name="jobDefinition">The job definition</param>
    /// <param name="status">The current status</param>
    /// <param name="additionalMessage">Additional message details</param>
    /// <returns>Formatted notification message</returns>
    string FormatNotificationMessage(IngestionJobDefinition jobDefinition, IngestionStatus status, string? additionalMessage = null);
} 