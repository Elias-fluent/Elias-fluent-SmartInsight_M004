namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models
{
    /// <summary>
    /// Defines the types of relationships that can exist between entities
    /// </summary>
    public enum RelationType
    {
        /// <summary>
        /// Entity A is associated with Entity B without a specific relation type
        /// </summary>
        AssociatedWith,
        
        /// <summary>
        /// Person A works for Organization B
        /// </summary>
        WorksFor,
        
        /// <summary>
        /// Person A is located in Location B
        /// </summary>
        LocatedIn,
        
        /// <summary>
        /// Organization A is headquartered in Location B
        /// </summary>
        HeadquarteredIn,
        
        /// <summary>
        /// Person A has Job Title B
        /// </summary>
        HasTitle,
        
        /// <summary>
        /// Person A has Skill B
        /// </summary>
        HasSkill,
        
        /// <summary>
        /// Person/Organization A created Product B
        /// </summary>
        Created,
        
        /// <summary>
        /// Entity A is a part of Entity B
        /// </summary>
        PartOf,
        
        /// <summary>
        /// Entity A owns Entity B
        /// </summary>
        Owns,
        
        /// <summary>
        /// Organization A is a subsidiary of Organization B
        /// </summary>
        SubsidiaryOf,
        
        /// <summary>
        /// Person A is the author of Document B
        /// </summary>
        AuthorOf,
        
        /// <summary>
        /// Person A leads Project B
        /// </summary>
        Leads,
        
        /// <summary>
        /// Person A participates in Project B
        /// </summary>
        ParticipatesIn,
        
        /// <summary>
        /// Entity A occurred before Entity B (temporal relation)
        /// </summary>
        OccurredBefore,
        
        /// <summary>
        /// Entity A occurred after Entity B (temporal relation)
        /// </summary>
        OccurredAfter,
        
        /// <summary>
        /// Entity A is related to Entity B in a domain-specific way
        /// </summary>
        DomainSpecific,
        
        /// <summary>
        /// Entity A uses Entity B
        /// </summary>
        Uses,
        
        /// <summary>
        /// Entity A depends on Entity B
        /// </summary>
        DependsOn,
        
        /// <summary>
        /// Entity A is similar to Entity B
        /// </summary>
        SimilarTo,
        
        /// <summary>
        /// Entity A references Entity B
        /// </summary>
        References,
        
        /// <summary>
        /// Entity A is a synonym of Entity B
        /// </summary>
        SynonymOf,
        
        /// <summary>
        /// Entity A is a parent category of Entity B
        /// </summary>
        ParentCategoryOf,
        
        /// <summary>
        /// Entity A is a subcategory of Entity B
        /// </summary>
        SubcategoryOf,
        
        /// <summary>
        /// Database column A belongs to database table B
        /// </summary>
        ColumnOf,
        
        /// <summary>
        /// Database table A belongs to database schema B
        /// </summary>
        TableOf,
        
        /// <summary>
        /// Entity A has attribute B with value C
        /// </summary>
        HasAttribute,
        
        /// <summary>
        /// Other relationship not covered by predefined types
        /// </summary>
        Other
    }
} 