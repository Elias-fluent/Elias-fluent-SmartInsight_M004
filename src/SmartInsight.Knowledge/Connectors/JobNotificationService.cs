using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartInsight.Core.Enums;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Models;
using System.Net.Http;
using System.Text;

namespace SmartInsight.Knowledge.Connectors;

/// <summary>
/// Service for sending notifications about job execution
/// </summary>
public class JobNotificationService : IJobNotificationService
{
    private readonly ILogger<JobNotificationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    // In a real implementation, you'd inject an email service here
    
    /// <summary>
    /// Creates a new job notification service
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="httpClientFactory">HTTP client factory for webhook calls</param>
    public JobNotificationService(ILogger<JobNotificationService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }
    
    /// <inheritdoc/>
    public string FormatNotificationMessage(IngestionJobDefinition jobDefinition, IngestionStatus status, string? additionalMessage = null)
    {
        var statusText = status switch
        {
            IngestionStatus.Completed => "completed successfully",
            IngestionStatus.Failed => "failed",
            IngestionStatus.Cancelled => "was cancelled",
            IngestionStatus.Paused => "was paused",
            IngestionStatus.Running => "is currently running",
            _ => status.ToString().ToLower()
        };
        
        var config = ParseNotificationConfig(jobDefinition.NotificationConfigJson);
        var template = config?.MessageTemplate;
        
        if (!string.IsNullOrEmpty(template))
        {
            // Replace placeholders in template
            return template
                .Replace("{jobId}", jobDefinition.Id)
                .Replace("{jobName}", jobDefinition.Name)
                .Replace("{status}", statusText)
                .Replace("{timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                .Replace("{message}", additionalMessage ?? string.Empty);
        }
        
        // Default message format
        var message = $"Ingestion job '{jobDefinition.Name}' (ID: {jobDefinition.Id}) {statusText} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}.";
        
        if (!string.IsNullOrEmpty(additionalMessage))
        {
            message += $"\n\nDetails: {additionalMessage}";
        }
        
        return message;
    }
    
    /// <inheritdoc/>
    public JobNotificationConfig? ParseNotificationConfig(string? jsonConfig)
    {
        if (string.IsNullOrEmpty(jsonConfig))
        {
            return new JobNotificationConfig();
        }
        
        try
        {
            return JsonConvert.DeserializeObject<JobNotificationConfig>(jsonConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse notification config: {Message}", ex.Message);
            return new JobNotificationConfig();
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> SendNotificationAsync(IngestionJobDefinition jobDefinition, IngestionStatus status, string? message = null)
    {
        if (!ShouldSendNotification(jobDefinition, status))
        {
            _logger.LogDebug("No notification configured for job {JobId} with status {Status}", jobDefinition.Id, status);
            return false;
        }
        
        var config = ParseNotificationConfig(jobDefinition.NotificationConfigJson);
        if (config == null)
        {
            _logger.LogWarning("Invalid notification configuration for job {JobId}", jobDefinition.Id);
            return false;
        }
        
        var formattedMessage = FormatNotificationMessage(jobDefinition, status, message);
        bool success = true;
        
        switch (config.Method)
        {
            case NotificationMethod.Email:
                success = await SendEmailNotificationAsync(config, formattedMessage, jobDefinition);
                break;
            case NotificationMethod.Webhook:
                success = await SendWebhookNotificationAsync(config, formattedMessage, jobDefinition);
                break;
            case NotificationMethod.Both:
                var emailResult = await SendEmailNotificationAsync(config, formattedMessage, jobDefinition);
                var webhookResult = await SendWebhookNotificationAsync(config, formattedMessage, jobDefinition);
                success = emailResult && webhookResult;
                break;
        }
        
        return success;
    }
    
    /// <inheritdoc/>
    public bool ShouldSendNotification(IngestionJobDefinition jobDefinition, IngestionStatus status)
    {
        var config = ParseNotificationConfig(jobDefinition.NotificationConfigJson);
        if (config == null)
        {
            return false;
        }
        
        // Check if notifications are enabled for this status
        return status switch
        {
            IngestionStatus.Completed => config.NotifyOnCompletion,
            IngestionStatus.Failed => config.NotifyOnFailure,
            _ => false // By default, don't notify on other statuses
        };
    }
    
    private async Task<bool> SendEmailNotificationAsync(JobNotificationConfig config, string message, IngestionJobDefinition jobDefinition)
    {
        if (config.EmailRecipients == null || !config.EmailRecipients.Any())
        {
            _logger.LogWarning("No email recipients configured for job {JobId}", jobDefinition.Id);
            return false;
        }
        
        try
        {
            // In a real implementation, you would call your email service here
            _logger.LogInformation("Would send email to {Recipients} for job {JobId}: {Message}", 
                string.Join(", ", config.EmailRecipients), jobDefinition.Id, message);
            
            // Simulate email sending
            await Task.Delay(100);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for job {JobId}: {Message}", jobDefinition.Id, ex.Message);
            return false;
        }
    }
    
    private async Task<bool> SendWebhookNotificationAsync(JobNotificationConfig config, string message, IngestionJobDefinition jobDefinition)
    {
        if (config.WebhookUrls == null || !config.WebhookUrls.Any())
        {
            _logger.LogWarning("No webhook URLs configured for job {JobId}", jobDefinition.Id);
            return false;
        }
        
        try
        {
            using var client = _httpClientFactory.CreateClient("JobNotifications");
            
            var payload = new
            {
                jobId = jobDefinition.Id,
                jobName = jobDefinition.Name,
                tenantId = jobDefinition.TenantId,
                status = jobDefinition.Status.ToString(),
                timestamp = DateTime.UtcNow,
                message = message
            };
            
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            
            var tasks = config.WebhookUrls.Select(async url =>
            {
                try
                {
                    var response = await client.PostAsync(url, content);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send webhook to {Url} for job {JobId}: {Message}", 
                        url, jobDefinition.Id, ex.Message);
                    return false;
                }
            });
            
            var results = await Task.WhenAll(tasks);
            return results.All(r => r); // All webhooks must succeed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notifications for job {JobId}: {Message}", jobDefinition.Id, ex.Message);
            return false;
        }
    }
} 