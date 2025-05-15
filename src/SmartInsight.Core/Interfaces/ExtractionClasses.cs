namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Parameters for data extraction from a data source
/// </summary>
public class ExtractionParameters
{
    /// <summary>
    /// Target data structures to extract (e.g., table names, file paths)
    /// </summary>
    public IReadOnlyList<string> TargetStructures { get; }
    
    /// <summary>
    /// Data structure extraction query/filter criteria
    /// </summary>
    public IDictionary<string, object>? FilterCriteria { get; }
    
    /// <summary>
    /// Fields to include in the extraction
    /// </summary>
    public IReadOnlyList<string>? IncludeFields { get; }
    
    /// <summary>
    /// Maximum number of records to extract (0 = no limit)
    /// </summary>
    public int MaxRecords { get; }
    
    /// <summary>
    /// Batch size for extraction (0 = default batch size)
    /// </summary>
    public int BatchSize { get; }
    
    /// <summary>
    /// Whether to extract only records modified since the last extraction
    /// </summary>
    public bool IncrementalExtraction { get; }
    
    /// <summary>
    /// Date from which to extract changes (if IncrementalExtraction is true)
    /// </summary>
    public DateTime? ChangesFromDate { get; }
    
    /// <summary>
    /// Field used to track changes for incremental extraction
    /// </summary>
    public string? ChangeTrackingField { get; }
    
    /// <summary>
    /// Additional extraction options
    /// </summary>
    public IDictionary<string, object>? Options { get; }
    
    /// <summary>
    /// Creates new extraction parameters
    /// </summary>
    /// <param name="targetStructures">Target data structures to extract</param>
    /// <param name="filterCriteria">Data structure extraction query/filter criteria</param>
    /// <param name="includeFields">Fields to include in the extraction</param>
    /// <param name="maxRecords">Maximum number of records to extract (0 = no limit)</param>
    /// <param name="batchSize">Batch size for extraction (0 = default batch size)</param>
    /// <param name="incrementalExtraction">Whether to extract only records modified since the last extraction</param>
    /// <param name="changesFromDate">Date from which to extract changes (if IncrementalExtraction is true)</param>
    /// <param name="changeTrackingField">Field used to track changes for incremental extraction</param>
    /// <param name="options">Additional extraction options</param>
    public ExtractionParameters(
        IEnumerable<string> targetStructures,
        IDictionary<string, object>? filterCriteria = null,
        IEnumerable<string>? includeFields = null,
        int maxRecords = 0,
        int batchSize = 1000,
        bool incrementalExtraction = false,
        DateTime? changesFromDate = null,
        string? changeTrackingField = null,
        IDictionary<string, object>? options = null)
    {
        TargetStructures = targetStructures.ToList().AsReadOnly();
        FilterCriteria = filterCriteria;
        IncludeFields = includeFields?.ToList().AsReadOnly();
        MaxRecords = maxRecords;
        BatchSize = batchSize;
        IncrementalExtraction = incrementalExtraction;
        ChangesFromDate = changesFromDate;
        ChangeTrackingField = changeTrackingField;
        Options = options;
    }
}

/// <summary>
/// Result of a data extraction operation
/// </summary>
public class ExtractionResult
{
    /// <summary>
    /// Whether the extraction was successful
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Extracted data as a collection of dictionaries
    /// </summary>
    public IReadOnlyList<IDictionary<string, object>>? Data { get; }
    
    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Additional error details (if unsuccessful)
    /// </summary>
    public string? ErrorDetails { get; }
    
    /// <summary>
    /// Number of records extracted
    /// </summary>
    public int RecordCount { get; }
    
    /// <summary>
    /// Timestamp of the extraction
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Time taken for the extraction (in milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; }
    
    /// <summary>
    /// Data structure metadata for the extracted data
    /// </summary>
    public DataStructureInfo? StructureInfo { get; }
    
    /// <summary>
    /// Whether there are more records available for extraction
    /// </summary>
    public bool HasMoreRecords { get; }
    
    /// <summary>
    /// Continuation token for retrieving the next batch of records
    /// </summary>
    public string? ContinuationToken { get; }
    
    /// <summary>
    /// Creates a new successful extraction result
    /// </summary>
    /// <param name="data">Extracted data</param>
    /// <param name="recordCount">Number of records extracted</param>
    /// <param name="executionTimeMs">Time taken for the extraction (in milliseconds)</param>
    /// <param name="structureInfo">Data structure metadata for the extracted data</param>
    /// <param name="hasMoreRecords">Whether there are more records available for extraction</param>
    /// <param name="continuationToken">Continuation token for retrieving the next batch of records</param>
    /// <returns>A successful extraction result</returns>
    public static ExtractionResult Success(
        IEnumerable<IDictionary<string, object>> data,
        int recordCount,
        long executionTimeMs,
        DataStructureInfo? structureInfo = null,
        bool hasMoreRecords = false,
        string? continuationToken = null)
    {
        return new ExtractionResult(
            true, 
            data.ToList(), 
            null, 
            null, 
            recordCount, 
            executionTimeMs, 
            structureInfo, 
            hasMoreRecords, 
            continuationToken);
    }
    
    /// <summary>
    /// Creates a new failed extraction result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorDetails">Additional error details</param>
    /// <param name="executionTimeMs">Time taken for the extraction attempt (in milliseconds)</param>
    /// <returns>A failed extraction result</returns>
    public static ExtractionResult Failure(
        string errorMessage, 
        string? errorDetails = null, 
        long executionTimeMs = 0)
    {
        return new ExtractionResult(
            false, 
            null, 
            errorMessage, 
            errorDetails, 
            0, 
            executionTimeMs, 
            null, 
            false, 
            null);
    }
    
    /// <summary>
    /// Creates a new extraction result
    /// </summary>
    /// <param name="isSuccess">Whether the extraction was successful</param>
    /// <param name="data">Extracted data</param>
    /// <param name="errorMessage">Error message (if unsuccessful)</param>
    /// <param name="errorDetails">Additional error details (if unsuccessful)</param>
    /// <param name="recordCount">Number of records extracted</param>
    /// <param name="executionTimeMs">Time taken for the extraction (in milliseconds)</param>
    /// <param name="structureInfo">Data structure metadata for the extracted data</param>
    /// <param name="hasMoreRecords">Whether there are more records available for extraction</param>
    /// <param name="continuationToken">Continuation token for retrieving the next batch of records</param>
    private ExtractionResult(
        bool isSuccess,
        IEnumerable<IDictionary<string, object>>? data,
        string? errorMessage,
        string? errorDetails,
        int recordCount,
        long executionTimeMs,
        DataStructureInfo? structureInfo,
        bool hasMoreRecords,
        string? continuationToken)
    {
        IsSuccess = isSuccess;
        Data = data?.ToList().AsReadOnly();
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        RecordCount = recordCount;
        Timestamp = DateTime.UtcNow;
        ExecutionTimeMs = executionTimeMs;
        StructureInfo = structureInfo;
        HasMoreRecords = hasMoreRecords;
        ContinuationToken = continuationToken;
    }
} 