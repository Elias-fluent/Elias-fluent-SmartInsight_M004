using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartInsight.API.Extensions;
using SmartInsight.Core.DTOs;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.API.Controllers
{
    /// <summary>
    /// API endpoints for Knowledge Graph operations including versioning and temporal queries
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class KnowledgeGraphController : ApiControllerBase
    {
        private readonly ILogger<KnowledgeGraphController> _logger;
        private readonly ITripleStore _tripleStore;
        private readonly IKnowledgeGraphVersioningManager _versioningManager;
        
        /// <summary>
        /// Initializes a new instance of the KnowledgeGraphController
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="tripleStore">The triple store implementation</param>
        /// <param name="versioningManager">The knowledge graph versioning manager</param>
        public KnowledgeGraphController(
            ILogger<KnowledgeGraphController> logger,
            ITripleStore tripleStore,
            IKnowledgeGraphVersioningManager versioningManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tripleStore = tripleStore ?? throw new ArgumentNullException(nameof(tripleStore));
            _versioningManager = versioningManager ?? throw new ArgumentNullException(nameof(versioningManager));
        }
        
        /// <summary>
        /// Gets the version history for a triple
        /// </summary>
        /// <param name="tripleId">ID of the triple</param>
        /// <param name="maxVersions">Maximum number of versions to return (default: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of triple versions</returns>
        [HttpGet("versions/{tripleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<TripleVersion>>> GetVersionHistory(
            string tripleId,
            [FromQuery] int maxVersions = 100,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tripleId))
            {
                return BadRequest("Triple ID is required");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var versions = await _versioningManager.GetVersionHistoryAsync(
                    tripleId, tenantId, maxVersions, cancellationToken);
                    
                if (versions == null || !versions.Any())
                {
                    return NotFound($"No versions found for triple with ID {tripleId}");
                }
                
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version history for triple {TripleId}", tripleId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving version history");
            }
        }
        
        /// <summary>
        /// Gets a specific version of a triple
        /// </summary>
        /// <param name="tripleId">ID of the triple</param>
        /// <param name="versionNumber">Version number to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The specified version of the triple</returns>
        [HttpGet("versions/{tripleId}/{versionNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TripleVersion>> GetVersion(
            string tripleId,
            int versionNumber,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tripleId))
            {
                return BadRequest("Triple ID is required");
            }
            
            if (versionNumber <= 0)
            {
                return BadRequest("Version number must be greater than zero");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var version = await _versioningManager.GetVersionAsync(
                    tripleId, versionNumber, tenantId, cancellationToken);
                    
                if (version == null)
                {
                    return NotFound($"Version {versionNumber} not found for triple with ID {tripleId}");
                }
                
                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version {Version} for triple {TripleId}", versionNumber, tripleId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the version");
            }
        }
        
        /// <summary>
        /// Gets the differences between two versions of a triple
        /// </summary>
        /// <param name="tripleId">ID of the triple</param>
        /// <param name="fromVersion">The earlier version number</param>
        /// <param name="toVersion">The later version number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Differences between the versions</returns>
        [HttpGet("versions/{tripleId}/diff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TripleDiff>> GetVersionDiff(
            string tripleId,
            [FromQuery] int fromVersion,
            [FromQuery] int toVersion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tripleId))
            {
                return BadRequest("Triple ID is required");
            }
            
            if (fromVersion <= 0 || toVersion <= 0)
            {
                return BadRequest("Version numbers must be greater than zero");
            }
            
            if (fromVersion >= toVersion)
            {
                return BadRequest("From version must be less than to version");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var diff = await _versioningManager.GetVersionDiffAsync(
                    tripleId, fromVersion, toVersion, tenantId, cancellationToken);
                    
                if (diff == null)
                {
                    return NotFound($"Could not calculate diff between versions {fromVersion} and {toVersion} for triple with ID {tripleId}");
                }
                
                return Ok(diff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating diff between versions {FromVersion} and {ToVersion} for triple {TripleId}", 
                    fromVersion, toVersion, tripleId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while calculating the version diff");
            }
        }
        
        /// <summary>
        /// Performs a temporal query on the knowledge graph
        /// </summary>
        /// <param name="queryDto">The temporal query parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Results of the temporal query</returns>
        [HttpPost("temporal-query")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TemporalQueryResult>> QueryTemporal(
            [FromBody] TemporalQueryDto queryDto,
            CancellationToken cancellationToken = default)
        {
            if (queryDto == null)
            {
                return BadRequest("Query parameters are required");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var query = queryDto.ToModel();
                var results = await _versioningManager.QueryTemporalAsync(query, tenantId, cancellationToken);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing temporal query");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while executing the temporal query");
            }
        }
        
        /// <summary>
        /// Creates a snapshot of the current knowledge graph state
        /// </summary>
        /// <param name="snapshotName">Name for the snapshot</param>
        /// <param name="graphUris">Optional list of graph URIs to include (null for all)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success indicator</returns>
        [HttpPost("snapshots/{snapshotName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateSnapshot(
            string snapshotName,
            [FromBody] IEnumerable<string> graphUris = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(snapshotName))
            {
                return BadRequest("Snapshot name is required");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var result = await _versioningManager.CreateSnapshotAsync(
                    snapshotName, tenantId, graphUris, cancellationToken);
                    
                if (result)
                {
                    return Ok(new { Message = $"Snapshot '{snapshotName}' created successfully" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create snapshot");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snapshot '{SnapshotName}'", snapshotName);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the snapshot");
            }
        }
        
        /// <summary>
        /// Restores the knowledge graph from a snapshot
        /// </summary>
        /// <param name="snapshotName">Name of the snapshot to restore</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success indicator</returns>
        [HttpPost("snapshots/{snapshotName}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RestoreSnapshot(
            string snapshotName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(snapshotName))
            {
                return BadRequest("Snapshot name is required");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var result = await _versioningManager.RestoreSnapshotAsync(
                    snapshotName, tenantId, cancellationToken);
                    
                if (result)
                {
                    return Ok(new { Message = $"Snapshot '{snapshotName}' restored successfully" });
                }
                else
                {
                    return NotFound($"Snapshot '{snapshotName}' not found or could not be restored");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring snapshot '{SnapshotName}'", snapshotName);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while restoring the snapshot");
            }
        }
        
        /// <summary>
        /// Gets a list of available snapshots
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available snapshots with metadata</returns>
        [HttpGet("snapshots")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<Dictionary<string, Dictionary<string, object>>>> GetAvailableSnapshots(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var tenantId = GetTenantId();
                var snapshots = await _versioningManager.GetAvailableSnapshotsAsync(tenantId, cancellationToken);
                return Ok(snapshots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available snapshots");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving snapshots");
            }
        }
        
        /// <summary>
        /// Restores a triple to a previous version
        /// </summary>
        /// <param name="tripleId">ID of the triple to restore</param>
        /// <param name="versionNumber">Version to restore to</param>
        /// <param name="comment">Optional comment about the restoration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The restored triple</returns>
        [HttpPost("versions/{tripleId}/{versionNumber}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Triple>> RestoreVersion(
            string tripleId,
            int versionNumber,
            [FromQuery] string comment = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tripleId))
            {
                return BadRequest("Triple ID is required");
            }
            
            if (versionNumber <= 0)
            {
                return BadRequest("Version number must be greater than zero");
            }
            
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();
                
                var restoredTriple = await _versioningManager.RestoreVersionAsync(
                    tripleId, versionNumber, tenantId, userId, comment, cancellationToken);
                    
                if (restoredTriple == null)
                {
                    return NotFound($"Could not restore version {versionNumber} for triple with ID {tripleId}");
                }
                
                return Ok(restoredTriple);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring version {Version} for triple {TripleId}", versionNumber, tripleId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while restoring the version");
            }
        }
        
        /// <summary>
        /// Gets the tenant ID from the current user context
        /// </summary>
        private string GetTenantId()
        {
            // In a real implementation, this would extract the tenant ID from claims or headers
            // For now, we'll use a default tenant ID for simplicity
            return "default-tenant";
        }
        
        /// <summary>
        /// Gets the user ID from the current user context
        /// </summary>
        private string GetUserId()
        {
            // In a real implementation, this would extract the user ID from claims
            // For now, we'll use a default user ID for simplicity
            return "system-user";
        }
    }
} 