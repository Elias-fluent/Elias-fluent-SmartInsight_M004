# Data Source Connector Implementation Guide

This document provides guidance for implementing data source connectors in the SmartInsight system.

## Overview

Data source connectors provide a standardized way to connect to, extract data from, and interact with various external data systems. The connector interface is designed to be flexible enough to support a wide range of data sources while providing a consistent API for the rest of the system.

## Core Interfaces

The following interfaces form the foundation of the connector system:

- `IDataSourceConnector`: The main connector interface that all connectors must implement
- `IConnectorConfiguration`: Provides configuration values and credentials to connectors
- `IConnectorRegistry`: Manages connector registration and discovery
- `IConnectorFactory`: Creates connector instances

## Implementing a Connector

### Step 1: Create a New Connector Class

Create a new class that implements the `IDataSourceConnector` interface. Use the `SampleConnector` as a reference implementation.

```csharp
[ConnectorMetadata(
    id: "my-connector", 
    name: "My Connector", 
    sourceType: "Database",
    Description = "Connects to My Database System.",
    Version = "1.0.0",
    Author = "Your Name")]
public class MyConnector : IDataSourceConnector
{
    // Implementation here
}
```

### Step 2: Implement Required Properties

```csharp
public string Id => "my-connector";
public string Name => "My Connector";
public string SourceType => "Database";
public string Description => "Connects to My Database System.";
public string Version => "1.0.0";
public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
```

### Step 3: Implement Event Handlers

```csharp
public event EventHandler<ConnectorStateChangedEventArgs>? StateChanged;
public event EventHandler<ConnectorErrorEventArgs>? ErrorOccurred;
public event EventHandler<ConnectorProgressEventArgs>? ProgressChanged;

private void OnStateChanged(ConnectionState oldState, ConnectionState newState)
{
    StateChanged?.Invoke(this, new ConnectorStateChangedEventArgs(Id, oldState, newState));
}

private void OnErrorOccurred(string operation, string message, Exception? exception = null)
{
    ErrorOccurred?.Invoke(this, new ConnectorErrorEventArgs(Id, operation, message, null, exception));
}

private void OnProgressChanged(string operationId, int current, int total, string message)
{
    ProgressChanged?.Invoke(this, new ConnectorProgressEventArgs(operationId, current, total, message));
}
```

### Step 4: Implement Core Methods

#### Initialization and Connection

```csharp
public Task<bool> InitializeAsync(IConnectorConfiguration configuration, CancellationToken cancellationToken = default)
{
    // Store configuration, validate it, set up any resources needed
    return Task.FromResult(true);
}

public Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams)
{
    // Validate connection parameters
    // Return ValidationResult.Success() or ValidationResult.Failure(errors)
}

public Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
{
    // Connect to the data source
    // Return ConnectionResult.Success(...) or ConnectionResult.Failure(...)
}

public Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
{
    // Test the connection and return true if successful
}

public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
{
    // Disconnect from the data source
    return Task.FromResult(true);
}

public Task DisposeAsync(CancellationToken cancellationToken = default)
{
    // Clean up resources
    return Task.CompletedTask;
}
```

#### Data Operations

```csharp
public Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(IDictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
{
    // Discover available data structures (tables, files, etc.) and their schemas
}

public Task<ExtractionResult> ExtractDataAsync(ExtractionParameters extractionParams, CancellationToken cancellationToken = default)
{
    // Extract data from the source
    // Return ExtractionResult.Success(...) or ExtractionResult.Failure(...)
}

public Task<TransformationResult> TransformDataAsync(IEnumerable<IDictionary<string, object>> data, TransformationParameters transformationParams, CancellationToken cancellationToken = default)
{
    // Transform the data
    // Return TransformationResult.Success(...) or TransformationResult.Failure(...)
}
```

#### Metadata

```csharp
public ConnectorCapabilities GetCapabilities()
{
    // Return the connector's capabilities
    return new ConnectorCapabilities(
        supportsIncremental: true,
        supportsSchemaDiscovery: true,
        supportsAdvancedFiltering: false,
        supportsPreview: true,
        maxConcurrentExtractions: 1,
        supportedAuthentications: new[] { "basic", "oauth" },
        supportedSourceTypes: new[] { "database" });
}

public IDictionary<string, object> GetMetadata()
{
    // Return any additional metadata about the connector
    return new Dictionary<string, object>
    {
        ["Description"] = Description,
        ["Version"] = Version,
        ["Author"] = "Your Name",
        ["Documentation"] = "https://docs.example.com/connectors/my-connector"
    };
}
```

## Connector Attributes

Use the following attributes to provide metadata about your connector:

```csharp
[ConnectorMetadata(
    id: "my-connector", 
    name: "My Connector", 
    sourceType: "Database",
    Description = "Connects to My Database System.",
    Version = "1.0.0",
    Author = "Your Name",
    Capabilities = new[] { "read", "extract" },
    SupportUrl = "https://docs.example.com/connectors/my-connector",
    ConnectionSchema = @"{""your"": ""json schema here""}"
)]
[ConnectorCategory("Database")]
[ConnectorCategory("Enterprise")]
[ConnectionParameter(
    name: "server",
    displayName: "Server",
    description: "Database server address",
    type: "string",
    IsRequired = true)]
```

## Best Practices

1. **Error Handling**: Always handle exceptions and report them via the `ErrorOccurred` event
2. **Cancellation Support**: Honor cancellation tokens in all async methods
3. **Progress Reporting**: Use the `ProgressChanged` event to report progress during long-running operations
4. **State Management**: Always update and report connection state changes
5. **Validation**: Thoroughly validate all inputs, especially connection parameters
6. **Resource Management**: Clean up resources properly in `DisconnectAsync` and `DisposeAsync`
7. **Configuration Security**: Never log sensitive information like passwords or API keys

## Testing Your Connector

Use the `ConnectorFactoryExample` as a template for testing your connector. Test the following:

1. Connector registration and creation
2. Parameter validation
3. Connection and disconnection
4. Data structure discovery
5. Data extraction
6. Error handling
7. Event firing

## Registering Your Connector

Connectors are automatically registered using the `IConnectorRegistry` when you add them to the dependency injection container:

```csharp
// Register a specific connector
services.AddConnector<MyConnector>();

// Or register all connectors from an assembly
services.AddConnectorsFromAssembly(typeof(MyConnector).Assembly);
```

## Sample Usage

```csharp
// Get connector factory
var factory = serviceProvider.GetRequiredService<IConnectorFactory>();

// Create connector instance
var connector = factory.CreateConnector("my-connector");

// Initialize with configuration
await connector.InitializeAsync(configuration);

// Connect
var connectionResult = await connector.ConnectAsync(connectionParams);

// Extract data
var extractionResult = await connector.ExtractDataAsync(extractionParams);

// Disconnect
await connector.DisconnectAsync();
```

See the `ConnectorFactoryExample` for a complete working example. 