using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for SQL log retention and cleanup services
    /// </summary>
    public interface ISqlLogRetentionService
    {
        /// <summary>
        /// Cleans up old SQL logs based on retention policy
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the cleanup operation</returns>
        Task<LogCleanupResult> CleanupLogsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Cleans up old SQL logs for a specific tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the cleanup operation</returns>
        Task<LogCleanupResult> CleanupLogsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the total number of log entries
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The count of log entries</returns>
        Task<long> GetLogCountAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the count of log entries by type
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of log types and counts</returns>
        Task<Dictionary<LogType, long>> GetLogCountByTypeAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the current retention settings
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The retention settings</returns>
        Task<SqlLogRetentionOptions> GetRetentionSettingsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates the retention settings
        /// </summary>
        /// <param name="settings">The new retention settings</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completion task</returns>
        Task UpdateRetentionSettingsAsync(SqlLogRetentionOptions settings, CancellationToken cancellationToken = default);
    }
} 