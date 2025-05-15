using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.AI.Models;
using SmartInsight.Telemetry.Interfaces;
using SmartInsight.Telemetry.Models;
using SmartInsight.Telemetry.Options;
using SmartInsight.Telemetry.Repositories;

namespace SmartInsight.Telemetry.Services
{
    /// <summary>
    /// Service for capturing AI telemetry and metrics for training and improvement
    /// </summary>
    public class TelemetryService : ITelemetryService
    {
        private readonly ITelemetryRepository _repository;
        private readonly ILogger<TelemetryService> _logger;
        private readonly TelemetryOptions _options;
        private readonly string _appVersion;
        
        private static readonly HashSet<string> SensitiveFields = new HashSet<string>
        {
            "password", "token", "secret", "key", "credential", "credentials", "auth"
        };
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryService"/> class
        /// </summary>
        public TelemetryService(
            ITelemetryRepository repository,
            ILogger<TelemetryService> logger,
            IOptions<TelemetryOptions> options)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new TelemetryOptions();
            _appVersion = GetAppVersion();
        }
        
        /// <inheritdoc />
        public async Task<string> LogIntentDetectionAsync(
            string query,
            IntentDetectionResult result,
            bool usedContext,
            long processingTimeMs,
            string userId,
            string conversationId,
            string modelName,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || !IsEventTypeEnabled(EventTypes.IntentDetection))
            {
                return string.Empty;
            }
            
