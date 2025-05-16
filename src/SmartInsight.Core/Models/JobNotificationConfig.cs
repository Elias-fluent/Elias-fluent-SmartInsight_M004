using System.Text.Json.Serialization;

namespace SmartInsight.Core.Models;

/// <summary>
/// Configuration for job notifications
/// </summary>
public class JobNotificationConfig
{
    /// <summary>
    /// Whether to send notifications on job completion
    /// </summary>
    public bool NotifyOnCompletion { get; set; } = false;
    
    /// <summary>
    /// Whether to send notifications on job failure
    /// </summary>
    public bool NotifyOnFailure { get; set; } = true;
    
    /// <summary>
    /// Email addresses to notify
    /// </summary>
    public List<string> EmailRecipients { get; set; } = new List<string>();
    
    /// <summary>
    /// Webhook URLs to notify
    /// </summary>
    public List<string> WebhookUrls { get; set; } = new List<string>();
    
    /// <summary>
    /// Custom message template for notifications
    /// </summary>
    public string? MessageTemplate { get; set; }
    
    /// <summary>
    /// Notification delivery method
    /// </summary>
    public NotificationMethod Method { get; set; } = NotificationMethod.Email;
}

/// <summary>
/// Method of notification delivery
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationMethod
{
    /// <summary>
    /// Notification via email
    /// </summary>
    Email,
    
    /// <summary>
    /// Notification via webhook
    /// </summary>
    Webhook,
    
    /// <summary>
    /// Notification via both email and webhook
    /// </summary>
    Both
} 