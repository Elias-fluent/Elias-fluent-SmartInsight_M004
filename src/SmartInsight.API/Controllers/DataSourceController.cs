using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.DTOs;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Enums;
using SmartInsight.Knowledge.Connectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.API.Controllers
{
    /// <summary>
    /// API endpoints for data source management
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin")]
    public class DataSourceController : ApiControllerBase
    {
        private readonly ILogger<DataSourceController> _logger;
        private readonly IDataSourceService _dataSourceService;
        private readonly IConnectionTester _connectionTester;

        /// <summary>
        /// Initializes a new instance of the DataSourceController
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="dataSourceService">The data source service</param>
        /// <param name="connectionTester">The connection tester service</param>
        public DataSourceController(
            ILogger<DataSourceController> logger,
            IDataSourceService dataSourceService,
            IConnectionTester connectionTester)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            _connectionTester = connectionTester ?? throw new ArgumentNullException(nameof(connectionTester));
        }

        /// <summary>
        /// Gets all data sources for the current tenant
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of data sources</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<DataSourceDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetDataSources(CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue && !CanAccessAllTenants())
            {
                _logger.LogWarning("User {UserId} attempted to access data sources without a valid tenant ID", GetCurrentUserId());
                return Forbid();
            }

            var dataSources = await _dataSourceService.GetDataSourcesAsync(tenantId, CanAccessAllTenants(), cancellationToken);
            return Ok(dataSources);
        }

        /// <summary>
        /// Gets a specific data source
        /// </summary>
        /// <param name="id">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Data source details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DataSourceDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDataSource(Guid id, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var dataSource = await _dataSourceService.GetDataSourceAsync(id, cancellationToken);

            if (dataSource == null)
            {
                return NotFound(new { error = "Data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to access data source from another tenant", GetCurrentUserId());
                return Forbid();
            }

            return Ok(dataSource);
        }

        /// <summary>
        /// Creates a new data source
        /// </summary>
        /// <param name="model">Data source creation model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created data source</returns>
        [HttpPost]
        [ProducesResponseType(typeof(DataSourceDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CreateDataSource([FromBody] CreateDataSourceDto model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            // Set the tenant ID from the current user's context
            var createModel = new CreateDataSourceDto
            {
                Name = model.Name,
                Description = model.Description,
                SourceType = model.SourceType,
                TenantId = tenantId.ToString(),
                ConnectionParameters = model.ConnectionParameters,
                RefreshSchedule = model.RefreshSchedule
            };

            try
            {
                var dataSource = await _dataSourceService.CreateDataSourceAsync(createModel, cancellationToken);
                return CreatedAtAction(nameof(GetDataSource), new { id = dataSource.Id }, dataSource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data source");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing data source
        /// </summary>
        /// <param name="id">Data source ID</param>
        /// <param name="model">Updated data source details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content if successful</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateDataSource(Guid id, [FromBody] UpdateDataSourceDto model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenantId = GetCurrentTenantId();
            var dataSource = await _dataSourceService.GetDataSourceAsync(id, cancellationToken);

            if (dataSource == null)
            {
                return NotFound(new { error = "Data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to update data source from another tenant", GetCurrentUserId());
                return Forbid();
            }

            try
            {
                await _dataSourceService.UpdateDataSourceAsync(id, model, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data source {DataSourceId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a data source
        /// </summary>
        /// <param name="id">Data source ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteDataSource(Guid id, CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            var dataSource = await _dataSourceService.GetDataSourceAsync(id, cancellationToken);

            if (dataSource == null)
            {
                return NotFound(new { error = "Data source not found" });
            }

            // Validate tenant access
            if (!CanAccessAllTenants() && dataSource.TenantId != tenantId.ToString())
            {
                _logger.LogWarning("User {UserId} attempted to delete data source from another tenant", GetCurrentUserId());
                return Forbid();
            }

            try
            {
                await _dataSourceService.DeleteDataSourceAsync(id, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting data source {DataSourceId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tests a connection to a data source
        /// </summary>
        /// <param name="model">Connection details to test</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connection test result</returns>
        [HttpPost("test-connection")]
        [ProducesResponseType(typeof(ConnectionTestResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionDto model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _connectionTester.TestConnectionAsync(model.SourceType, model.ConnectionParameters, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection for data source of type {SourceType}", model.SourceType);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the available data source types
        /// </summary>
        /// <returns>List of data source types</returns>
        [HttpGet("types")]
        [ProducesResponseType(typeof(Dictionary<string, string>), 200)]
        [ProducesResponseType(401)]
        public IActionResult GetDataSourceTypes()
        {
            var types = new Dictionary<string, string>();
            foreach (var type in Enum.GetValues<DataSourceType>())
            {
                types.Add(((int)type).ToString(), type.ToString());
            }
            return Ok(types);
        }
    }
} 