using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance
{
    /// <summary>
    /// Implementation of the provenance tracker for the knowledge graph
    /// </summary>
    public class ProvenanceTracker : IProvenanceTracker, IDisposable
    {
        private readonly ILogger<ProvenanceTracker> _logger;
        private readonly ProvenanceTrackerOptions _options;
        private readonly ProvenanceStore _store;
        
        /// <summary>
        /// Initializes a new instance of the ProvenanceTracker class
        /// </summary>
        public ProvenanceTracker(
            ILogger<ProvenanceTracker> logger,
            IOptions<ProvenanceTrackerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            // Create the provenance store
            _store = new ProvenanceStore(
                new Logger<ProvenanceStore>(new LoggerFactory()),
                options);
                
            _logger.LogInformation("Initialized provenance tracker");
        }
        
        /// <summary>
        /// Records provenance metadata for a knowledge graph element
        /// </summary>
        public async Task<bool> RecordProvenanceAsync(
            ProvenanceMetadata metadata,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            
            if (!_options.EnableAutoTracking)
            {
                _logger.LogDebug("Automatic provenance tracking is disabled");
                return false;
            }
                
            try
            {
                // Validate confidence score
                if (metadata.ConfidenceScore < _options.DefaultConfidenceThreshold)
                {
                    _logger.LogDebug(
                        "Skipping provenance tracking for element with low confidence score: {ConfidenceScore} < {Threshold}",
                        metadata.ConfidenceScore,
                        _options.DefaultConfidenceThreshold);
                        
                    return false;
                }
                
                // Verify dependencies if tracking is enabled
                if (_options.TrackDependencies && metadata.Dependencies != null)
                {
                    foreach (var dependency in metadata.Dependencies)
                    {
                        if (string.IsNullOrEmpty(dependency.DependencyId) || 
                            string.IsNullOrEmpty(dependency.DependencyType))
                        {
                            _logger.LogWarning(
                                "Invalid dependency reference in provenance metadata for element {ElementType}:{ElementId}",
                                metadata.ElementType,
                                metadata.ElementId);
                                
                            // Skip invalid dependencies
                            continue;
                        }
                    }
                }
                else if (!_options.TrackDependencies)
                {
                    // Clear dependencies if not tracking
                    metadata.Dependencies.Clear();
                }
                
                // Store provenance metadata
                return await _store.StoreProvenanceAsync(metadata, tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recording provenance for element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    metadata.ElementType,
                    metadata.ElementId,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Records provenance metadata for a triple
        /// </summary>
        public async Task<ProvenanceMetadata> RecordTripleProvenanceAsync(
            Triple triple,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (triple == null)
                throw new ArgumentNullException(nameof(triple));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            if (!_options.EnableAutoTracking)
            {
                _logger.LogDebug("Automatic provenance tracking is disabled");
                return null;
            }
                
            try
            {
                // Convert triple to provenance metadata
                var metadata = triple.ToProvenanceMetadata();
                
                // Store provenance metadata
                var success = await RecordProvenanceAsync(metadata, tenantId, cancellationToken);
                
                return success ? metadata : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recording provenance for triple {TripleId} in tenant {TenantId}: {ErrorMessage}",
                    triple.Id,
                    tenantId,
                    ex.Message);
                    
                return null;
            }
        }
        
        /// <summary>
        /// Records provenance metadata for an entity
        /// </summary>
        public async Task<ProvenanceMetadata> RecordEntityProvenanceAsync(
            Entity entity,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            if (!_options.EnableAutoTracking)
            {
                _logger.LogDebug("Automatic provenance tracking is disabled");
                return null;
            }
                
            try
            {
                // Convert entity to provenance metadata
                var metadata = entity.ToProvenanceMetadata();
                
                // Store provenance metadata
                var success = await RecordProvenanceAsync(metadata, tenantId, cancellationToken);
                
                return success ? metadata : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recording provenance for entity {EntityId} in tenant {TenantId}: {ErrorMessage}",
                    entity.Id,
                    tenantId,
                    ex.Message);
                    
                return null;
            }
        }
        
        /// <summary>
        /// Records provenance metadata for a relation
        /// </summary>
        public async Task<ProvenanceMetadata> RecordRelationProvenanceAsync(
            Relation relation,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            if (!_options.EnableAutoTracking)
            {
                _logger.LogDebug("Automatic provenance tracking is disabled");
                return null;
            }
                
            try
            {
                // Convert relation to provenance metadata
                var metadata = relation.ToProvenanceMetadata();
                
                // Store provenance metadata
                var success = await RecordProvenanceAsync(metadata, tenantId, cancellationToken);
                
                return success ? metadata : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error recording provenance for relation {RelationId} in tenant {TenantId}: {ErrorMessage}",
                    relation.Id,
                    tenantId,
                    ex.Message);
                    
                return null;
            }
        }
        
        /// <summary>
        /// Gets provenance metadata for a specific element
        /// </summary>
        public async Task<ProvenanceMetadata> GetProvenanceAsync(
            string elementId,
            ProvenanceElementType elementType,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                return await _store.GetProvenanceAsync(
                    elementId,
                    elementType.ToString(),
                    tenantId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving provenance for element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    elementType,
                    elementId,
                    tenantId,
                    ex.Message);
                    
                return null;
            }
        }
        
        /// <summary>
        /// Queries provenance metadata records based on various filters
        /// </summary>
        public async Task<ProvenanceQueryResult> QueryProvenanceAsync(
            ProvenanceQuery query,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                return await _store.QueryProvenanceAsync(query, tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error querying provenance for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                return new ProvenanceQueryResult
                {
                    Query = query,
                    Results = new List<ProvenanceMetadata>(),
                    TotalCount = 0
                };
            }
        }
        
        /// <summary>
        /// Updates existing provenance metadata
        /// </summary>
        public async Task<bool> UpdateProvenanceAsync(
            ProvenanceMetadata metadata,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                return await _store.UpdateProvenanceAsync(metadata, tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating provenance for element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    metadata.ElementType,
                    metadata.ElementId,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Deletes provenance metadata for a specific element
        /// </summary>
        public async Task<bool> DeleteProvenanceAsync(
            string elementId,
            ProvenanceElementType elementType,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                return await _store.DeleteProvenanceAsync(
                    elementId,
                    elementType.ToString(),
                    tenantId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting provenance for element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    elementType,
                    elementId,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Gets the lineage (chain of dependencies) for a specific element
        /// </summary>
        public async Task<List<ProvenanceMetadata>> GetProvenanceLineageAsync(
            string elementId,
            ProvenanceElementType elementType,
            int maxDepth,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Cap max depth to configured limit if needed
                if (maxDepth > _options.MaxDependencyDepth)
                {
                    maxDepth = _options.MaxDependencyDepth;
                    
                    _logger.LogDebug(
                        "Capping lineage depth to configured maximum: {MaxDepth}",
                        maxDepth);
                }
                
                return await _store.GetProvenanceLineageAsync(
                    elementId,
                    elementType.ToString(),
                    maxDepth,
                    tenantId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving lineage for element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    elementType,
                    elementId,
                    tenantId,
                    ex.Message);
                    
                return new List<ProvenanceMetadata>();
            }
        }
        
        /// <summary>
        /// Gets all elements derived from a specific source
        /// </summary>
        public async Task<List<ProvenanceMetadata>> GetElementsFromSourceAsync(
            string sourceId,
            ProvenanceSourceType sourceType,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentNullException(nameof(sourceId));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                return await _store.GetElementsFromSourceAsync(
                    sourceId,
                    sourceType.ToString(),
                    tenantId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving elements from source {SourceType}:{SourceId} in tenant {TenantId}: {ErrorMessage}",
                    sourceType,
                    sourceId,
                    tenantId,
                    ex.Message);
                    
                return new List<ProvenanceMetadata>();
            }
        }
        
        /// <summary>
        /// Verifies a knowledge graph element
        /// </summary>
        public async Task<ProvenanceMetadata> VerifyElementAsync(
            string elementId,
            ProvenanceElementType elementType,
            string verifiedBy,
            string justification,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(verifiedBy))
                throw new ArgumentNullException(nameof(verifiedBy));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Get existing metadata
                var metadata = await _store.GetProvenanceAsync(
                    elementId,
                    elementType.ToString(),
                    tenantId,
                    cancellationToken);
                    
                if (metadata == null)
                {
                    _logger.LogWarning(
                        "Cannot verify element {ElementType}:{ElementId} in tenant {TenantId} - metadata not found",
                        elementType,
                        elementId,
                        tenantId);
                        
                    return null;
                }
                
                // Update verification details
                metadata.IsVerified = true;
                metadata.VerifiedBy = verifiedBy;
                metadata.VerifiedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(justification))
                {
                    metadata.Justification = justification;
                }
                
                // Update metadata
                var success = await _store.UpdateProvenanceAsync(metadata, tenantId, cancellationToken);
                
                if (success)
                {
                    _logger.LogInformation(
                        "Element {ElementType}:{ElementId} in tenant {TenantId} verified by {VerifiedBy}",
                        elementType,
                        elementId,
                        tenantId,
                        verifiedBy);
                        
                    return metadata;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error verifying element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    elementType,
                    elementId,
                    tenantId,
                    ex.Message);
                    
                return null;
            }
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            _store?.Dispose();
        }
    }
} 