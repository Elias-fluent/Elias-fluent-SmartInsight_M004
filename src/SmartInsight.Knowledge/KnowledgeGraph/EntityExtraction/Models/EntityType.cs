namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models
{
    /// <summary>
    /// Defines the types of entities that can be extracted
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// A person's name
        /// </summary>
        Person,
        
        /// <summary>
        /// An organization or company name
        /// </summary>
        Organization,
        
        /// <summary>
        /// A location or place
        /// </summary>
        Location,
        
        /// <summary>
        /// A date or time expression
        /// </summary>
        DateTime,
        
        /// <summary>
        /// A monetary amount
        /// </summary>
        Money,
        
        /// <summary>
        /// A percentage value
        /// </summary>
        Percentage,
        
        /// <summary>
        /// An email address
        /// </summary>
        Email,
        
        /// <summary>
        /// A phone number
        /// </summary>
        PhoneNumber,
        
        /// <summary>
        /// A URL or web address
        /// </summary>
        Url,
        
        /// <summary>
        /// A product name
        /// </summary>
        Product,
        
        /// <summary>
        /// A technical term or terminology
        /// </summary>
        TechnicalTerm,
        
        /// <summary>
        /// A job title or role
        /// </summary>
        JobTitle,
        
        /// <summary>
        /// A skill or capability
        /// </summary>
        Skill,
        
        /// <summary>
        /// A code snippet or reference
        /// </summary>
        CodeSnippet,
        
        /// <summary>
        /// A database table name
        /// </summary>
        DatabaseTable,
        
        /// <summary>
        /// A database column name
        /// </summary>
        DatabaseColumn,
        
        /// <summary>
        /// A database schema name
        /// </summary>
        DatabaseSchema,
        
        /// <summary>
        /// A numerical value
        /// </summary>
        Number,
        
        /// <summary>
        /// A unit of measurement
        /// </summary>
        Measurement,
        
        /// <summary>
        /// A custom entity type defined by a tenant
        /// </summary>
        Custom,
        
        /// <summary>
        /// A project or initiative name
        /// </summary>
        Project,
        
        /// <summary>
        /// A document or file name
        /// </summary>
        Document,
        
        /// <summary>
        /// An API or endpoint name
        /// </summary>
        Api,
        
        /// <summary>
        /// Other entities not covered by predefined types
        /// </summary>
        Other
    }
} 