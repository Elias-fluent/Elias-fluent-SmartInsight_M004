namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for securely storing and retrieving credentials
/// </summary>
public interface ISecureCredentialStore
{
    /// <summary>
    /// Gets a credential by key
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>Credential value, or null if not found</returns>
    string? GetCredential(string key);
    
    /// <summary>
    /// Sets a credential
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <param name="value">Credential value</param>
    void SetCredential(string key, string value);
    
    /// <summary>
    /// Removes a credential
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>True if the credential was removed, false if it didn't exist</returns>
    bool RemoveCredential(string key);
    
    /// <summary>
    /// Checks if a credential exists
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>True if the credential exists, false otherwise</returns>
    bool HasCredential(string key);
    
    /// <summary>
    /// Gets all credential keys (but not values)
    /// </summary>
    /// <returns>Collection of credential keys</returns>
    IEnumerable<string> GetCredentialKeys();
    
    /// <summary>
    /// Clears all credentials
    /// </summary>
    void Clear();
} 