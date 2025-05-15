using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Telemetry.Models;

namespace SmartInsight.Telemetry.Repositories
{
    /// <summary>
    /// Interface for telemetry data repositories
    /// </summary>
    public interface ITelemetryRepository
    {
        /// <summary>
        /// Stores a telemetry event
        /// </summary>
        /// <param name="telemetryEvent">The event to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success indicator</returns>
        Task<bool> StoreEventAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stores multiple telemetry events
        /// </summary>
        /// <param name="telemetryEvents">The events to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success indicator</returns>
        Task<bool> StoreBatchAsync(IEnumerable<TelemetryEvent> telemetryEvents, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets events by event type
        /// </summary>
        /// <param name="eventType">The type of event to retrieve</param>
        /// <param name="startTime">Start time filter</param>
        /// <param name="endTime">End time filter</param>
        /// <param name="limit">Maximum number of events to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching telemetry events</returns>
        Task<IEnumerable<TelemetryEvent>> GetEventsByTypeAsync(
            string eventType, 
            DateTime startTime, 
            DateTime endTime, 
            int limit = 100, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets events by user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="startTime">Start time filter</param>
        /// <param name="endTime">End time filter</param>
        /// <param name="limit">Maximum number of events to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching telemetry events</returns>
        Task<IEnumerable<TelemetryEvent>> GetEventsByUserAsync(
            string userId, 
            DateTime startTime, 
            DateTime endTime, 
            int limit = 100,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets events by conversation ID
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="limit">Maximum number of events to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching telemetry events</returns>
        Task<IEnumerable<TelemetryEvent>> GetEventsByConversationAsync(
            string conversationId, 
            int limit = 100, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets intent detection events with low confidence
        /// </summary>
        /// <param name="thresholdConfidence">Confidence threshold</param>
        /// <param name="startTime">Start time filter</param>
        /// <param name="endTime">End time filter</param>
        /// <param name="limit">Maximum number of events to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching events</returns>
        Task<IEnumerable<IntentDetectionEvent>> GetLowConfidenceEventsAsync(
            double thresholdConfidence,
            DateTime startTime,
            DateTime endTime,
            int limit = 100,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets fallback events that required user interaction
        /// </summary>
        /// <param name="startTime">Start time filter</param>
        /// <param name="endTime">End time filter</param>
        /// <param name="limit">Maximum number of events to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching events</returns>
        Task<IEnumerable<FallbackEvent>> GetUserInteractionFallbacksAsync(
            DateTime startTime,
            DateTime endTime,
            int limit = 100,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets performance metrics aggregated by operation
        /// </summary>
        /// <param name="operation">The operation name</param>
        /// <param name="startTime">Start time filter</param>
        /// <param name="endTime">End time filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Aggregated metrics</returns>
        Task<IDictionary<string, object>> GetAggregatedMetricsAsync(
            string operation,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Purges events older than a specified date
        /// </summary>
        /// <param name="cutoffDate">Cut-off date for purging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of events purged</returns>
        Task<int> PurgeEventsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Exports events to a file
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="startTime">Start time filter</param>
        /// <param name="endTime">End time filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of events exported</returns>
        Task<int> ExportEventsAsync(
            string filePath,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);
    }
} 