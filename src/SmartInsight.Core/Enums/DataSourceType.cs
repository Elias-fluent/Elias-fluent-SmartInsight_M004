namespace SmartInsight.Core.Enums;

/// <summary>
/// Types of data sources supported by the system
/// </summary>
public enum DataSourceType
{
    /// <summary>SQL Server database</summary>
    SqlServer = 1,
    
    /// <summary>PostgreSQL database</summary>
    PostgreSQL = 2,
    
    /// <summary>MySQL database</summary>
    MySQL = 3,
    
    /// <summary>REST API endpoint</summary>
    RestApi = 4,
    
    /// <summary>GraphQL API endpoint</summary>
    GraphQL = 5,
    
    /// <summary>SharePoint document repository</summary>
    SharePoint = 6,
    
    /// <summary>Confluence wiki</summary>
    Confluence = 7,
    
    /// <summary>JIRA issue tracker</summary>
    Jira = 8,
    
    /// <summary>File share (network or local)</summary>
    FileShare = 9,
    
    /// <summary>Git repository</summary>
    Git = 10,
    
    /// <summary>SVN repository</summary>
    Svn = 11,
    
    /// <summary>Custom data source type</summary>
    Custom = 99
} 