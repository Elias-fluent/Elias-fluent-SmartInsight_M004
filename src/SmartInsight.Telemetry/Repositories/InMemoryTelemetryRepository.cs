using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Telemetry.Models;
using SmartInsight.Telemetry.Options;

namespace SmartInsight.Telemetry.Repositories
{
    /// <summary>
    /// In-memory implementation of the telemetry repository for development and testing
    /// </summary>
    public class InMemoryTelemetryRepository : ITelemetryRepository
    {
        private ConcurrentBag<TelemetryEvent> _events = new ConcurrentBag<TelemetryEvent>();
        private readonly ILogger<InMemoryTelemetryRepository> _logger;
        private readonly TelemetryOptions _options;
        private readonly Random _random = new Random();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryTelemetryRepository"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="options">Telemetry options</param>
        public InMemoryTelemetryRepository(
            ILogger<InMemoryTelemetryRepository> logger,
            IOptions<TelemetryOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new TelemetryOptions();
        }
        
        /// <inheritdoc />
        public Task<bool> StoreEventAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
        {
            // Apply sampling if configured
            if (_options.SamplingRate < 1.0 && _random.NextDouble() > _options.SamplingRate)
            {
                return Task.FromResult(true); // Skip this event due to sampling
            }
            
            if (telemetryEvent == null)
            {
                _logger.LogWarning("Attempted to store null telemetry event");
                return Task.FromResult(false);
            }
            
            try
            {
                // Add the event to the in-memory collection
                _events.Add(telemetryEvent);
                
                // Optionally write to local storage if enabled
                if (_options.UseLocalStorage)
                {
                    Task.Run(async () => await WriteToLocalStorageAsync(telemetryEvent));
                }
                
                // Enforce max events limit if necessary
                EnforceMaxEventsLimit();
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing telemetry event");
                return Task.FromResult(false);
            }
        }
        
        /// <inheritdoc />
        public Task<bool> StoreBatchAsync(IEnumerable<TelemetryEvent> telemetryEvents, CancellationToken cancellationToken = default)
        {
            if (telemetryEvents == null)
            {
                _logger.LogWarning("Attempted to store null batch of telemetry events");
                return Task.FromResult(false);
            }
            
            try
            {
                int count = 0;
                foreach (var telemetryEvent in telemetryEvents)
                {
                    // Apply sampling if configured
                    if (_options.SamplingRate < 1.0 && _random.NextDouble() > _options.SamplingRate)
                    {
                        continue; // Skip this event due to sampling
                    }
                    
                    _events.Add(telemetryEvent);
                    count++;
                    
                    // Optionally write to local storage
                    if (_options.UseLocalStorage)
                    {
                        Task.Run(async () => await WriteToLocalStorageAsync(telemetryEvent));
                    }
                }
                
                _logger.LogDebug("Stored {Count} telemetry events in batch", count);
                
                // Enforce max events limit if necessary
                EnforceMaxEventsLimit();
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing batch of telemetry events");
                return Task.FromResult(false);
            }
        }
        
        /// <inheritdoc />
        public Task<IEnumerable<TelemetryEvent>> GetEventsByTypeAsync(
            string eventType, 
            DateTime startTime, 
            DateTime endTime, 
            int limit = 100, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var events = _events
                    .Where(e => e.EventType == eventType &&
                                e.Timestamp >= startTime &&
                                e.Timestamp <= endTime)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToList();
                
                _logger.LogDebug("Retrieved {Count} events of type {EventType}", events.Count, eventType);
                
                return Task.FromResult(events.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events by type {EventType}", eventType);
                return Task.FromResult(Enumerable.Empty<TelemetryEvent>());
            }
        }
        
        /// <inheritdoc />
        public Task<IEnumerable<TelemetryEvent>> GetEventsByUserAsync(
            string userId, 
            DateTime startTime, 
            DateTime endTime, 
            int limit = 100, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var events = _events
                    .Where(e => e.UserId == userId &&
                                e.Timestamp >= startTime &&
                                e.Timestamp <= endTime)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToList();
                
                _logger.LogDebug("Retrieved {Count} events for user {UserId}", events.Count, userId);
                
                return Task.FromResult(events.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for user {UserId}", userId);
                return Task.FromResult(Enumerable.Empty<TelemetryEvent>());
            }
        }
        
