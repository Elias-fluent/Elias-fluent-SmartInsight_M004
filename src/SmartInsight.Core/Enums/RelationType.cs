namespace SmartInsight.Core.Enums;

/// <summary>
/// Types of relations between entities in the knowledge graph
/// </summary>
public enum RelationType
{
    /// <summary>Entity is related to another entity</summary>
    RelatedTo = 1,
    
    /// <summary>Entity belongs to a category or group</summary>
    BelongsTo = 2,
    
    /// <summary>Entity is a part of another entity</summary>
    PartOf = 3,
    
    /// <summary>Entity created another entity</summary>
    CreatedBy = 4,
    
    /// <summary>Entity references another entity</summary>
    References = 5,
    
    /// <summary>Entity is derived from another entity</summary>
    DerivedFrom = 6,
    
    /// <summary>Entity depends on another entity</summary>
    DependsOn = 7,
    
    /// <summary>Entity is similar to another entity</summary>
    SimilarTo = 8,
    
    /// <summary>Entity is the parent of another entity</summary>
    ParentOf = 9,
    
    /// <summary>Entity is a child of another entity</summary>
    ChildOf = 10,
    
    /// <summary>Entity is a synonym of another entity</summary>
    SynonymOf = 11,
    
    /// <summary>Entity is an antonym of another entity</summary>
    AntonymOf = 12,
    
    /// <summary>Entity is an instance of another entity (type/class relationship)</summary>
    InstanceOf = 13,
    
    /// <summary>Custom relation type</summary>
    Custom = 99
} 