using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Example connector class demonstrating the usage of metadata attributes
/// </summary>
[ConnectorMetadata(
    id: "sqlserver",
    name: "SQL Server Connector",
    sourceType: "database")]
[ConnectorCapabilities(
    SupportsIncremental = true,
    SupportsSchemaDiscovery = true,
    SupportsAdvancedFiltering = true,
    SupportsPreview = true,
    MaxConcurrentExtractions = 5,
    SupportedAuthentications = new[] { "sql", "windows", "azure-ad" })]
[ConnectorCategory("Database")]
[ConnectorCategory("Microsoft")]
[ConnectionParameter(
    name: "server",
    displayName: "Server Name",
    description: "SQL Server hostname or IP address",
    type: "string")]
[ConnectionParameter(
    name: "database",
    displayName: "Database Name",
    description: "Name of the database to connect to",
    type: "string")]
[ConnectionParameter(
    name: "authentication",
    displayName: "Authentication Type",
    description: "Method used to authenticate with the server",
    type: "enum")]
[ParameterEnumValue(
    parameterName: "authentication",
    value: "sql",
    displayText: "SQL Server Authentication")]
[ParameterEnumValue(
    parameterName: "authentication",
    value: "windows",
    displayText: "Windows Authentication")]
[ParameterEnumValue(
    parameterName: "authentication",
    value: "azure-ad",
    displayText: "Azure Active Directory")]
[ConnectionParameter(
    name: "username",
    displayName: "Username",
    description: "SQL Server username (required for SQL Server Authentication)",
    type: "string",
    IsRequired = false)]
[ConnectionParameter(
    name: "password",
    displayName: "Password",
    description: "SQL Server password (required for SQL Server Authentication)",
    type: "password",
    IsRequired = false,
    IsSecret = true)]
[ParameterValidation(
    parameterName: "server",
    validationType: "required",
    validationRule: "true")]
[ParameterValidation(
    parameterName: "database",
    validationType: "required",
    validationRule: "true")]
[ConnectorSchema(
    id: "tables",
    name: "Tables Schema",
    schemaDefinition: @"{
        ""type"": ""object"",
        ""properties"": {
            ""schema"": { ""type"": ""string"" },
            ""name"": { ""type"": ""string"" },
            ""columns"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""name"": { ""type"": ""string"" },
                        ""type"": { ""type"": ""string"" },
                        ""nullable"": { ""type"": ""boolean"" }
                    }
                }
            }
        }
    }")]
public class SqlServerConnectorExample : IDataSourceConnector
{
    // This is just a skeleton example implementation to demonstrate attribute usage
    
    public string Id => "sqlserver";
    
    public string Name => "SQL Server Connector";
    
    public string SourceType => "database";
    
    public string Description => "Connects to Microsoft SQL Server databases for data extraction";
    
    public string Version => "1.0.0";
    
    public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
    
    public event EventHandler<ConnectorStateChangedEventArgs>? StateChanged;
    
    public event EventHandler<ConnectorErrorEventArgs>? ErrorOccurred;
    
    public event EventHandler<ConnectorProgressEventArgs>? ProgressChanged;
    
    public Task<bool> InitializeAsync(IConnectorConfiguration configuration, CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        return Task.FromResult(true);
    }
    
    public Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams)
    {
        // Implementation would go here
        return Task.FromResult(ValidationResult.Success());
    }
    
    public Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        return Task.FromResult(ConnectionResult.Success("sqlserver-connection-1"));
    }
    
    public Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
    {
        // Implementation would go here
        return Task.FromResult(true);
    }
    
    public Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(IDictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        return Task.FromResult<IEnumerable<DataStructureInfo>>(new List<DataStructureInfo>());
    }
    
    public Task<ExtractionResult> ExtractDataAsync(ExtractionParameters extractionParams, CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        var data = new List<IDictionary<string, object>>();
        return Task.FromResult(ExtractionResult.Success(data, 0, 100));
    }
    
    public Task<TransformationResult> TransformDataAsync(IEnumerable<IDictionary<string, object>> data, TransformationParameters transformationParams, CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        return Task.FromResult(TransformationResult.Success(data, 50));
    }
    
    public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        return Task.FromResult(true);
    }
    
    public Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        // Implementation would go here
        return Task.CompletedTask;
    }
    
    public ConnectorCapabilities GetCapabilities()
    {
        return new ConnectorCapabilities(
            supportsIncremental: true,
            supportsSchemaDiscovery: true,
            supportsAdvancedFiltering: true,
            supportsPreview: true,
            maxConcurrentExtractions: 5,
            supportedAuthentications: new[] { "sql", "windows", "azure-ad" },
            supportedSourceTypes: new[] { "database" });
    }
    
    public IDictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            { "Description", Description },
            { "Version", Version },
            { "Author", "SmartInsight" },
            { "DocumentationUrl", "https://docs.smartinsight.com/connectors/sqlserver" }
        };
    }
} 