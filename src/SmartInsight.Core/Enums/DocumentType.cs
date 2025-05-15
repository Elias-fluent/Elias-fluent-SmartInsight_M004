namespace SmartInsight.Core.Enums;

/// <summary>
/// Types of documents that can be ingested into the system
/// </summary>
public enum DocumentType
{
    /// <summary>Plain text document</summary>
    Text = 1,
    
    /// <summary>Markdown formatted document</summary>
    Markdown = 2,
    
    /// <summary>HTML document</summary>
    Html = 3,
    
    /// <summary>PDF document</summary>
    Pdf = 4,
    
    /// <summary>Microsoft Word document</summary>
    Word = 5,
    
    /// <summary>Microsoft Excel spreadsheet</summary>
    Excel = 6,
    
    /// <summary>Microsoft PowerPoint presentation</summary>
    PowerPoint = 7,
    
    /// <summary>Source code file</summary>
    SourceCode = 8,
    
    /// <summary>JSON data</summary>
    Json = 9,
    
    /// <summary>XML data</summary>
    Xml = 10,
    
    /// <summary>Email message</summary>
    Email = 11,
    
    /// <summary>Database table or view export</summary>
    DatabaseExport = 12,
    
    /// <summary>API response data</summary>
    ApiResponse = 13,
    
    /// <summary>Image with text content (OCR processed)</summary>
    Image = 14,
    
    /// <summary>Custom document type</summary>
    Custom = 99
} 