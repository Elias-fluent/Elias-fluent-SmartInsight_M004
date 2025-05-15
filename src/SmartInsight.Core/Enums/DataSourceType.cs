namespace SmartInsight.Core.Enums;

/// <summary>
/// Types of data sources supported by the system
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// PostgreSQL database
    /// </summary>
    PostgreSQL,
    
    /// <summary>
    /// Microsoft SQL Server database
    /// </summary>
    MsSqlServer,
    
    /// <summary>
    /// MySQL database
    /// </summary>
    MySQL,
    
    /// <summary>
    /// File system (local or network)
    /// </summary>
    FileSystem,
    
    /// <summary>
    /// Text files (TXT)
    /// </summary>
    TextFile,
    
    /// <summary>
    /// Markdown documents
    /// </summary>
    Markdown,
    
    /// <summary>
    /// PDF documents
    /// </summary>
    PDF,
    
    /// <summary>
    /// Microsoft Word documents
    /// </summary>
    Word,
    
    /// <summary>
    /// Atlassian Confluence
    /// </summary>
    Confluence,
    
    /// <summary>
    /// Atlassian JIRA
    /// </summary>
    JIRA,
    
    /// <summary>
    /// Git repository
    /// </summary>
    Git,
    
    /// <summary>
    /// Any REST API endpoint
    /// </summary>
    RestApi,
    
    /// <summary>
    /// Custom data source
    /// </summary>
    Custom
} 