            try
            {
                var telemetryEvent = new IntentDetectionEvent
                {
                    EventType = "IntentDetection",
                    Query = SanitizeText(query),
                    Intent = result?.Intent ?? "unknown",
                    Confidence = result?.Confidence ?? 0,
                    UsedContext = usedContext,
                    ProcessingTimeMs = processingTimeMs,
                    ModelName = modelName,
                    UserId = AnonymizeUserIdIfNeeded(userId),
                    ConversationId = conversationId,
                    AppVersion = _appVersion,
                    SourceComponent = "SmartInsight.AI.IntentDetector",
                    SelfVerified = false // Currently not tracked but could be in future
                };
                
                // Add entities from the result
                if (result?.Entities != null)
                {
                    foreach (var entity in result.Entities)
                    {
                        telemetryEvent.ExtractedEntities.Add(new EntityInfo
                        {
                            Type = entity.Type,
                            Value = SanitizeText(entity.Value),
                            Confidence = entity.Confidence
                        });
                    }
                }
                
                // Extract fallback info if it exists
                var fallbackLevel = result?.Entities?.FirstOrDefault(e => e.Type == "fallback_level");
                if (fallbackLevel != null)
                {
                    telemetryEvent.Fallback = new FallbackInfo
                    {
                        Level = fallbackLevel.Value,
                        WasNeeded = true,
                        WasSuccessful = true, // Assume successful since we got a result
                        Reason = result?.Entities?.FirstOrDefault(e => e.Type == "fallback_reason")?.Value ?? "Low confidence"
                    };
                }
                
                await _repository.StoreEventAsync(telemetryEvent, cancellationToken);
                
                // Log low confidence detections to help improve the model
                if (result != null && result.Confidence < _options.MisclassificationThreshold)
                {
                    _logger.LogWarning(
                        "Low confidence intent detection: Query: {Query}, Intent: {Intent}, Confidence: {Confidence}",
                        SanitizeText(query), result.Intent, result.Confidence);
                }
                
                return telemetryEvent.EventId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging intent detection event");
                return string.Empty;
            }
        }
        
        /// <inheritdoc />
        public async Task<string> LogFallbackProcessingAsync(
            string query,
            FallbackResult fallbackResult,
            long processingTimeMs,
            string userId, 
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || !IsEventTypeEnabled(EventTypes.Fallback))
            {
                return string.Empty;
            }
            
            try
            {
                var telemetryEvent = new FallbackEvent
                {
                    EventType = "Fallback",
                    Query = SanitizeText(query),
                    OriginalIntent = fallbackResult.OriginalResult?.Intent ?? "unknown",
                    OriginalConfidence = fallbackResult.OriginalResult?.Confidence ?? 0,
                    FallbackLevel = fallbackResult.FallbackLevel.ToString(),
                    WasSuccessful = fallbackResult.IsSuccessful,
                    Reason = fallbackResult.FallbackReason,
                    FinalIntent = fallbackResult.FinalResult?.Intent ?? "unknown",
                    FinalConfidence = fallbackResult.FinalResult?.Confidence ?? 0,
                    RequiredUserInteraction = fallbackResult.RequiresUserInteraction,
                    ClarificationQuestionCount = fallbackResult.ClarificationQuestions.Count,
                    ProcessingTimeMs = processingTimeMs,
                    UserId = AnonymizeUserIdIfNeeded(userId),
                    ConversationId = conversationId,
                    AppVersion = _appVersion,
                    SourceComponent = "SmartInsight.AI.FallbackManager"
                };
                
                await _repository.StoreEventAsync(telemetryEvent, cancellationToken);
                
                // Log fallback data to improve the system
                _logger.LogInformation(
                    "Fallback processing: Level: {Level}, Success: {Success}, Interaction Required: {Interaction}, Original Intent: {OriginalIntent}, Final Intent: {FinalIntent}",
                    fallbackResult.FallbackLevel, fallbackResult.IsSuccessful, 
                    fallbackResult.RequiresUserInteraction, telemetryEvent.OriginalIntent, telemetryEvent.FinalIntent);
                
                return telemetryEvent.EventId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging fallback processing event");
                return string.Empty;
            }
        }
        
        /// <inheritdoc />
        public async Task<string> LogUserFeedbackAsync(
            string query,
            string detectedIntent,
            string expectedIntent,
            bool wasCorrect,
            int? rating,
            string comments,
            string relatedEventId,
            string userId,
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || !IsEventTypeEnabled(EventTypes.UserFeedback))
            {
                return string.Empty;
            }
            
            try
            {
                var telemetryEvent = new UserFeedbackEvent
                {
                    EventType = "UserFeedback",
                    Query = SanitizeText(query),
                    DetectedIntent = detectedIntent,
                    ExpectedIntent = expectedIntent,
                    WasCorrect = wasCorrect,
                    Rating = rating,
                    Comments = SanitizeText(comments),
                    RelatedEventId = relatedEventId,
                    UserId = AnonymizeUserIdIfNeeded(userId),
                    ConversationId = conversationId,
                    AppVersion = _appVersion,
                    SourceComponent = "SmartInsight.API"
                };
                
                await _repository.StoreEventAsync(telemetryEvent, cancellationToken);
                
                // Log incorrect classifications to improve the model
                if (!wasCorrect)
                {
                    _logger.LogWarning(
                        "User feedback indicates incorrect classification: Query: {Query}, Detected: {Detected}, Expected: {Expected}",
                        SanitizeText(query), detectedIntent, expectedIntent);
                }
                
                return telemetryEvent.EventId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user feedback event");
                return string.Empty;
            }
        }
        
        /// <inheritdoc />
        public async Task<string> LogPerformanceMetricAsync(
            string operation,
            long durationMs,
            bool succeeded,
            string errorMessage,
            int? inputTokens,
            int? outputTokens,
            decimal? cost,
            string userId,
            string conversationId,
            string sourceComponent,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || !IsEventTypeEnabled(EventTypes.PerformanceMetrics))
            {
                return string.Empty;
            }
            
            try
            {
                var telemetryEvent = new PerformanceMetricEvent
                {
                    EventType = "PerformanceMetric",
                    Operation = operation,
                    DurationMs = durationMs,
                    Succeeded = succeeded,
                    ErrorMessage = errorMessage,
                    InputTokens = _options.TrackTokensAndCosts ? inputTokens : null,
                    OutputTokens = _options.TrackTokensAndCosts ? outputTokens : null,
                    Cost = _options.TrackTokensAndCosts ? cost : null,
                    UserId = AnonymizeUserIdIfNeeded(userId),
                    ConversationId = conversationId,
                    AppVersion = _appVersion,
                    SourceComponent = sourceComponent
                };
                
                await _repository.StoreEventAsync(telemetryEvent, cancellationToken);
                
                // Log slow operations
                if (durationMs > 5000) // 5 seconds
                {
                    _logger.LogWarning(
                        "Slow operation detected: Operation: {Operation}, Duration: {Duration}ms, Component: {Component}",
                        operation, durationMs, sourceComponent);
                }
                
                // Log failures
                if (!succeeded)
                {
                    _logger.LogError(
                        "Operation failed: Operation: {Operation}, Error: {Error}, Component: {Component}",
                        operation, errorMessage, sourceComponent);
                }
                
                return telemetryEvent.EventId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging performance metric event");
                return string.Empty;
            }
        }
        
        /// <inheritdoc />
        public async Task<string> LogReasoningOperationAsync(
            string query,
            SmartInsight.Telemetry.Models.ReasoningResult reasoningResult,
            long processingTimeMs,
            string userId,
            string conversationId,
            string modelName,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || !IsEventTypeEnabled(EventTypes.Reasoning))
            {
                return string.Empty;
            }
            
            try
            {
                var telemetryEvent = new IntentDetectionEvent
                {
                    EventType = "Reasoning",
                    Query = SanitizeText(query),
                    Intent = reasoningResult.DetectedIntent?.Intent ?? "unknown",
                    Confidence = reasoningResult.DetectedIntent?.Confidence ?? 0,
                    UsedContext = true, // Reasoning always uses context
                    ProcessingTimeMs = processingTimeMs,
                    ModelName = modelName,
                    UserId = AnonymizeUserIdIfNeeded(userId),
                    ConversationId = conversationId,
                    AppVersion = _appVersion,
                    SourceComponent = "SmartInsight.AI.ReasoningEngine",
                    SelfVerified = true
                };
                
                // Add reasoning steps
                foreach (var step in reasoningResult.ReasoningSteps)
                {
                    telemetryEvent.ReasoningSteps.Add(new ReasoningStepInfo
                    {
                        StepNumber = step.StepNumber,
                        Description = SanitizeText(step.Description),
                        Outcome = SanitizeText(step.Outcome),
                        Confidence = step.Confidence
                    });
                }
                
                // Add entities from the result
                if (reasoningResult.ExtractedEntities != null)
                {
                    foreach (var entity in reasoningResult.ExtractedEntities)
                    {
                        telemetryEvent.ExtractedEntities.Add(new EntityInfo
                        {
                            Type = entity.Type,
                            Value = SanitizeText(entity.Value),
                            Confidence = entity.Confidence
                        });
                    }
                }
                
                await _repository.StoreEventAsync(telemetryEvent, cancellationToken);
                
                // Log failed reasoning
                if (!reasoningResult.IsSuccessful)
                {
                    _logger.LogWarning(
                        "Reasoning operation failed: Query: {Query}, Error: {Error}",
                        SanitizeText(query), reasoningResult.ErrorMessage);
                }
                
                return telemetryEvent.EventId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging reasoning operation event");
                return string.Empty;
            }
        }
        
        /// <inheritdoc />
        public async Task<object> GetAnonymizedMetricsAsync(
            DateTime startTime,
            DateTime endTime,
            string metricType,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // For intent detection metrics
                if (metricType == "intent_detection")
                {
                    var events = await _repository.GetEventsByTypeAsync("IntentDetection", startTime, endTime, limit, cancellationToken);
                    var intentEvents = events.OfType<IntentDetectionEvent>().ToList();
                    
                    // Calculate intent distribution
                    var intentDistribution = intentEvents
                        .GroupBy(e => e.Intent)
                        .Select(g => new { Intent = g.Key, Count = g.Count(), AverageConfidence = g.Average(e => e.Confidence) })
                        .OrderByDescending(g => g.Count)
                        .ToList();
                        
                    // Calculate confidence distribution
                    var confidenceBuckets = new Dictionary<string, int>
                    {
                        { "Very Low (0.0-0.3)", 0 },
                        { "Low (0.3-0.5)", 0 },
                        { "Medium (0.5-0.7)", 0 },
                        { "High (0.7-0.9)", 0 },
                        { "Very High (0.9-1.0)", 0 }
                    };
                    
                    foreach (var e in intentEvents)
                    {
                        if (e.Confidence < 0.3) confidenceBuckets["Very Low (0.0-0.3)"]++;
                        else if (e.Confidence < 0.5) confidenceBuckets["Low (0.3-0.5)"]++;
                        else if (e.Confidence < 0.7) confidenceBuckets["Medium (0.5-0.7)"]++;
                        else if (e.Confidence < 0.9) confidenceBuckets["High (0.7-0.9)"]++;
                        else confidenceBuckets["Very High (0.9-1.0)"]++;
                    }
                    
                    // Calculate context usage metrics
                    var contextUsage = new
                    {
                        WithContext = intentEvents.Count(e => e.UsedContext),
                        WithoutContext = intentEvents.Count(e => !e.UsedContext),
                        AverageConfidenceWithContext = intentEvents.Where(e => e.UsedContext).Average(e => e.Confidence),
                        AverageConfidenceWithoutContext = intentEvents.Where(e => !e.UsedContext).Average(e => e.Confidence)
                    };
                    
                    // Calculate fallback metrics
                    var fallbackCounts = intentEvents
                        .Where(e => e.Fallback != null && e.Fallback.WasNeeded)
                        .GroupBy(e => e.Fallback.Level)
                        .Select(g => new { Level = g.Key, Count = g.Count(), SuccessRate = g.Average(e => e.Fallback.WasSuccessful ? 1.0 : 0.0) })
                        .OrderByDescending(g => g.Count)
                        .ToList();
                        
                    return new 
                    {
                        TotalCount = intentEvents.Count,
                        AverageConfidence = intentEvents.Any() ? intentEvents.Average(e => e.Confidence) : 0,
                        AverageProcessingTime = intentEvents.Any() ? intentEvents.Average(e => e.ProcessingTimeMs) : 0,
                        IntentDistribution = intentDistribution,
                        ConfidenceDistribution = confidenceBuckets,
                        ContextUsage = contextUsage,
                        FallbackMetrics = fallbackCounts,
                        TimeRange = new { Start = startTime, End = endTime }
                    };
                }
                // For fallback metrics
                else if (metricType == "fallback")
                {
                    var events = await _repository.GetEventsByTypeAsync("Fallback", startTime, endTime, limit, cancellationToken);
                    var fallbackEvents = events.OfType<FallbackEvent>().ToList();
                    
                    // Calculate metrics by fallback level
                    var levelMetrics = fallbackEvents
                        .GroupBy(e => e.FallbackLevel)
                        .Select(g => new
                        {
                            Level = g.Key,
                            Count = g.Count(),
                            SuccessRate = g.Average(e => e.WasSuccessful ? 1.0 : 0.0),
                            RequiredInteractionRate = g.Average(e => e.RequiredUserInteraction ? 1.0 : 0.0),
                            AverageConfidenceImprovement = g.Average(e => e.FinalConfidence - e.OriginalConfidence)
                        })
                        .OrderByDescending(g => g.Count)
                        .ToList();
                        
                    return new
                    {
                        TotalCount = fallbackEvents.Count,
                        OverallSuccessRate = fallbackEvents.Any() ? fallbackEvents.Average(e => e.WasSuccessful ? 1.0 : 0.0) : 0,
                        AverageProcessingTime = fallbackEvents.Any() ? fallbackEvents.Average(e => e.ProcessingTimeMs) : 0,
                        InteractionRate = fallbackEvents.Any() ? fallbackEvents.Average(e => e.RequiredUserInteraction ? 1.0 : 0.0) : 0,
                        LevelMetrics = levelMetrics,
                        TimeRange = new { Start = startTime, End = endTime }
                    };
                }
                // For performance metrics
                else if (metricType == "performance")
                {
                    // Get aggregated metrics for common operations
                    var operations = new[] 
                    { 
                        "IntentDetection", 
                        "Reasoning", 
                        "FallbackProcessing", 
                        "VectorEmbedding", 
                        "LLMCompletion" 
                    };
                    
                    var metricsDict = new Dictionary<string, object>();
                    
                    foreach (var operation in operations)
                    {
                        var metrics = await _repository.GetAggregatedMetricsAsync(
                            operation, startTime, endTime, cancellationToken);
                            
                        if (metrics.Count > 0)
                        {
                            metricsDict[operation] = metrics;
                        }
                    }
                    
                    return new
                    {
                        Metrics = metricsDict,
                        TimeRange = new { Start = startTime, End = endTime }
                    };
                }
                
                return new { Error = "Unsupported metric type" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving anonymized metrics");
                return new { Error = ex.Message };
            }
        }
        
        #region Private Helper Methods
        
        private string AnonymizeUserIdIfNeeded(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return "anonymous";
            }
            
            if (_options.AnonymizeUserData)
            {
                // Use a hash of the user ID instead of the actual ID
                return Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.Create()
                        .ComputeHash(System.Text.Encoding.UTF8.GetBytes(userId)))
                        .Substring(0, 16); // Take the first 16 chars of the hash
            }
            
            return userId;
        }
        
        private string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            
            // Remove any sensitive information that might be in the text
            foreach (var field in SensitiveFields)
            {
                // Match patterns like "password=123456" or "password: 123456" or "password":"123456"
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    $@"({field})(=|:|\s*"":\s*""|\s*:\s*)([^\s""&]+|""[^""]*"")",
                    $"$1$2[REDACTED]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            return text;
        }
        
        private bool IsEventTypeEnabled(EventTypes eventType)
        {
            return (_options.EventTypesToLog & eventType) == eventType;
        }
        
        private string GetAppVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }
        
        #endregion
    }
} 