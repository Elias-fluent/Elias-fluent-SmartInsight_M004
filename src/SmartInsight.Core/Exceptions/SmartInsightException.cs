namespace SmartInsight.Core.Exceptions;

/// <summary>
/// Base exception class for all SmartInsight application exceptions
/// </summary>
public class SmartInsightException : Exception
{
    /// <summary>
    /// Error code for the exception
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Creates a new SmartInsight exception
    /// </summary>
    public SmartInsightException() : base("An error occurred in the SmartInsight application.")
    {
        ErrorCode = "SMARTINSIGHT_ERROR";
    }
    
    /// <summary>
    /// Creates a new SmartInsight exception with a message
    /// </summary>
    /// <param name="message">Exception message</param>
    public SmartInsightException(string message) : base(message)
    {
        ErrorCode = "SMARTINSIGHT_ERROR";
    }
    
    /// <summary>
    /// Creates a new SmartInsight exception with a message and error code
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="errorCode">Error code</param>
    public SmartInsightException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    /// <summary>
    /// Creates a new SmartInsight exception with a message and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public SmartInsightException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "SMARTINSIGHT_ERROR";
    }
    
    /// <summary>
    /// Creates a new SmartInsight exception with a message, error code, and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="errorCode">Error code</param>
    /// <param name="innerException">Inner exception</param>
    public SmartInsightException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
} 