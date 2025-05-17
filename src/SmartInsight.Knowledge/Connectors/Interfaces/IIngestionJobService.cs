using SmartInsight.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Knowledge.Connectors.Interfaces
{
    /// <summary>
    /// Service for managing data ingestion jobs
    /// </summary>
    public interface IIngestionJobService
    {
        /// <summary>
        /// Gets all ingestion jobs for the given tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="canAccessAllTenants">Whether the caller can access all tenants</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of ingestion jobs</returns>
        Task<List<IngestionJobDto>> GetIngestionJobsAsync(Guid? tenantId, bool canAccessAllTenants, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific ingestion job by ID
        /// </summary>
        /// <param name="id">Ingestion job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ingestion job if found, null otherwise</returns>
        Task<IngestionJobDto?> GetIngestionJobAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all ingestion jobs for a specific data source
        /// </summary>
        /// <param name="dataSourceId">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of ingestion jobs</returns>
        Task<List<IngestionJobDto>> GetIngestionJobsByDataSourceAsync(Guid dataSourceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a new ingestion job for a data source
        /// </summary>
        /// <param name="dataSourceId">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created ingestion job</returns>
        Task<IngestionJobDto> StartIngestionJobAsync(Guid dataSourceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an in-progress ingestion job
        /// </summary>
        /// <param name="id">Ingestion job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CancelIngestionJobAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the log entries for an ingestion job
        /// </summary>
        /// <param name="id">Ingestion job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of log entries</returns>
        Task<List<IngestionLogEntryDto>> GetIngestionJobLogsAsync(Guid id, CancellationToken cancellationToken = default);
    }
} 