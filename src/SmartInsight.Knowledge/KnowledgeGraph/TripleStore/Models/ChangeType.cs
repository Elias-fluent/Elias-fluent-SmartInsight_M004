namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Defines the types of changes that can be made to triples in the knowledge graph
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// A new triple was created
        /// </summary>
        Creation = 0,
        
        /// <summary>
        /// An existing triple was updated
        /// </summary>
        Update = 1,
        
        /// <summary>
        /// A triple was marked as deleted (soft delete)
        /// </summary>
        Deletion = 2,
        
        /// <summary>
        /// A triple was restored from a deleted state
        /// </summary>
        Restoration = 3,
        
        /// <summary>
        /// A triple's metadata was updated without changing its core S-P-O values
        /// </summary>
        MetadataUpdate = 4,
        
        /// <summary>
        /// A triple was verified by a user or system
        /// </summary>
        Verification = 5,
        
        /// <summary>
        /// A triple was moved between different graphs
        /// </summary>
        GraphMigration = 6,
        
        /// <summary>
        /// A triple was merged with another triple
        /// </summary>
        Merge = 7,
        
        /// <summary>
        /// A triple was split into multiple triples
        /// </summary>
        Split = 8,
        
        /// <summary>
        /// A correction was made to fix an error
        /// </summary>
        Correction = 9
    }
} 