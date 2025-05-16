namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Models
{
    /// <summary>
    /// Defines the types of nodes in the taxonomy hierarchy
    /// </summary>
    public enum TaxonomyNodeType
    {
        /// <summary>
        /// Represents a class or concept in the taxonomy
        /// </summary>
        Class,
        
        /// <summary>
        /// Represents a property or attribute of a class
        /// </summary>
        Property,
        
        /// <summary>
        /// Represents a relationship type between classes
        /// </summary>
        Relation,
        
        /// <summary>
        /// Represents a category grouping for organizing the taxonomy
        /// </summary>
        Category,
        
        /// <summary>
        /// Represents an instance or individual of a class
        /// </summary>
        Instance,
        
        /// <summary>
        /// Represents a rule or constraint in the taxonomy
        /// </summary>
        Rule,
        
        /// <summary>
        /// Represents a data type definition
        /// </summary>
        DataType
    }
} 