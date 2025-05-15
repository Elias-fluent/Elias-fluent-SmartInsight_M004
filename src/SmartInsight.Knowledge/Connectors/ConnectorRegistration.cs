using System;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Represents a connector registration in the connector registry
    /// </summary>
    public class ConnectorRegistration
    {
        /// <summary>
        /// Unique identifier for the connector
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// Display name for the connector
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Type of data source this connector handles
        /// </summary>
        public string SourceType { get; }
        
        /// <summary>
        /// The type that implements the connector
        /// </summary>
        public Type ConnectorType { get; }
        
        /// <summary>
        /// When this connector was registered
        /// </summary>
        public DateTime RegisteredAt { get; }
        
        /// <summary>
        /// Creates a new connector registration
        /// </summary>
        /// <param name="id">Connector ID</param>
        /// <param name="name">Display name</param>
        /// <param name="sourceType">Source type</param>
        /// <param name="connectorType">The type that implements the connector</param>
        public ConnectorRegistration(string id, string name, string sourceType, Type connectorType)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be empty", nameof(id));
                
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
                
            if (string.IsNullOrWhiteSpace(sourceType))
                throw new ArgumentException("Source type cannot be empty", nameof(sourceType));
                
            if (connectorType == null)
                throw new ArgumentNullException(nameof(connectorType));
                
            if (!typeof(IDataSourceConnector).IsAssignableFrom(connectorType))
                throw new ArgumentException(
                    $"Type {connectorType.FullName} does not implement IDataSourceConnector", 
                    nameof(connectorType));
            
            Id = id;
            Name = name;
            SourceType = sourceType;
            ConnectorType = connectorType;
            RegisteredAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Creates a connector instance
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection (optional)</param>
        /// <returns>A new instance of the connector</returns>
        public IDataSourceConnector CreateInstance(IServiceProvider? serviceProvider = null)
        {
            // Try to create instance using DI if service provider is available
            if (serviceProvider != null)
            {
                try
                {
                    var instance = serviceProvider.GetService(ConnectorType);
                    if (instance != null)
                    {
                        return (IDataSourceConnector)instance;
                    }
                }
                catch
                {
                    // Fall back to activator if DI fails
                }
            }
            
            // Fall back to Activator.CreateInstance
            try
            {
                var instance = Activator.CreateInstance(ConnectorType);
                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {ConnectorType.FullName}");
                }
                
                return (IDataSourceConnector)instance;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create instance of connector {Id} ({ConnectorType.FullName})", ex);
            }
        }
        
        /// <summary>
        /// Returns a string representation of this registration
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({Id}) - {SourceType} [{ConnectorType.FullName}]";
        }
    }
} 