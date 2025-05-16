using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance
{
    /// <summary>
    /// Storage mechanism for provenance metadata
    /// </summary>
    public class ProvenanceStore : IDisposable
    {
        private readonly ILogger<ProvenanceStore> _logger;
        private readonly ProvenanceTrackerOptions _options;
        
        // In-memory storage for development/testing - would be replaced with database in production
        private readonly Dictionary<string, Dictionary<string, ProvenanceMetadata>> _provenanceByTenant = 
            new Dictionary<string, Dictionary<string, ProvenanceMetadata>>();
            
        /// <summary>
        /// Initializes a new instance of the ProvenanceStore class
        /// </summary>
        public ProvenanceStore(
            ILogger<ProvenanceStore> logger,
            IOptions<ProvenanceTrackerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _logger.LogInformation("Initialized provenance store");
        }
        
        /// <summary>
        /// Stores provenance metadata
        /// </summary>
        public async Task<bool> StoreProvenanceAsync(
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
                // Ensure tenant ID is set and matches
                metadata.TenantId = tenantId;
                
                // Apply text context truncation if enabled
                if (_options.StoreFullTextContext && 
                    metadata.Source?.TextContext != null && 
                    metadata.Source.TextContext.Length > _options.MaxTextContextLength)
                {
                    metadata.Source.TextContext = metadata.Source.TextContext.Substring(0, _options.MaxTextContextLength);
                }
                
                // Ensure tenant dictionary exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    tenantDict = new Dictionary<string, ProvenanceMetadata>();
                    _provenanceByTenant[tenantId] = tenantDict;
                }
                
                // Generate composite key from element type and ID
                var key = GetCompositeKey(metadata.ElementId, metadata.ElementType);
                
                // Store metadata
                tenantDict[key] = metadata;
                
                _logger.LogDebug(
                    "Stored provenance for element {ElementType}:{ElementId} in tenant {TenantId}",
                    metadata.ElementType,
                    metadata.ElementId,
                    tenantId);
                    
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error storing provenance for element {ElementType}:{ElementId} in tenant {TenantId}: {ErrorMessage}",
                    metadata.ElementType,
                    metadata.ElementId,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Retrieves provenance metadata for a specific element
        /// </summary>
        public async Task<ProvenanceMetadata> GetProvenanceAsync(
            string elementId,
            string elementType,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(elementType))
                throw new ArgumentNullException(nameof(elementType));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Check if tenant exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    return null;
                }
                
                // Generate composite key
                var key = GetCompositeKey(elementId, elementType);
                
                // Retrieve metadata
                if (tenantDict.TryGetValue(key, out var metadata))
                {
                    return metadata;
                }
                
                return null;
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
                // Ensure tenant ID is set and matches
                metadata.TenantId = tenantId;
                
                // Check if tenant exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    return false;
                }
                
                // Generate composite key
                var key = GetCompositeKey(metadata.ElementId, metadata.ElementType);
                
                // Check if metadata exists
                if (!tenantDict.ContainsKey(key))
                {
                    return false;
                }
                
                // Update timestamp
                metadata.UpdatedAt = DateTime.UtcNow;
                
                // Store updated metadata
                tenantDict[key] = metadata;
                
                _logger.LogDebug(
                    "Updated provenance for element {ElementType}:{ElementId} in tenant {TenantId}",
                    metadata.ElementType,
                    metadata.ElementId,
                    tenantId);
                    
                return true;
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
            string elementType,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(elementType))
                throw new ArgumentNullException(nameof(elementType));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Check if tenant exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    return false;
                }
                
                // Generate composite key
                var key = GetCompositeKey(elementId, elementType);
                
                // Delete metadata
                if (tenantDict.Remove(key))
                {
                    _logger.LogDebug(
                        "Deleted provenance for element {ElementType}:{ElementId} in tenant {TenantId}",
                        elementType,
                        elementId,
                        tenantId);
                        
                    return true;
                }
                
                return false;
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
        /// Queries provenance metadata based on various filters
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
                var result = new ProvenanceQueryResult { Query = query };
                
                // Check if tenant exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    result.Results = new List<ProvenanceMetadata>();
                    result.TotalCount = 0;
                    return result;
                }
                
                // Apply filters
                var filteredMetadata = tenantDict.Values.AsEnumerable();
                
                // Element ID filter
                if (!string.IsNullOrEmpty(query.ElementId))
                {
                    filteredMetadata = filteredMetadata.Where(m => m.ElementId == query.ElementId);
                }
                
                // Element type filter
                if (query.ElementType.HasValue)
                {
                    var typeStr = query.ElementType.Value.ToString();
                    filteredMetadata = filteredMetadata.Where(m => m.ElementType == typeStr);
                }
                
                // Source ID filter
                if (!string.IsNullOrEmpty(query.SourceId))
                {
                    filteredMetadata = filteredMetadata.Where(m => m.Source?.SourceId == query.SourceId);
                }
                
                // Source type filter
                if (query.SourceType.HasValue)
                {
                    var typeStr = query.SourceType.Value.ToString();
                    filteredMetadata = filteredMetadata.Where(m => m.Source?.SourceType == typeStr);
                }
                
                // Connector name filter
                if (!string.IsNullOrEmpty(query.ConnectorName))
                {
                    filteredMetadata = filteredMetadata.Where(m => m.Source?.ConnectorName == query.ConnectorName);
                }
                
                // Confidence score filter
                if (query.MinConfidenceScore.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.ConfidenceScore >= query.MinConfidenceScore.Value);
                }
                
                // Verification status filter
                if (query.IsVerified.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.IsVerified == query.IsVerified.Value);
                }
                
                // Verified by filter
                if (!string.IsNullOrEmpty(query.VerifiedBy))
                {
                    filteredMetadata = filteredMetadata.Where(m => m.VerifiedBy == query.VerifiedBy);
                }
                
                // Extraction method filter
                if (!string.IsNullOrEmpty(query.ExtractionMethod))
                {
                    filteredMetadata = filteredMetadata.Where(m => m.ExtractionMethod == query.ExtractionMethod);
                }
                
                // Created date range filters
                if (query.CreatedAfter.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.CreatedAt >= query.CreatedAfter.Value);
                }
                
                if (query.CreatedBefore.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.CreatedAt <= query.CreatedBefore.Value);
                }
                
                // Updated date range filters
                if (query.UpdatedAfter.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.UpdatedAt >= query.UpdatedAfter.Value);
                }
                
                if (query.UpdatedBefore.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.UpdatedAt <= query.UpdatedBefore.Value);
                }
                
                // Version filter
                if (query.Version.HasValue)
                {
                    filteredMetadata = filteredMetadata.Where(m => m.Version == query.Version.Value);
                }
                
                // Justification text filter
                if (!string.IsNullOrEmpty(query.JustificationContains))
                {
                    filteredMetadata = filteredMetadata.Where(m => 
                        m.Justification != null && 
                        m.Justification.Contains(query.JustificationContains, StringComparison.OrdinalIgnoreCase));
                }
                
                // Dependencies filter
                if (query.DependencyIds != null && query.DependencyIds.Count > 0)
                {
                    filteredMetadata = filteredMetadata.Where(m => 
                        m.Dependencies != null && 
                        m.Dependencies.Any(d => query.DependencyIds.Contains(d.DependencyId)));
                }
                
                // Custom attribute filters
                if (query.AttributeFilters != null && query.AttributeFilters.Count > 0)
                {
                    foreach (var attributeFilter in query.AttributeFilters)
                    {
                        var key = attributeFilter.Key;
                        var value = attributeFilter.Value;
                        
                        filteredMetadata = filteredMetadata.Where(m => 
                            m.Attributes != null && 
                            m.Attributes.TryGetValue(key, out var attrValue) && 
                            (value == null ? attrValue == null : value.Equals(attrValue)));
                    }
                }
                
                // Count total results before pagination
                result.TotalCount = filteredMetadata.Count();
                
                // Apply sorting
                if (!string.IsNullOrEmpty(query.SortBy))
                {
                    filteredMetadata = ApplySorting(filteredMetadata, query.SortBy, query.SortAscending);
                }
                
                // Apply pagination
                filteredMetadata = filteredMetadata
                    .Skip(query.Skip)
                    .Take(query.MaxResults);
                
                // Store results
                result.Results = filteredMetadata.ToList();
                
                _logger.LogDebug(
                    "Query returned {ResultCount} provenance records out of {TotalCount} for tenant {TenantId}",
                    result.Results.Count,
                    result.TotalCount,
                    tenantId);
                    
                return result;
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
        /// Gets all elements derived from a specific source
        /// </summary>
        public async Task<List<ProvenanceMetadata>> GetElementsFromSourceAsync(
            string sourceId,
            string sourceType,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentNullException(nameof(sourceId));
                
            if (string.IsNullOrEmpty(sourceType))
                throw new ArgumentNullException(nameof(sourceType));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            try
            {
                // Check if tenant exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    return new List<ProvenanceMetadata>();
                }
                
                // Filter by source ID and type
                var results = tenantDict.Values
                    .Where(m => m.Source?.SourceId == sourceId && m.Source?.SourceType == sourceType)
                    .ToList();
                
                _logger.LogDebug(
                    "Found {ResultCount} elements from source {SourceType}:{SourceId} in tenant {TenantId}",
                    results.Count,
                    sourceType,
                    sourceId,
                    tenantId);
                    
                return results;
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
        /// Gets the lineage (chain of dependencies) for a specific element
        /// </summary>
        public async Task<List<ProvenanceMetadata>> GetProvenanceLineageAsync(
            string elementId,
            string elementType,
            int maxDepth,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(elementId))
                throw new ArgumentNullException(nameof(elementId));
                
            if (string.IsNullOrEmpty(elementType))
                throw new ArgumentNullException(nameof(elementType));
                
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
                
            if (maxDepth < 1)
                maxDepth = 1;
                
            try
            {
                // Check if tenant exists
                if (!_provenanceByTenant.TryGetValue(tenantId, out var tenantDict))
                {
                    return new List<ProvenanceMetadata>();
                }
                
                var lineage = new HashSet<string>(); // Composite keys of visited elements
                var result = new List<ProvenanceMetadata>();
                
                // Start with the target element
                await TraverseLineage(elementId, elementType, tenantDict, lineage, result, maxDepth, 0);
                
                _logger.LogDebug(
                    "Found lineage with {ResultCount} elements for {ElementType}:{ElementId} in tenant {TenantId}",
                    result.Count,
                    elementType,
                    elementId,
                    tenantId);
                    
                return result;
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
        /// Helper method to recursively traverse the dependency lineage
        /// </summary>
        private async Task TraverseLineage(
            string elementId,
            string elementType,
            Dictionary<string, ProvenanceMetadata> tenantDict,
            HashSet<string> visited,
            List<ProvenanceMetadata> result,
            int maxDepth,
            int currentDepth)
        {
            // Stop if max depth reached
            if (currentDepth > maxDepth)
                return;
                
            // Generate composite key
            var key = GetCompositeKey(elementId, elementType);
            
            // Skip if already visited to prevent cycles
            if (visited.Contains(key))
                return;
                
            // Mark as visited
            visited.Add(key);
            
            // Get element metadata
            if (tenantDict.TryGetValue(key, out var metadata))
            {
                // Add to result
                result.Add(metadata);
                
                // Process dependencies if any
                if (metadata.Dependencies != null)
                {
                    foreach (var dependency in metadata.Dependencies)
                    {
                        // Recursively traverse dependencies
                        await TraverseLineage(
                            dependency.DependencyId,
                            dependency.DependencyType,
                            tenantDict,
                            visited,
                            result,
                            maxDepth,
                            currentDepth + 1);
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies sorting to metadata records
        /// </summary>
        private IEnumerable<ProvenanceMetadata> ApplySorting(
            IEnumerable<ProvenanceMetadata> metadata,
            string sortBy,
            bool ascending)
        {
            // Apply sorting based on property name
            switch (sortBy.ToLowerInvariant())
            {
                case "id":
                    return ascending ? 
                        metadata.OrderBy(m => m.Id) : 
                        metadata.OrderByDescending(m => m.Id);
                        
                case "elementid":
                    return ascending ? 
                        metadata.OrderBy(m => m.ElementId) : 
                        metadata.OrderByDescending(m => m.ElementId);
                        
                case "elementtype":
                    return ascending ? 
                        metadata.OrderBy(m => m.ElementType) : 
                        metadata.OrderByDescending(m => m.ElementType);
                        
                case "confidencescore":
                    return ascending ? 
                        metadata.OrderBy(m => m.ConfidenceScore) : 
                        metadata.OrderByDescending(m => m.ConfidenceScore);
                        
                case "createdat":
                    return ascending ? 
                        metadata.OrderBy(m => m.CreatedAt) : 
                        metadata.OrderByDescending(m => m.CreatedAt);
                        
                case "updatedat":
                    return ascending ? 
                        metadata.OrderBy(m => m.UpdatedAt) : 
                        metadata.OrderByDescending(m => m.UpdatedAt);
                        
                case "isverified":
                    return ascending ? 
                        metadata.OrderBy(m => m.IsVerified) : 
                        metadata.OrderByDescending(m => m.IsVerified);
                        
                case "version":
                    return ascending ? 
                        metadata.OrderBy(m => m.Version) : 
                        metadata.OrderByDescending(m => m.Version);
                        
                default:
                    // Default to creation date
                    return ascending ? 
                        metadata.OrderBy(m => m.CreatedAt) : 
                        metadata.OrderByDescending(m => m.CreatedAt);
            }
        }
        
        /// <summary>
        /// Creates a composite key from element ID and type
        /// </summary>
        private string GetCompositeKey(string elementId, string elementType)
        {
            return $"{elementType}:{elementId}";
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            // Clean up any resources
            _provenanceByTenant.Clear();
        }
    }
}