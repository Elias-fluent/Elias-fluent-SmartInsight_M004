namespace SmartInsight.Core.Exceptions;

/// <summary>
/// Exception thrown when credential operations fail
/// </summary>
public class CredentialException : Exception
{
    /// <summary>
    /// Associated credential key, if applicable
    /// </summary>
    public string? CredentialKey { get; }
    
    /// <summary>
    /// Type of credential operation that failed
    /// </summary>
    public CredentialOperationType OperationType { get; }
    
    /// <summary>
    /// Creates a new CredentialException
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="operationType">Type of credential operation that failed</param>
    /// <param name="credentialKey">Associated credential key, if applicable</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    public CredentialException(
        string message, 
        CredentialOperationType operationType,
        string? credentialKey = null,
        Exception? innerException = null) 
        : base(message, innerException)
    {
        CredentialKey = credentialKey;
        OperationType = operationType;
    }
    
    /// <summary>
    /// Creates a new storage exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException Storage(string credentialKey, Exception? innerException = null)
    {
        return new CredentialException(
            $"Failed to store credential: {credentialKey}",
            CredentialOperationType.Storage,
            credentialKey,
            innerException);
    }
    
    /// <summary>
    /// Creates a new retrieval exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException Retrieval(string credentialKey, Exception? innerException = null)
    {
        return new CredentialException(
            $"Failed to retrieve credential: {credentialKey}",
            CredentialOperationType.Retrieval,
            credentialKey,
            innerException);
    }
    
    /// <summary>
    /// Creates a new decryption exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException Decryption(string credentialKey, Exception? innerException = null)
    {
        return new CredentialException(
            $"Failed to decrypt credential: {credentialKey}",
            CredentialOperationType.Decryption,
            credentialKey,
            innerException);
    }
    
    /// <summary>
    /// Creates a new encryption exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException Encryption(string credentialKey, Exception? innerException = null)
    {
        return new CredentialException(
            $"Failed to encrypt credential: {credentialKey}",
            CredentialOperationType.Encryption,
            credentialKey,
            innerException);
    }
    
    /// <summary>
    /// Creates a new rotation exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException Rotation(string credentialKey, Exception? innerException = null)
    {
        return new CredentialException(
            $"Failed to rotate credential: {credentialKey}",
            CredentialOperationType.Rotation,
            credentialKey,
            innerException);
    }
    
    /// <summary>
    /// Creates a new validation exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="reason">Validation failure reason</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException Validation(string credentialKey, string reason, Exception? innerException = null)
    {
        return new CredentialException(
            $"Credential validation failed for {credentialKey}: {reason}",
            CredentialOperationType.Validation,
            credentialKey,
            innerException);
    }
    
    /// <summary>
    /// Creates a new access control exception
    /// </summary>
    /// <param name="credentialKey">Credential key</param>
    /// <param name="operation">Operation attempted</param>
    /// <param name="innerException">Inner exception, if applicable</param>
    /// <returns>Configured exception</returns>
    public static CredentialException AccessControl(string credentialKey, string operation, Exception? innerException = null)
    {
        return new CredentialException(
            $"Access denied for {operation} operation on credential: {credentialKey}",
            CredentialOperationType.AccessControl,
            credentialKey,
            innerException);
    }
}

/// <summary>
/// Types of credential operations
/// </summary>
public enum CredentialOperationType
{
    /// <summary>
    /// Storing a credential
    /// </summary>
    Storage,
    
    /// <summary>
    /// Retrieving a credential
    /// </summary>
    Retrieval,
    
    /// <summary>
    /// Decrypting a credential
    /// </summary>
    Decryption,
    
    /// <summary>
    /// Encrypting a credential
    /// </summary>
    Encryption,
    
    /// <summary>
    /// Rotating a credential
    /// </summary>
    Rotation,
    
    /// <summary>
    /// Validating a credential
    /// </summary>
    Validation,
    
    /// <summary>
    /// Access control check
    /// </summary>
    AccessControl,
    
    /// <summary>
    /// Other or unknown operation
    /// </summary>
    Other
} 