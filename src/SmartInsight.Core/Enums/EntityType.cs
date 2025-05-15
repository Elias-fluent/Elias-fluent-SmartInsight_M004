namespace SmartInsight.Core.Enums;

/// <summary>
/// Types of entities that can be extracted and stored in the knowledge graph
/// </summary>
public enum EntityType
{
    /// <summary>A person</summary>
    Person = 1,
    
    /// <summary>An organization or company</summary>
    Organization = 2,
    
    /// <summary>A location or place</summary>
    Location = 3,
    
    /// <summary>A date or time reference</summary>
    DateTime = 4,
    
    /// <summary>A product or service</summary>
    Product = 5,
    
    /// <summary>A technical concept or term</summary>
    Concept = 6,
    
    /// <summary>A document or file</summary>
    Document = 7,
    
    /// <summary>A numeric value or statistic</summary>
    Numeric = 8,
    
    /// <summary>An event that occurred</summary>
    Event = 9,
    
    /// <summary>Software code or programming construct</summary>
    Code = 10,
    
    /// <summary>Database object (table, view, etc.)</summary>
    DatabaseObject = 11,
    
    /// <summary>API or service reference</summary>
    ApiResource = 12,
    
    /// <summary>Custom entity type</summary>
    Custom = 99
} 