        /// <inheritdoc />
        public Task<IEnumerable<TelemetryEvent>> GetEventsByConversationAsync(
            string conversationId, 
            int limit = 100, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var events = _events
                    .Where(e => e.ConversationId == conversationId)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToList();
                
                _logger.LogDebug("Retrieved {Count} events for conversation {ConversationId}", events.Count, conversationId);
                
                return Task.FromResult(events.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for conversation {ConversationId}", conversationId);
                return Task.FromResult(Enumerable.Empty<TelemetryEvent>());
            }
        }
        
        /// <inheritdoc />
        public Task<IEnumerable<IntentDetectionEvent>> GetLowConfidenceEventsAsync(
            double thresholdConfidence, 
            DateTime startTime, 
            DateTime endTime, 
            int limit = 100, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var events = _events
                    .OfType<IntentDetectionEvent>()
                    .Where(e => e.Confidence < thresholdConfidence &&
                                e.Timestamp >= startTime &&
                                e.Timestamp <= endTime)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToList();
                
                _logger.LogDebug("Retrieved {Count} low confidence events below threshold {Threshold}", 
                    events.Count, thresholdConfidence);
                
                return Task.FromResult(events.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low confidence events");
                return Task.FromResult(Enumerable.Empty<IntentDetectionEvent>());
            }
        }
        
        /// <inheritdoc />
        public Task<IEnumerable<FallbackEvent>> GetUserInteractionFallbacksAsync(
            DateTime startTime, 
            DateTime endTime, 
            int limit = 100, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var events = _events
                    .OfType<FallbackEvent>()
                    .Where(e => e.RequiredUserInteraction &&
                                e.Timestamp >= startTime &&
                                e.Timestamp <= endTime)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .ToList();
                
                _logger.LogDebug("Retrieved {Count} fallback events requiring user interaction", events.Count);
                
                return Task.FromResult(events.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fallback events requiring user interaction");
                return Task.FromResult(Enumerable.Empty<FallbackEvent>());
            }
        }
        
        /// <inheritdoc />
        public Task<IDictionary<string, object>> GetAggregatedMetricsAsync(
            string operation,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var metrics = new Dictionary<string, object>();
                
                // Filter performance metrics for the specified operation
                var performanceEvents = _events
                    .OfType<PerformanceMetricEvent>()
                    .Where(e => e.Operation == operation &&
                                e.Timestamp >= startTime &&
                                e.Timestamp <= endTime)
                    .ToList();
                
                if (performanceEvents.Any())
                {
                    // Calculate basic statistics
                    metrics["Count"] = performanceEvents.Count;
                    metrics["AverageDuration"] = performanceEvents.Average(e => e.DurationMs);
                    metrics["MinDuration"] = performanceEvents.Min(e => e.DurationMs);
                    metrics["MaxDuration"] = performanceEvents.Max(e => e.DurationMs);
                    metrics["SuccessRate"] = (double)performanceEvents.Count(e => e.Succeeded) / performanceEvents.Count;
                    
                    // Calculate token usage if available
                    var eventsWithTokens = performanceEvents.Where(e => e.InputTokens.HasValue && e.OutputTokens.HasValue);
                    if (eventsWithTokens.Any())
                    {
                        metrics["TotalInputTokens"] = eventsWithTokens.Sum(e => e.InputTokens.Value);
                        metrics["TotalOutputTokens"] = eventsWithTokens.Sum(e => e.OutputTokens.Value);
                        metrics["AverageInputTokens"] = eventsWithTokens.Average(e => e.InputTokens.Value);
                        metrics["AverageOutputTokens"] = eventsWithTokens.Average(e => e.OutputTokens.Value);
                    }
                    
                    // Calculate costs if available
                    var eventsWithCost = performanceEvents.Where(e => e.Cost.HasValue);
                    if (eventsWithCost.Any())
                    {
                        metrics["TotalCost"] = eventsWithCost.Sum(e => e.Cost.Value);
                        metrics["AverageCost"] = eventsWithCost.Average(e => e.Cost.Value);
                    }
                    
                    // Add error distribution if any failures
                    var failedEvents = performanceEvents.Where(e => !e.Succeeded);
                    if (failedEvents.Any())
                    {
                        var errorGroups = failedEvents
                            .GroupBy(e => e.ErrorMessage)
                            .Select(g => new { ErrorMessage = g.Key, Count = g.Count() })
                            .OrderByDescending(g => g.Count)
                            .Take(5)
                            .ToList();
                        
                        var topErrors = new Dictionary<string, int>();
                        foreach (var error in errorGroups)
                        {
                            topErrors[error.ErrorMessage ?? "Unknown error"] = error.Count;
                        }
                        
                        metrics["TopErrors"] = topErrors;
                    }
                }
                
                _logger.LogDebug("Generated aggregated metrics for operation {Operation}", operation);
                
                return Task.FromResult((IDictionary<string, object>)metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating aggregated metrics for operation {Operation}", operation);
                return Task.FromResult((IDictionary<string, object>)new Dictionary<string, object>());
            }
        }
        
