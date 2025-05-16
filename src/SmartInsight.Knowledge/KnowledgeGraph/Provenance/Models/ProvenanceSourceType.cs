using System;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models
{
    /// <summary>
    /// Defines the types of sources that knowledge graph elements can be derived from
    /// </summary>
    public enum ProvenanceSourceType
    {
        /// <summary>
        /// Structured database (SQL, NoSQL)
        /// </summary>
        Database = 0,
        
        /// <summary>
        /// Document file (PDF, DOCX, TXT, MD, etc.)
        /// </summary>
        Document = 1,
        
        /// <summary>
        /// Web resource (URL, webpage)
        /// </summary>
        Web = 2,
        
        /// <summary>
        /// API or web service
        /// </summary>
        Api = 3,
        
        /// <summary>
        /// Code repository (Git, SVN)
        /// </summary>
        CodeRepository = 4,
        
        /// <summary>
        /// Knowledge management system (Confluence, SharePoint)
        /// </summary>
        KnowledgeSystem = 5,
        
        /// <summary>
        /// Ticketing or issue tracking system (JIRA, GitHub Issues)
        /// </summary>
        TicketingSystem = 6,
        
        /// <summary>
        /// File system (local or network drive)
        /// </summary>
        FileSystem = 7,
        
        /// <summary>
        /// User input or manual entry
        /// </summary>
        UserInput = 8,
        
        /// <summary>
        /// Derived from other knowledge (inference, reasoning)
        /// </summary>
        Inference = 9,
        
        /// <summary>
        /// System-generated (default values, system rules)
        /// </summary>
        System = 10,
        
        /// <summary>
        /// Custom or domain-specific source
        /// </summary>
        Custom = 11
    }
} 