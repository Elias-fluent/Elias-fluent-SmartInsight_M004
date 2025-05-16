using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore
{
    /// <summary>
    /// Extension methods for registering Triple Store services
    /// </summary>
    public static class TripleStoreExtensions
    {
        /// <summary>
        /// Adds the Triple Store services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="enableVersioning">Whether to enable versioning support (default: true)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTripleStore(
            this IServiceCollection services, 
            IConfiguration configuration, 
            bool enableVersioning = true)
        {
            // Register options
            services.Configure<TripleStoreOptions>(configuration.GetSection("TripleStore"));
            
            // Register versioning manager if enabled
            if (enableVersioning)
            {
                services.AddSingleton<IKnowledgeGraphVersioningManager, KnowledgeGraphVersioningManager>();
            }
            
            // Register the triple store implementation
            services.AddSingleton<ITripleStore, InMemoryTripleStore>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryTripleStore>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripleStoreOptions>>();
                var versioningManager = enableVersioning 
                    ? sp.GetRequiredService<IKnowledgeGraphVersioningManager>() 
                    : null;
                
                return new InMemoryTripleStore(logger, options, versioningManager);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds the Triple Store services to the service collection with custom options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <param name="enableVersioning">Whether to enable versioning support (default: true)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTripleStore(
            this IServiceCollection services, 
            Action<TripleStoreOptions> configureOptions,
            bool enableVersioning = true)
        {
            // Register options
            services.Configure(configureOptions);
            
            // Register versioning manager if enabled
            if (enableVersioning)
            {
                services.AddSingleton<IKnowledgeGraphVersioningManager, KnowledgeGraphVersioningManager>();
            }
            
            // Register the triple store implementation
            services.AddSingleton<ITripleStore, InMemoryTripleStore>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryTripleStore>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripleStoreOptions>>();
                var versioningManager = enableVersioning 
                    ? sp.GetRequiredService<IKnowledgeGraphVersioningManager>() 
                    : null;
                
                return new InMemoryTripleStore(logger, options, versioningManager);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds temporal query and versioning support to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTemporalQuerySupport(this IServiceCollection services)
        {
            // Ensure the versioning manager is registered for temporal queries
            if (!services.BuildServiceProvider().GetService<IKnowledgeGraphVersioningManager>().IsServiceRegistered())
            {
                services.AddSingleton<IKnowledgeGraphVersioningManager, KnowledgeGraphVersioningManager>();
            }
            
            return services;
        }
        
        /// <summary>
        /// Checks if a service is registered in the service collection
        /// </summary>
        private static bool IsServiceRegistered<T>(this T service)
        {
            return service != null;
        }
        
        /// <summary>
        /// Adds a triple with provenance tracking
        /// </summary>
        public static async Task<bool> AddTripleWithProvenanceAsync(
            this ITripleStore tripleStore,
            Triple triple,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            // Add triple to store
            var result = await tripleStore.AddTripleAsync(triple, tenantId, cancellationToken);
            
            // If successful, record provenance
            if (result && provenanceTracker != null)
            {
                await provenanceTracker.RecordTripleProvenanceAsync(triple, tenantId, cancellationToken);
            }
            
            return result;
        }
        
        /// <summary>
        /// Adds multiple triples with provenance tracking
        /// </summary>
        public static async Task<int> AddTriplesWithProvenanceAsync(
            this ITripleStore tripleStore,
            IEnumerable<Triple> triples,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            // Add triples to store
            var result = await tripleStore.AddTriplesAsync(triples, tenantId, cancellationToken);
            
            // If successful, record provenance for each triple
            if (result > 0 && provenanceTracker != null)
            {
                foreach (var triple in triples)
                {
                    await provenanceTracker.RecordTripleProvenanceAsync(triple, tenantId, cancellationToken);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets triples with provenance information
        /// </summary>
        public static async Task<IEnumerable<(Triple Triple, ProvenanceMetadata Provenance)>> GetTriplesWithProvenanceAsync(
            this ITripleStore tripleStore,
            TripleQuery query,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            // Get triples
            var queryResult = await tripleStore.QueryAsync(query, tenantId, cancellationToken);
            var triples = queryResult?.Triples ?? Enumerable.Empty<Triple>();
            
            // If provenance tracker is available, get provenance for each triple
            if (provenanceTracker != null)
            {
                var result = new List<(Triple Triple, ProvenanceMetadata Provenance)>();
                
                foreach (var triple in triples)
                {
                    var provenance = await provenanceTracker.GetProvenanceAsync(
                        triple.Id,
                        ProvenanceElementType.Triple,
                        tenantId,
                        cancellationToken);
                        
                    result.Add((Triple: triple, Provenance: provenance));
                }
                
                return result;
            }
            
            // Otherwise, return triples with null provenance
            return triples.Select(triple => (Triple: triple, Provenance: (ProvenanceMetadata)null));
        }
        
        /// <summary>
        /// Updates a triple with provenance tracking
        /// </summary>
        public static async Task<bool> UpdateTripleWithProvenanceAsync(
            this ITripleStore tripleStore,
            Triple triple,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            // Update triple in store
            var result = await tripleStore.UpdateTripleAsync(triple, tenantId, cancellationToken);
            
            // If successful, update provenance
            if (result && provenanceTracker != null)
            {
                // Get existing provenance
                var provenance = await provenanceTracker.GetProvenanceAsync(
                    triple.Id,
                    ProvenanceElementType.Triple,
                    tenantId,
                    cancellationToken);
                    
                if (provenance != null)
                {
                    // Update provenance from triple
                    var updatedProvenance = triple.ToProvenanceMetadata();
                    
                    // Preserve existing provenance data
                    updatedProvenance.Id = provenance.Id;
                    updatedProvenance.CreatedAt = provenance.CreatedAt;
                    updatedProvenance.Dependencies = provenance.Dependencies;
                    
                    // Apply update
                    await provenanceTracker.UpdateProvenanceAsync(
                        updatedProvenance,
                        tenantId,
                        cancellationToken);
                }
                else
                {
                    // No existing provenance, create new
                    await provenanceTracker.RecordTripleProvenanceAsync(
                        triple,
                        tenantId,
                        cancellationToken);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Removes a triple with provenance tracking
        /// </summary>
        public static async Task<bool> RemoveTripleWithProvenanceAsync(
            this ITripleStore tripleStore,
            string tripleId,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            // Remove triple from store
            var result = await tripleStore.RemoveTripleAsync(tripleId, tenantId, cancellationToken);
            
            // If successful, delete provenance
            if (result && provenanceTracker != null)
            {
                await provenanceTracker.DeleteProvenanceAsync(
                    tripleId,
                    ProvenanceElementType.Triple,
                    tenantId,
                    cancellationToken);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets provenance lineage for a triple
        /// </summary>
        public static async Task<List<ProvenanceMetadata>> GetTripleProvenanceLineageAsync(
            this ITripleStore tripleStore,
            string tripleId,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            int maxDepth = 5,
            CancellationToken cancellationToken = default)
        {
            if (provenanceTracker == null)
            {
                return new List<ProvenanceMetadata>();
            }
            
            return await provenanceTracker.GetProvenanceLineageAsync(
                tripleId,
                ProvenanceElementType.Triple,
                maxDepth,
                tenantId,
                cancellationToken);
        }
        
        /// <summary>
        /// Queries triples by provenance criteria
        /// </summary>
        public static async Task<IEnumerable<Triple>> QueryTriplesByProvenanceAsync(
            this ITripleStore tripleStore,
            ProvenanceQuery provenanceQuery,
            IProvenanceTracker provenanceTracker,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (provenanceTracker == null)
            {
                return Enumerable.Empty<Triple>();
            }
            
            // Force element type filter to Triple
            provenanceQuery.ElementType = ProvenanceElementType.Triple;
            
            // Query provenance
            var provenanceResult = await provenanceTracker.QueryProvenanceAsync(
                provenanceQuery,
                tenantId,
                cancellationToken);
                
            if (provenanceResult == null || provenanceResult.Results.Count == 0)
            {
                return Enumerable.Empty<Triple>();
            }
            
            // Extract triple IDs
            var tripleIds = provenanceResult.Results
                .Select(p => p.ElementId)
                .ToList();
                
            // Get triples by IDs (using query for each ID since there's no direct GetTripleByIdAsync)
            var triples = new List<Triple>();
            foreach (var id in tripleIds)
            {
                // Create a query that filters by subject ID, predicate URI, and object ID to try to match by ID
                // Since TripleQuery doesn't have a direct ID field, we need to test different combinations
                var query = new TripleQuery 
                { 
                    SubjectId = id,
                    TenantId = tenantId
                };
                
                var result = await tripleStore.QueryAsync(query, tenantId, cancellationToken);
                
                if (result?.Triples.Count > 0)
                {
                    var matchingTriple = result.Triples.FirstOrDefault(t => t.Id == id);
                    if (matchingTriple != null)
                    {
                        triples.Add(matchingTriple);
                    }
                }
            }
            
            return triples;
        }
        
        /// <summary>
        /// Verifies a triple with provenance tracking
        /// </summary>
        public static async Task<bool> VerifyTripleAsync(
            this ITripleStore tripleStore,
            Triple triple,
            IProvenanceTracker provenanceTracker,
            string verifiedBy,
            string justification,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (provenanceTracker == null)
            {
                return false;
            }
            
            // Update triple verification status
            triple.IsVerified = true;
            var updateResult = await tripleStore.UpdateTripleAsync(triple, tenantId, cancellationToken);
            
            if (!updateResult)
            {
                return false;
            }
            
            // Update provenance verification
            var provenanceResult = await provenanceTracker.VerifyElementAsync(
                triple.Id,
                ProvenanceElementType.Triple,
                verifiedBy,
                justification,
                tenantId,
                cancellationToken);
                
            return provenanceResult != null;
        }
    }
} 