        /// <inheritdoc />
        public Task<int> PurgeEventsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create a new collection with only events after the cutoff date
                var currentEvents = _events.ToList();
                var eventsToKeep = currentEvents.Where(e => e.Timestamp > cutoffDate).ToList();
                
                int purgeCount = currentEvents.Count - eventsToKeep.Count;
                
                if (purgeCount > 0)
                {
                    // Replace the events collection with the filtered list
                    _events = new ConcurrentBag<TelemetryEvent>(eventsToKeep);
                    
                    _logger.LogInformation("Purged {Count} events older than {CutoffDate}", purgeCount, cutoffDate);
                }
                
                return Task.FromResult(purgeCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purging events older than {CutoffDate}", cutoffDate);
                return Task.FromResult(0);
            }
        }
        
        /// <inheritdoc />
        public async Task<int> ExportEventsAsync(
            string filePath, 
            DateTime startTime, 
            DateTime endTime, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var eventsToExport = _events
                    .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                    .OrderBy(e => e.Timestamp)
                    .ToList();
                
                if (!eventsToExport.Any())
                {
                    _logger.LogInformation("No events found to export for the specified time period");
                    return 0;
                }
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Serialize and write events to file
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(fs, eventsToExport, options, cancellationToken);
                }
                
                _logger.LogInformation("Exported {Count} events to {FilePath}", eventsToExport.Count, filePath);
                
                return eventsToExport.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting events to {FilePath}", filePath);
                return 0;
            }
        }
        
        #region Private Helper Methods
        
        private void EnforceMaxEventsLimit()
        {
            if (_options.MaxEventsInMemory > 0 && _events.Count > _options.MaxEventsInMemory)
            {
                var currentEvents = _events.ToList();
                if (currentEvents.Count > _options.MaxEventsInMemory)
                {
                    var eventsToKeep = currentEvents
                        .OrderByDescending(e => e.Timestamp)
                        .Take(_options.MaxEventsInMemory)
                        .ToList();
                        
                    // Replace the events collection with the trimmed list
                    _events = new ConcurrentBag<TelemetryEvent>(eventsToKeep);
                    
                    _logger.LogInformation("Trimmed in-memory events from {OriginalCount} to {MaxEvents}", 
                        currentEvents.Count, _options.MaxEventsInMemory);
                }
            }
        }
        
        private async Task WriteToLocalStorageAsync(TelemetryEvent telemetryEvent)
        {
            try
            {
                // Create the directory if it doesn't exist
                var directoryPath = _options.LocalStoragePath;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                
                // Create a file for each day
                var date = telemetryEvent.Timestamp.ToString("yyyy-MM-dd");
                var filePath = Path.Combine(directoryPath, $"telemetry-{date}.jsonl");
                
                // Serialize the event to JSON
                var json = JsonSerializer.Serialize(telemetryEvent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                // Append to the file
                using (var writer = new StreamWriter(filePath, true))
                {
                    await writer.WriteLineAsync(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing telemetry event to local storage");
            }
        }
        
        #endregion
    }
} 