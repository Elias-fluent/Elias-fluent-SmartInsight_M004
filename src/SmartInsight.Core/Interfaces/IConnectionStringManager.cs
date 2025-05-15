using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Provides methods for managing database connection strings
/// </summary>
public interface IConnectionStringManager
{
    /// <summary>
    /// Gets a connection string for the specified name, decrypting if necessary
    /// </summary>
    /// <param name="name">Name of the connection string</param>
    /// <param name="forTenant">Whether to get a tenant-specific connection if available</param>
    /// <param name="decrypt">Whether to decrypt encrypted connection strings</param>
    /// <returns>The connection string</returns>
    string GetConnectionString(string name, bool forTenant = true, bool decrypt = true);
    
    /// <summary>
    /// Builds a full connection string with additional parameters like Application Name
    /// </summary>
    /// <param name="baseConnectionString">Base connection string</param>
    /// <param name="additionalProperties">Additional connection properties</param>
    /// <returns>Complete connection string</returns>
    string BuildConnectionString(string baseConnectionString, IDictionary<string, string>? additionalProperties = null);
    
    /// <summary>
    /// Gets a connection string with tenant-specific application name
    /// </summary>
    /// <param name="name">Name of the connection string</param>
    /// <param name="applicationNamePrefix">Prefix for the application name</param>
    /// <returns>Connection string with tenant-specific application name</returns>
    string GetConnectionStringWithApplicationName(string name, string applicationNamePrefix);
    
    /// <summary>
    /// Encrypts a connection string
    /// </summary>
    /// <param name="connectionString">Connection string to encrypt</param>
    /// <returns>Encrypted connection string</returns>
    string EncryptConnectionString(string connectionString);
} 