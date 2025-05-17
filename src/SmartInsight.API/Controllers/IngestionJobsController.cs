using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.DTOs;
using SmartInsight.Core.Entities;
using SmartInsight.Knowledge.Connectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.API.Controllers
{
    /// <summary>
    /// API endpoints for managing and monitoring ingestion jobs
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class IngestionJobsController : ApiControllerBase
    {
        private readonly ILogger<IngestionJobsController> _logger;
        private readonly IDataSourceService _dataSourceService;
        private readonly IIngestionJobService _ingestionJobService;

        /// <summary>
        /// Initializes a new instance of the IngestionJobsController
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="dataSourceService">The data source service</param>
        /// <param name="ingestionJobService">The ingestion job service</param>
        public IngestionJobsController(
            ILogger<IngestionJobsController> logger,
            IDataSourceService dataSourceService,
            IIngestionJobService ingestionJobService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            _ingestionJobService = ingestionJobService ?? throw new ArgumentNullException(nameof(ingestionJobService));
        }

        /// <summary>
        /// Gets all ingestion jobs for the current tenant
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of ingestion jobs</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<IngestionJobDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetIngestionJobs(CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue && !CanAccessAllTenants())
            {
                _logger.LogWarning("User {UserId} attempted to access ingestion jobs without a valid tenant ID", GetCurrentUserId());
                return Forbid();
            }

            var jobs = await _ingestionJobService.GetIngestionJobsAsync(tenantId, CanAccessAllTenants(), cancellationToken);
            return Ok(jobs);
        }

        /// <summary>
        /// Gets a specific ingestion job
        /// </summary>
        /// <param name="id">Ingestion job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ingestion job details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IngestionJobDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetIngestionJob(Guid id, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var job = await _ingestionJobService.GetIngestionJobAsync(id, cancellationToken);

            if (job == null)
            {
                return NotFound(new { error = "Ingestion job not found" });
            }

            // Verify data source access permission
            var dataSource = await _dataSourceService.GetDataSourceAsync(job.DataSourceId, cancellationToken);
            if (dataSource == null)
            {
                return NotFound(new { error = "Related data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to access ingestion job from another tenant", GetCurrentUserId());
                return Forbid();
            }

            return Ok(job);
        }

        /// <summary>
        /// Gets ingestion jobs for a specific data source
        /// </summary>
        /// <param name="dataSourceId">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of ingestion jobs for the data source</returns>
        [HttpGet("dataSource/{dataSourceId}")]
        [ProducesResponseType(typeof(List<IngestionJobDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetIngestionJobsByDataSource(Guid dataSourceId, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            
            // Verify data source access permission
            var dataSource = await _dataSourceService.GetDataSourceAsync(dataSourceId, cancellationToken);
            if (dataSource == null)
            {
                return NotFound(new { error = "Data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to access ingestion jobs from another tenant", GetCurrentUserId());
                return Forbid();
            }

            var jobs = await _ingestionJobService.GetIngestionJobsByDataSourceAsync(dataSourceId, cancellationToken);
            return Ok(jobs);
        }

        /// <summary>
        /// Starts a new ingestion job for a data source
        /// </summary>
        /// <param name="dataSourceId">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created ingestion job</returns>
        [HttpPost("dataSource/{dataSourceId}/start")]
        [ProducesResponseType(typeof(IngestionJobDto), 202)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> StartIngestionJob(Guid dataSourceId, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            
            // Verify data source access permission
            var dataSource = await _dataSourceService.GetDataSourceAsync(dataSourceId, cancellationToken);
            if (dataSource == null)
            {
                return NotFound(new { error = "Data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to start ingestion job for a data source from another tenant", GetCurrentUserId());
                return Forbid();
            }

            try
            {
                var job = await _ingestionJobService.StartIngestionJobAsync(dataSourceId, cancellationToken);
                return Accepted(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ingestion job for data source {DataSourceId}", dataSourceId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Cancels an in-progress ingestion job
        /// </summary>
        /// <param name="id">Ingestion job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content if successful</returns>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelIngestionJob(Guid id, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var job = await _ingestionJobService.GetIngestionJobAsync(id, cancellationToken);

            if (job == null)
            {
                return NotFound(new { error = "Ingestion job not found" });
            }

            // Verify data source access permission
            var dataSource = await _dataSourceService.GetDataSourceAsync(job.DataSourceId, cancellationToken);
            if (dataSource == null)
            {
                return NotFound(new { error = "Related data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to cancel ingestion job from another tenant", GetCurrentUserId());
                return Forbid();
            }

            try
            {
                await _ingestionJobService.CancelIngestionJobAsync(id, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling ingestion job {JobId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the log entries for an ingestion job
        /// </summary>
        /// <param name="id">Ingestion job ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of log entries</returns>
        [HttpGet("{id}/logs")]
        [ProducesResponseType(typeof(List<IngestionLogEntryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetIngestionJobLogs(Guid id, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var job = await _ingestionJobService.GetIngestionJobAsync(id, cancellationToken);

            if (job == null)
            {
                return NotFound(new { error = "Ingestion job not found" });
            }

            // Verify data source access permission
            var dataSource = await _dataSourceService.GetDataSourceAsync(job.DataSourceId, cancellationToken);
            if (dataSource == null)
            {
                return NotFound(new { error = "Related data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to access logs for ingestion job from another tenant", GetCurrentUserId());
                return Forbid();
            }

            var logs = await _ingestionJobService.GetIngestionJobLogsAsync(id, cancellationToken);
            return Ok(logs);
        }
    }
} 