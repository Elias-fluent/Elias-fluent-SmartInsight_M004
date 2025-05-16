using System;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models
{
    /// <summary>
    /// Defines the types of knowledge graph elements that can have provenance tracking
    /// </summary>
    public enum ProvenanceElementType
    {
        /// <summary>
        /// Triple in the knowledge graph (subject-predicate-object)
        /// </summary>
        Triple = 0,
        
        /// <summary>
        /// Entity extracted from content
        /// </summary>
        Entity = 1,
        
        /// <summary>
        /// Relation between entities
        /// </summary>
        Relation = 2,
        
        /// <summary>
        /// Named graph in the triple store
        /// </summary>
        Graph = 3,
        
        /// <summary>
        /// Taxonomy term or class
        /// </summary>
        TaxonomyTerm = 4,
        
        /// <summary>
        /// Document or file ingested into the system
        /// </summary>
        Document = 5,
        
        /// <summary>
        /// Query result or inference
        /// </summary>
        Inference = 6,
        
        /// <summary>
        /// User-defined annotation
        /// </summary>
        Annotation = 7,
        
        /// <summary>
        /// Data transformation or processing step
        /// </summary>
        Transformation = 8,
        
        /// <summary>
        /// Custom or domain-specific element type
        /// </summary>
        Custom = 9
    }
} 