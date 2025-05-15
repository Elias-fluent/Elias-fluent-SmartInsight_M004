using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Simple in-memory implementation of ISecureCredentialStore
/// </summary>
/// <remarks>
/// This implementation is intended for development and testing purposes.
/// In production, use a more secure implementation with proper encryption.
/// </remarks>
public class MemoryCredentialStore : ISecureCredentialStore
{
    private readonly Dictionary<string, string> _credentials = new();
    
    /// <summary>
    /// Gets all credential keys (but not values)
    /// </summary>
    /// <returns>Collection of credential keys</returns>
    public IEnumerable<string> GetCredentialKeys() => _credentials.Keys;
    
    /// <summary>
    /// Gets a credential by key
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>Credential value, or null if not found</returns>
    public string? GetCredential(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        
        return _credentials.TryGetValue(key, out var value) ? value : null;
    }
    
    /// <summary>
    /// Checks if a credential exists
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>True if the credential exists, false otherwise</returns>
    public bool HasCredential(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        
        return _credentials.ContainsKey(key);
    }
    
    /// <summary>
    /// Removes a credential
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>True if the credential was removed, false if it didn't exist</returns>
    public bool RemoveCredential(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        
        return _credentials.Remove(key);
    }
    
    /// <summary>
    /// Sets a credential
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <param name="value">Credential value</param>
    public void SetCredential(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        
        _credentials[key] = value;
    }
    
    /// <summary>
    /// Clears all credentials
    /// </summary>
    public void Clear() => _credentials.Clear();
} 