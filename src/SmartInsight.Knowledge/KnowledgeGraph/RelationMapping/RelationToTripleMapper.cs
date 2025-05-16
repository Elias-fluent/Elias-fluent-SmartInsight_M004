using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping
{
    /// <summary>
    /// Converts relations into triples for storage in the triple store
    /// </summary>
    public class RelationToTripleMapper : IRelationToTripleMapper
    {
        private readonly ILogger<RelationToTripleMapper> _logger;
        private readonly ITripleStore _tripleStore;
        
        private readonly Dictionary<RelationType, string> _relationTypeToPredicateUri = new Dictionary<RelationType, string>
        {
            { RelationType.AssociatedWith, "http://smartinsight.com/ontology/associatedWith" },
            { RelationType.WorksFor, "http://smartinsight.com/ontology/worksFor" },
            { RelationType.LocatedIn, "http://smartinsight.com/ontology/locatedIn" },
            { RelationType.HeadquarteredIn, "http://smartinsight.com/ontology/headquarteredIn" },
            { RelationType.HasTitle, "http://smartinsight.com/ontology/hasTitle" },
            { RelationType.HasSkill, "http://smartinsight.com/ontology/hasSkill" },
            { RelationType.Created, "http://smartinsight.com/ontology/created" },
            { RelationType.PartOf, "http://smartinsight.com/ontology/partOf" },
            { RelationType.Owns, "http://smartinsight.com/ontology/owns" },
            { RelationType.SubsidiaryOf, "http://smartinsight.com/ontology/subsidiaryOf" },
            { RelationType.AuthorOf, "http://smartinsight.com/ontology/authorOf" },
            { RelationType.Leads, "http://smartinsight.com/ontology/leads" },
            { RelationType.ParticipatesIn, "http://smartinsight.com/ontology/participatesIn" },
            { RelationType.OccurredBefore, "http://smartinsight.com/ontology/occurredBefore" },
            { RelationType.OccurredAfter, "http://smartinsight.com/ontology/occurredAfter" },
            { RelationType.DomainSpecific, "http://smartinsight.com/ontology/domainSpecific" },
            { RelationType.Uses, "http://smartinsight.com/ontology/uses" },
            { RelationType.DependsOn, "http://smartinsight.com/ontology/dependsOn" },
            { RelationType.SimilarTo, "http://smartinsight.com/ontology/similarTo" },
            { RelationType.References, "http://smartinsight.com/ontology/references" },
            { RelationType.SynonymOf, "http://smartinsight.com/ontology/synonymOf" },
            { RelationType.ParentCategoryOf, "http://smartinsight.com/ontology/parentCategoryOf" },
            { RelationType.SubcategoryOf, "http://smartinsight.com/ontology/subcategoryOf" },
            { RelationType.ColumnOf, "http://smartinsight.com/ontology/columnOf" },
            { RelationType.TableOf, "http://smartinsight.com/ontology/tableOf" },
            { RelationType.HasAttribute, "http://smartinsight.com/ontology/hasAttribute" },
            { RelationType.Other, "http://smartinsight.com/ontology/hasRelation" }
        };
        
        /// <summary>
        /// Initializes a new instance of the RelationToTripleMapper class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="tripleStore">The triple store instance</param>
        public RelationToTripleMapper(
            ILogger<RelationToTripleMapper> logger,
            ITripleStore tripleStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tripleStore = tripleStore ?? throw new ArgumentNullException(nameof(tripleStore));
        }
        
        /// <summary>
        /// Maps a relation to a triple and stores it in the triple store
        /// </summary>
        /// <param name="relation">The relation to map</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional graph URI for the triple</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> MapAndStoreAsync(
            Relation relation, 
            string tenantId, 
            string graphUri = null, 
            CancellationToken cancellationToken = default)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
            
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            
            try
            {
                // Create a triple from the relation
                var triple = MapToTriple(relation, tenantId, graphUri);
                
                // Store the triple in the triple store
                return await _tripleStore.AddTripleAsync(triple, tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error mapping and storing relation {RelationId} for tenant {TenantId}: {ErrorMessage}",
                    relation.Id,
                    tenantId,
                    ex.Message);
                    
                return false;
            }
        }
        
        /// <summary>
        /// Maps multiple relations to triples and stores them in the triple store
        /// </summary>
        /// <param name="relations">The relations to map</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional graph URI for the triples</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of triples successfully stored</returns>
        public async Task<int> MapAndStoreBatchAsync(
            IEnumerable<Relation> relations, 
            string tenantId, 
            string graphUri = null, 
            CancellationToken cancellationToken = default)
        {
            if (relations == null)
                throw new ArgumentNullException(nameof(relations));
            
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            
            try
            {
                // Map relations to triples
                var triples = new List<Triple>();
                
                foreach (var relation in relations)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    try
                    {
                        var triple = MapToTriple(relation, tenantId, graphUri);
                        triples.Add(triple);
                        
                        // If the relation is bidirectional, add the inverse triple
                        if (!relation.IsDirectional)
                        {
                            var inverseTriple = CreateInverseTriple(triple);
                            triples.Add(inverseTriple);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error mapping relation {RelationId} for tenant {TenantId}: {ErrorMessage}",
                            relation.Id,
                            tenantId,
                            ex.Message);
                    }
                }
                
                // Store the triples in the triple store
                return await _tripleStore.AddTriplesAsync(triples, tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error mapping and storing batch of relations for tenant {TenantId}: {ErrorMessage}",
                    tenantId,
                    ex.Message);
                    
                return 0;
            }
        }
        
        /// <summary>
        /// Maps a relation to a triple
        /// </summary>
        /// <param name="relation">The relation to map</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="graphUri">Optional graph URI for the triple</param>
        /// <returns>The mapped triple</returns>
        private Triple MapToTriple(Relation relation, string tenantId, string graphUri = null)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
                
            // Get the predicate URI for the relation type
            string predicateUri;
            
            if (relation.RelationType == RelationType.DomainSpecific && !string.IsNullOrEmpty(relation.RelationName))
            {
                // For domain-specific relations, use the relation name in the URI
                predicateUri = $"http://smartinsight.com/ontology/domain/{Uri.EscapeDataString(relation.RelationName)}";
            }
            else if (_relationTypeToPredicateUri.TryGetValue(relation.RelationType, out var uri))
            {
                predicateUri = uri;
            }
            else
            {
                // Default URI for unknown relation types
                predicateUri = "http://smartinsight.com/ontology/hasRelation";
                
                _logger.LogWarning(
                    "Unknown relation type {RelationType} for relation {RelationId}. Using default predicate URI.",
                    relation.RelationType,
                    relation.Id);
            }
            
            // Create the triple
            var triple = new Triple
            {
                Id = relation.Id, // Reuse the relation ID for traceability
                TenantId = tenantId,
                SubjectId = relation.SourceEntityId,
                PredicateUri = predicateUri,
                ObjectId = relation.TargetEntityId,
                IsLiteral = false,
                GraphUri = graphUri ?? $"http://smartinsight.com/graph/tenant/{tenantId}",
                ConfidenceScore = relation.ConfidenceScore,
                CreatedAt = relation.CreatedAt,
                UpdatedAt = relation.UpdatedAt,
                IsVerified = relation.IsVerified,
                Version = relation.Version,
                SourceDocumentId = relation.SourceDocumentId,
                ProvenanceInfo = new Dictionary<string, object>
                {
                    { "SourceContext", relation.SourceContext },
                    { "ExtractionMethod", relation.ExtractionMethod }
                }
            };
            
            // Add any additional attributes from the relation
            if (relation.Attributes != null && relation.Attributes.Count > 0)
            {
                foreach (var attr in relation.Attributes)
                {
                    triple.ProvenanceInfo[attr.Key] = attr.Value;
                }
            }
            
            return triple;
        }
        
        /// <summary>
        /// Creates an inverse triple for bidirectional relations
        /// </summary>
        /// <param name="triple">The original triple</param>
        /// <returns>The inverse triple</returns>
        private Triple CreateInverseTriple(Triple triple)
        {
            return new Triple
            {
                Id = $"{triple.Id}_inverse",
                TenantId = triple.TenantId,
                SubjectId = triple.ObjectId,
                PredicateUri = triple.PredicateUri,
                ObjectId = triple.SubjectId,
                IsLiteral = triple.IsLiteral,
                GraphUri = triple.GraphUri,
                ConfidenceScore = triple.ConfidenceScore,
                CreatedAt = triple.CreatedAt,
                UpdatedAt = triple.UpdatedAt,
                IsVerified = triple.IsVerified,
                Version = triple.Version,
                SourceDocumentId = triple.SourceDocumentId,
                ProvenanceInfo = new Dictionary<string, object>(triple.ProvenanceInfo)
            };
        }
    }
} 