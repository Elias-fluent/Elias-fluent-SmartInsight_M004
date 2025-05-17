using System;
using System.Collections.Generic;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Tests.SQL.Common.Utilities
{
    /// <summary>
    /// Factory for creating connector configurations for testing
    /// </summary>
    public static class ConnectorConfigurationFactory
    {
        /// <summary>
        /// Creates a connector configuration for testing
        /// </summary>
        /// <param name="id">Connector ID</param>
        /// <param name="name">Connector name</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="connectionParams">Connection parameters</param>
        /// <returns>IConnectorConfiguration instance</returns>
        public static IConnectorConfiguration Create(
            string id, 
            string name, 
            Guid tenantId,
            IDictionary<string, string> connectionParams)
        {
            return new TestConnectorConfiguration(id, name, tenantId, connectionParams);
        }
        
        /// <summary>
        /// Implementation of IConnectorConfiguration for testing
        /// </summary>
        private class TestConnectorConfiguration : IConnectorConfiguration
        {
            public TestConnectorConfiguration(
                string connectorId, 
                string name, 
                Guid tenantId,
                IDictionary<string, string> connectionParams)
            {
                Id = Guid.NewGuid(); // Generate a new GUID for Id
                Name = name;
                ConnectorId = connectorId;
                TenantId = tenantId;
                ConnectionParameters = connectionParams ?? new Dictionary<string, string>();
                Credentials = new TestCredentialStore();
                Settings = new Dictionary<string, object>();
                Properties = new Dictionary<string, object>();
                CreatedAt = DateTime.UtcNow;
                ModifiedAt = DateTime.UtcNow;
                CreatedBy = "Test";
                ModifiedBy = "Test";
                IsEnabled = true;
            }
            
            public Guid Id { get; }
            
            public string Name { get; set; }
            
            public string ConnectorId { get; }
            
            public Guid TenantId { get; }
            
            public IDictionary<string, string> ConnectionParameters { get; }
            
            public ISecureCredentialStore Credentials { get; }
            
            public IDictionary<string, object> Settings { get; }
            
            public IDictionary<string, object> Properties { get; }
            
            public DateTime CreatedAt { get; }
            
            public DateTime ModifiedAt { get; }
            
            public string CreatedBy { get; }
            
            public string ModifiedBy { get; }
            
            public bool IsEnabled { get; set; }
            
            public ValidationResult Validate()
            {
                return ValidationResult.Success();
            }
            
            public void SetConnectionParameter(string key, string value)
            {
                ConnectionParameters[key] = value;
            }
            
            public void SetSetting(string key, object value)
            {
                Settings[key] = value;
            }
            
            public IDictionary<string, object> ToSerializable()
            {
                return new Dictionary<string, object>
                {
                    { "id", Id },
                    { "name", Name },
                    { "connectorId", ConnectorId },
                    { "tenantId", TenantId },
                    { "isEnabled", IsEnabled },
                    { "createdAt", CreatedAt },
                    { "modifiedAt", ModifiedAt },
                    { "connectionParameters", ConnectionParameters }
                };
            }
            
            public T GetValue<T>(string key, T defaultValue = default)
            {
                if (Settings.TryGetValue(key, out var value) && value is T typedValue)
                {
                    return typedValue;
                }
                
                return defaultValue;
            }
            
            public IDictionary<string, object> GetAll()
            {
                var result = new Dictionary<string, object>();
                
                foreach (var kvp in Settings)
                {
                    result[kvp.Key] = kvp.Value;
                }
                
                return result;
            }
            
            public bool HasValue(string key)
            {
                return Settings.ContainsKey(key);
            }
            
            public IDictionary<string, string> GetConnectionParameters()
            {
                return new Dictionary<string, string>(ConnectionParameters);
            }
        }
        
        /// <summary>
        /// Simple in-memory credential store for testing
        /// </summary>
        private class TestCredentialStore : ISecureCredentialStore
        {
            private readonly Dictionary<string, string> _credentials = new Dictionary<string, string>();
            
            public string? GetCredential(string key)
            {
                return _credentials.TryGetValue(key, out var value) ? value : null;
            }
            
            public void SetCredential(string key, string value)
            {
                _credentials[key] = value;
            }
            
            public bool RemoveCredential(string key)
            {
                return _credentials.Remove(key);
            }
            
            public bool HasCredential(string key)
            {
                return _credentials.ContainsKey(key);
            }
            
            public IEnumerable<string> GetCredentialKeys()
            {
                return _credentials.Keys;
            }
            
            public void Clear()
            {
                _credentials.Clear();
            }
        }
    }
} 