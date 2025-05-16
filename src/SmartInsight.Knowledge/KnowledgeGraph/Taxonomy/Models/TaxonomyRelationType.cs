namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models
{
    /// <summary>
    /// Defines the types of relationships between taxonomy nodes
    /// </summary>
    public enum TaxonomyRelationType
    {
        /// <summary>
        /// Represents a parent-child or superclass-subclass relationship (inheritance)
        /// </summary>
        IsA,
        
        /// <summary>
        /// Represents a part-whole relationship
        /// </summary>
        PartOf,
        
        /// <summary>
        /// Represents a relationship where one node has an attribute defined by another
        /// </summary>
        HasProperty,
        
        /// <summary>
        /// Represents a relationship where one node contains instances of another
        /// </summary>
        Contains,
        
        /// <summary>
        /// Represents a relationship where one node is related to another in a generic way
        /// </summary>
        RelatedTo,
        
        /// <summary>
        /// Represents a relationship where one node is synonymous with another
        /// </summary>
        SameAs,
        
        /// <summary>
        /// Represents a relationship where one node is the opposite of another
        /// </summary>
        OppositeOf,
        
        /// <summary>
        /// Represents a relationship where one node is a prerequisite for another
        /// </summary>
        Prerequisite,
        
        /// <summary>
        /// Represents a relationship where one node is mutually exclusive with another
        /// </summary>
        MutuallyExclusiveWith,
        
        /// <summary>
        /// Represents a custom defined relationship type
        /// </summary>
        Custom
    }
} 