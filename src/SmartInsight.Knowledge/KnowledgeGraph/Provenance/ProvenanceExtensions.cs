using System;
using System.Collections.Generic;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance
{
    /// <summary>
    /// Extension methods for working with provenance metadata
    /// </summary>
    public static class ProvenanceExtensions
    {
        /// <summary>
        /// Creates provenance metadata from a triple
        /// </summary>
        /// <param name="triple">The triple to extract provenance from</param>
        /// <returns>ProvenanceMetadata populated from the triple</returns>
        public static ProvenanceMetadata ToProvenanceMetadata(this Triple triple)
        {
            if (triple == null)
                throw new ArgumentNullException(nameof(triple));
                
            var metadata = new ProvenanceMetadata
            {
                ElementId = triple.Id,
                ElementType = ProvenanceElementType.Triple.ToString(),
                TenantId = triple.TenantId,
                ConfidenceScore = triple.ConfidenceScore,
                CreatedAt = triple.CreatedAt,
                UpdatedAt = triple.UpdatedAt,
                IsVerified = triple.IsVerified,
                Version = triple.Version
            };
            
            // Set up source reference
            metadata.Source = new SourceReference
            {
                SourceId = triple.SourceDocumentId,
                SourceType = !string.IsNullOrEmpty(triple.SourceDocumentId) 
                    ? ProvenanceSourceType.Document.ToString() 
                    : ProvenanceSourceType.System.ToString()
            };
            
            // Transfer existing provenance info from the triple if available
            if (triple.ProvenanceInfo != null)
            {
                // Copy attributes
                foreach (var kvp in triple.ProvenanceInfo)
                {
                    // Skip null values
                    if (kvp.Value == null)
                        continue;
                    
                    // Special handling for known provenance fields
                    switch (kvp.Key)
                    {
                        case "ExtractionMethod":
                            metadata.ExtractionMethod = kvp.Value.ToString();
                            break;
                        case "VerifiedBy":
                            metadata.VerifiedBy = kvp.Value.ToString();
                            break;
                        case "VerifiedAt":
                            if (kvp.Value is DateTime dt)
                                metadata.VerifiedAt = dt;
                            break;
                        case "Justification":
                            metadata.Justification = kvp.Value.ToString();
                            break;
                        case "SourceType":
                            metadata.Source.SourceType = kvp.Value.ToString();
                            break;
                        case "ConnectorName":
                            metadata.Source.ConnectorName = kvp.Value.ToString();
                            break;
                        case "TextContext":
                            metadata.Source.TextContext = kvp.Value.ToString();
                            break;
                        case "StartPosition":
                            if (kvp.Value is int sp)
                                metadata.Source.StartPosition = sp;
                            break;
                        case "EndPosition":
                            if (kvp.Value is int ep)
                                metadata.Source.EndPosition = ep;
                            break;
                        default:
                            // Add to custom attributes
                            metadata.Attributes[kvp.Key] = kvp.Value;
                            break;
                    }
                }
            }
            
            return metadata;
        }
        
        /// <summary>
        /// Creates provenance metadata from an entity
        /// </summary>
        /// <param name="entity">The entity to extract provenance from</param>
        /// <returns>ProvenanceMetadata populated from the entity</returns>
        public static ProvenanceMetadata ToProvenanceMetadata(this Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            var metadata = new ProvenanceMetadata
            {
                ElementId = entity.Id,
                ElementType = ProvenanceElementType.Entity.ToString(),
                TenantId = entity.TenantId,
                ConfidenceScore = entity.ConfidenceScore,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsVerified = entity.IsVerified,
                Version = entity.Version
            };
            
            // Set up source reference
            metadata.Source = new SourceReference
            {
                SourceId = entity.SourceId,
                TextContext = entity.OriginalContext,
                StartPosition = entity.StartPosition,
                EndPosition = entity.EndPosition,
                SourceType = !string.IsNullOrEmpty(entity.SourceId) 
                    ? ProvenanceSourceType.Document.ToString() 
                    : ProvenanceSourceType.System.ToString()
            };
            
            // Transfer attributes
            if (entity.Attributes != null)
            {
                foreach (var kvp in entity.Attributes)
                {
                    if (kvp.Value != null)
                    {
                        metadata.Attributes[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return metadata;
        }
        
        /// <summary>
        /// Creates provenance metadata from a relation
        /// </summary>
        /// <param name="relation">The relation to extract provenance from</param>
        /// <returns>ProvenanceMetadata populated from the relation</returns>
        public static ProvenanceMetadata ToProvenanceMetadata(this Relation relation)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
                
            var metadata = new ProvenanceMetadata
            {
                ElementId = relation.Id,
                ElementType = ProvenanceElementType.Relation.ToString(),
                TenantId = relation.TenantId,
                ConfidenceScore = relation.ConfidenceScore,
                CreatedAt = relation.CreatedAt,
                UpdatedAt = relation.UpdatedAt,
                IsVerified = relation.IsVerified,
                Version = relation.Version,
                ExtractionMethod = relation.ExtractionMethod
            };
            
            // Set up source reference
            metadata.Source = new SourceReference
            {
                SourceId = relation.SourceDocumentId,
                TextContext = relation.SourceContext,
                SourceType = !string.IsNullOrEmpty(relation.SourceDocumentId) 
                    ? ProvenanceSourceType.Document.ToString() 
                    : ProvenanceSourceType.System.ToString()
            };
            
            // Add dependencies (source and target entities)
            if (!string.IsNullOrEmpty(relation.SourceEntityId))
            {
                metadata.Dependencies.Add(new DependencyReference
                {
                    DependencyId = relation.SourceEntityId,
                    DependencyType = ProvenanceElementType.Entity.ToString(),
                    RelationshipType = "SourceEntity",
                    ConfidenceScore = 1.0
                });
            }
            
            if (!string.IsNullOrEmpty(relation.TargetEntityId))
            {
                metadata.Dependencies.Add(new DependencyReference
                {
                    DependencyId = relation.TargetEntityId,
                    DependencyType = ProvenanceElementType.Entity.ToString(),
                    RelationshipType = "TargetEntity",
                    ConfidenceScore = 1.0
                });
            }
            
            // Transfer attributes
            if (relation.Attributes != null)
            {
                foreach (var kvp in relation.Attributes)
                {
                    if (kvp.Value != null)
                    {
                        metadata.Attributes[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return metadata;
        }
        
        /// <summary>
        /// Updates a triple with provenance information from metadata
        /// </summary>
        /// <param name="triple">The triple to update</param>
        /// <param name="metadata">The provenance metadata to apply</param>
        /// <returns>The updated triple</returns>
        public static Triple ApplyProvenanceMetadata(this Triple triple, ProvenanceMetadata metadata)
        {
            if (triple == null)
                throw new ArgumentNullException(nameof(triple));
                
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
                
            // Apply basic provenance fields
            triple.ConfidenceScore = metadata.ConfidenceScore;
            triple.IsVerified = metadata.IsVerified;
            triple.SourceDocumentId = metadata.Source?.SourceId;
            triple.Version = metadata.Version;
            
            // Initialize ProvenanceInfo if null
            if (triple.ProvenanceInfo == null)
            {
                triple.ProvenanceInfo = new Dictionary<string, object>();
            }
            
            // Add standard fields to ProvenanceInfo
            triple.ProvenanceInfo["ExtractionMethod"] = metadata.ExtractionMethod;
            triple.ProvenanceInfo["VerifiedBy"] = metadata.VerifiedBy;
            triple.ProvenanceInfo["VerifiedAt"] = metadata.VerifiedAt;
            triple.ProvenanceInfo["Justification"] = metadata.Justification;
            
            // Add source details
            if (metadata.Source != null)
            {
                triple.ProvenanceInfo["SourceType"] = metadata.Source.SourceType;
                triple.ProvenanceInfo["ConnectorName"] = metadata.Source.ConnectorName;
                triple.ProvenanceInfo["IngestionTimestamp"] = metadata.Source.IngestionTimestamp;
                triple.ProvenanceInfo["TextContext"] = metadata.Source.TextContext;
                triple.ProvenanceInfo["StartPosition"] = metadata.Source.StartPosition;
                triple.ProvenanceInfo["EndPosition"] = metadata.Source.EndPosition;
                
                // Add source attributes
                if (metadata.Source.SourceAttributes != null)
                {
                    foreach (var kvp in metadata.Source.SourceAttributes)
                    {
                        triple.ProvenanceInfo[$"Source_{kvp.Key}"] = kvp.Value;
                    }
                }
            }
            
            // Add custom attributes
            if (metadata.Attributes != null)
            {
                foreach (var kvp in metadata.Attributes)
                {
                    triple.ProvenanceInfo[kvp.Key] = kvp.Value;
                }
            }
            
            // Add dependencies
            if (metadata.Dependencies != null && metadata.Dependencies.Count > 0)
            {
                triple.ProvenanceInfo["Dependencies"] = metadata.Dependencies;
            }
            
            return triple;
        }
    }
} 