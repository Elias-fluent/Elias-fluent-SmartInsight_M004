using SmartInsight.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Knowledge.Connectors.Interfaces
{
    /// <summary>
    /// Service for managing data sources
    /// </summary>
    public interface IDataSourceService
    {
        /// <summary>
        /// Gets all data sources for the given tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="canAccessAllTenants">Whether the caller can access all tenants</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of data sources</returns>
        Task<List<DataSourceDto>> GetDataSourcesAsync(Guid? tenantId, bool canAccessAllTenants, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific data source by ID
        /// </summary>
        /// <param name="id">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Data source if found, null otherwise</returns>
        Task<DataSourceDto?> GetDataSourceAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new data source
        /// </summary>
        /// <param name="model">Data source creation model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created data source</returns>
        Task<DataSourceDto> CreateDataSourceAsync(CreateDataSourceDto model, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing data source
        /// </summary>
        /// <param name="id">Data source ID</param>
        /// <param name="model">Data source update model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateDataSourceAsync(Guid id, UpdateDataSourceDto model, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a data source
        /// </summary>
        /// <param name="id">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteDataSourceAsync(Guid id, CancellationToken cancellationToken = default);
    }
} 