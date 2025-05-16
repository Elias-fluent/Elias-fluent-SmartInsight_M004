using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces
{
    /// <summary>
    /// Interface for managing versions of the knowledge graph
    /// </summary>
    public interface IKnowledgeGraphVersioningManager
    {
        /// <summary>
        /// Records a new version of a triple
        /// </summary>
        /// <param name="triple">The triple being versioned</param>
        /// <param name="changeType">Type of change made to the triple</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="userId">ID of the user making the change (if applicable)</param>
        /// <param name="comment">Optional comment about the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created version record</returns>
        Task<TripleVersion> RecordVersionAsync(
            Triple triple, 
            ChangeType changeType, 
            string tenantId, 
            string userId = null, 
            string comment = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets the version history for a specific triple
        /// </summary>
        /// <param name="tripleId">ID of the triple</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="maxVersions">Maximum number of versions to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of version records for the triple</returns>
        Task<List<TripleVersion>> GetVersionHistoryAsync(
            string tripleId, 
            string tenantId, 
            int maxVersions = 100, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets a specific version of a triple
        /// </summary>
        /// <param name="tripleId">ID of the triple</param>
        /// <param name="versionNumber">The version number to retrieve</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The requested version record</returns>
        Task<TripleVersion> GetVersionAsync(
            string tripleId, 
            int versionNumber, 
            string tenantId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Calculates the difference between two versions of a triple
        /// </summary>
        /// <param name="tripleId">ID of the triple</param>
        /// <param name="fromVersion">The earlier version number</param>
        /// <param name="toVersion">The later version number</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object describing the differences between versions</returns>
        Task<TripleDiff> GetVersionDiffAsync(
            string tripleId, 
            int fromVersion, 
            int toVersion,
            string tenantId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Performs a temporal query on the knowledge graph
        /// </summary>
        /// <param name="query">The temporal query parameters</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query results</returns>
        Task<TemporalQueryResult> QueryTemporalAsync(
            TemporalQuery query, 
            string tenantId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Creates a snapshot of the current state of the knowledge graph
        /// </summary>
        /// <param name="snapshotName">Name for the snapshot</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUris">Optional list of graph URIs to include in the snapshot (null for all)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CreateSnapshotAsync(
            string snapshotName, 
            string tenantId,
            IEnumerable<string> graphUris = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Restores the knowledge graph to a previously created snapshot
        /// </summary>
        /// <param name="snapshotName">Name of the snapshot to restore</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RestoreSnapshotAsync(
            string snapshotName, 
            string tenantId, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Restores a triple to a previous version
        /// </summary>
        /// <param name="tripleId">ID of the triple to restore</param>
        /// <param name="versionNumber">The version to restore to</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="userId">ID of the user performing the restoration</param>
        /// <param name="comment">Optional comment about the restoration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The restored triple</returns>
        Task<Triple> RestoreVersionAsync(
            string tripleId, 
            int versionNumber, 
            string tenantId, 
            string userId = null, 
            string comment = null, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Gets a list of available snapshots
        /// </summary>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available snapshots with metadata</returns>
        Task<Dictionary<string, Dictionary<string, object>>> GetAvailableSnapshotsAsync(
            string tenantId, 
            CancellationToken cancellationToken = default);
    }
} 