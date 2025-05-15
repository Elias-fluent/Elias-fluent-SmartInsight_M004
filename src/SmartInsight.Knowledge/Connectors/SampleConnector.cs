using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Sample connector implementation for testing and demonstration
    /// </summary>
    [ConnectorMetadata(
        id: "sample-connector", 
        name: "Sample Connector", 
        sourceType: "Sample",
        Description = "A sample connector for demonstration purposes.",
        Version = "1.0.0",
        Author = "SmartInsight Team",
        Capabilities = new[] { "read", "extract", "transform" },
        DocumentationUrl = "https://docs.example.com/connectors/sample",
        ConnectionSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""server"": {
      ""type"": ""string"",
      ""description"": ""Server address""
    },
    ""port"": {
      ""type"": ""integer"",
      ""description"": ""Server port"",
      ""default"": 1234
    },
    ""apiKey"": {
      ""type"": ""string"",
      ""description"": ""API key for authentication""
    },
    ""useTls"": {
      ""type"": ""boolean"",
      ""description"": ""Use TLS/SSL for connections"",
      ""default"": true
    },
    ""maxRecords"": {
      ""type"": ""integer"",
      ""description"": ""Maximum records to fetch per request"",
      ""default"": 1000,
      ""minimum"": 1,
      ""maximum"": 10000
    }
  },
  ""required"": [""server"", ""apiKey""]
}")
]
[ConnectorCategory("Sample")]
[ConnectorCategory("Demo")]
[ConnectionParameter(
    name: "server",
    displayName: "Server Address",
    description: "The address of the sample server",
    type: "string",
    IsRequired = true)]
[ConnectionParameter(
    name: "port",
    displayName: "Port",
    description: "The port to connect on",
    type: "integer",
    IsRequired = false,
    DefaultValue = "1234")]
[ConnectionParameter(
    name: "apiKey",
    displayName: "API Key",
    description: "Authentication key for the server",
    type: "password",
    IsRequired = true,
    IsSecret = true)]
[ConnectionParameter(
    name: "useTls",
    displayName: "Use TLS/SSL",
    description: "Whether to use TLS/SSL for connections",
    type: "boolean",
    IsRequired = false,
    DefaultValue = "true")]
[ParameterValidation(
    parameterName: "server",
    validationType: "regex",
    validationRule: @"^[a-zA-Z0-9_.-]+$|^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")]
[ParameterValidation(
    parameterName: "port",
    validationType: "range",
    validationRule: "1-65535")]
public class SampleConnector : IDataSourceConnector, IDisposable
{
    private readonly ILogger<SampleConnector>? _logger;
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
    private IConnectorConfiguration? _configuration;
    private bool _disposed;
    private string? _connectionId;
    
    // Mock client to simulate a real connection
    private SampleMockClient? _client;
    
    /// <summary>
    /// Creates a new instance of the sample connector
    /// </summary>
    public SampleConnector() 
    {
        // Default constructor for use when logger is not available
    }
    
    /// <summary>
    /// Creates a new instance of the sample connector with logging
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public SampleConnector(ILogger<SampleConnector> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public string Id => "sample-connector";
    
    /// <inheritdoc/>
    public string Name => "Sample Connector";
    
    /// <inheritdoc/>
    public string SourceType => "Sample";
    
    /// <inheritdoc/>
    public string Description => "A sample connector for demonstration purposes.";
    
    /// <inheritdoc/>
    public string Version => "1.0.0";
    
    /// <inheritdoc/>
    public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
    
    /// <inheritdoc/>
    public event EventHandler<ConnectorStateChangedEventArgs>? StateChanged;
    
    /// <inheritdoc/>
    public event EventHandler<ConnectorErrorEventArgs>? ErrorOccurred;
    
    /// <inheritdoc/>
    public event EventHandler<ConnectorProgressEventArgs>? ProgressChanged;
    
    /// <inheritdoc/>
    public async Task<bool> InitializeAsync(IConnectorConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Initializing sample connector");
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
            
        // Store configuration for later use
        _configuration = configuration;
        
        try
        {
            // Validate configuration
            var validationResult = await ValidateConfigurationAsync(configuration);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join(", ", validationResult.Errors);
                _logger?.LogError("Configuration validation failed: {Errors}", errorMessages);
                OnErrorOccurred("Initialize", $"Configuration validation failed: {errorMessages}");
                return false;
            }
            
            _logger?.LogInformation("Sample connector initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing sample connector");
            OnErrorOccurred("Initialize", $"Error initializing connector: {ex.Message}", ex);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams)
    {
        _logger?.LogDebug("Validating connection parameters");
        
        var errors = new List<ValidationError>();
        var warnings = new List<string>();
        
        try
        {
            // Validate required parameters
            if (!connectionParams.TryGetValue("server", out var server) || string.IsNullOrWhiteSpace(server))
            {
                errors.Add(new ValidationError("server", "Server address is required"));
            }
            else if (!IsValidServerName(server))
            {
                errors.Add(new ValidationError("server", "Server must be a valid hostname or IP address"));
            }
            
            if (!connectionParams.TryGetValue("apiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                errors.Add(new ValidationError("apiKey", "API key is required"));
            }
            
            // Validate optional parameters
            if (connectionParams.TryGetValue("port", out var portStr) && !string.IsNullOrWhiteSpace(portStr))
            {
                if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
                {
                    errors.Add(new ValidationError("port", "Port must be between 1 and 65535"));
                }
            }
            
            // Add warnings for optional parameters
            if (!connectionParams.ContainsKey("useTls") || string.IsNullOrWhiteSpace(connectionParams["useTls"]))
            {
                warnings.Add("useTls parameter not specified, using default value (true)");
            }
            
            if (!connectionParams.ContainsKey("maxRecords") || string.IsNullOrWhiteSpace(connectionParams["maxRecords"]))
            {
                warnings.Add("maxRecords parameter not specified, using default value (1000)");
            }
            
            _logger?.LogDebug("Validation completed with {ErrorCount} errors and {WarningCount} warnings", 
                errors.Count, warnings.Count);
                
            if (errors.Count > 0)
            {
                return Task.FromResult(ValidationResult.Failure(errors, warnings));
            }
            
            return Task.FromResult(ValidationResult.Success(warnings));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating connection parameters");
            errors.Add(new ValidationError("", $"Validation error: {ex.Message}"));
            return Task.FromResult(ValidationResult.Failure(errors));
        }
    }
    
    /// <inheritdoc/>
    public async Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
    {
        try
        {
            await _connectionLock.WaitAsync(cancellationToken);
            
            try
            {
                _logger?.LogInformation("Connecting to sample server: {Server}", 
                    connectionParams.ContainsKey("server") ? connectionParams["server"] : "unknown");
                
                // Update state
                ConnectionState = ConnectionState.Connecting;
                OnStateChanged(ConnectionState.Disconnected, ConnectionState);
                
                // Validate parameters
                var validationResult = await ValidateConnectionAsync(connectionParams);
                if (!validationResult.IsValid)
                {
                    _logger?.LogWarning("Connection validation failed: {ErrorCount} errors", validationResult.Errors.Count);
                    ConnectionState = ConnectionState.Disconnected;
                    OnStateChanged(ConnectionState.Connecting, ConnectionState);
                    return ConnectionResult.Failure("Connection validation failed", validationResult.Errors);
                }
                
                // Get connection parameters
                string server = connectionParams["server"];
                int port = connectionParams.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var p) ? p : 1234;
                string apiKey = connectionParams["apiKey"];
                bool useTls = connectionParams.TryGetValue("useTls", out var tlsStr) && bool.TryParse(tlsStr, out var t) ? t : true;
                
                // Simulate connection
                _client = new SampleMockClient(server, port, apiKey, useTls);
                
                // Simulate network delay
                await Task.Delay(500, cancellationToken);
                
                // 90% success rate for demo purposes
                bool success = new Random().NextDouble() > 0.1;
                if (!success)
                {
                    _logger?.LogError("Failed to connect to sample server");
                    ConnectionState = ConnectionState.Error;
                    OnStateChanged(ConnectionState.Connecting, ConnectionState);
                    return ConnectionResult.Failure("Failed to connect to server");
                }
                
                // Generate a unique connection ID
                _connectionId = $"sample-{Guid.NewGuid():N}";
                
                // Update state
                ConnectionState = ConnectionState.Connected;
                OnStateChanged(ConnectionState.Connecting, ConnectionState);
                
                _logger?.LogInformation("Successfully connected to sample server: {ConnectionId}", _connectionId);
                
                // Return connection result
                return ConnectionResult.Success(
                    connectionId: _connectionId,
                    serverVersion: "1.0.0",
                    connectionInfo: new Dictionary<string, object>
                    {
                        ["connected_at"] = DateTime.UtcNow,
                        ["server"] = server,
                        ["port"] = port,
                        ["useTls"] = useTls
                    });
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Connection operation was cancelled");
            ConnectionState = ConnectionState.Disconnected;
            OnStateChanged(ConnectionState.Connecting, ConnectionState);
            return ConnectionResult.Failure("Connection was cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error connecting to sample server");
            ConnectionState = ConnectionState.Error;
            OnStateChanged(ConnectionState.Connecting, ConnectionState);
            OnErrorOccurred("Connect", ex.Message, ex);
            
            return ConnectionResult.Failure(ex.Message);
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
    {
        _logger?.LogDebug("Testing connection to sample server");
        
        try
        {
            var result = await ConnectAsync(connectionParams);
            
            if (result.IsSuccess)
            {
                _logger?.LogInformation("Connection test successful");
                await DisconnectAsync();
                return true;
            }
            
            _logger?.LogWarning("Connection test failed: {ErrorMessage}", result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error testing connection");
            return false;
        }
    }
    
    public Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(
        IDictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var fieldsTable1 = new List<FieldInfo>
        {
            new FieldInfo("id", "integer", false, "Primary key", true),
            new FieldInfo("name", "string", true, "Sample name"),
            new FieldInfo("value", "double", true, "Sample value"),
            new FieldInfo("date", "datetime", true, "Sample date")
        };
        
        var fieldsTable2 = new List<FieldInfo>
        {
            new FieldInfo("id", "integer", false, "Primary key", true),
            new FieldInfo("category", "string", false, "Category name"),
            new FieldInfo("is_active", "boolean", true, "Active status")
        };
        
        var structures = new List<DataStructureInfo>
        {
            new DataStructureInfo(
                name: "sample_table_1",
                type: "table",
                fields: fieldsTable1,
                description: "Sample data table 1"),
                
            new DataStructureInfo(
                name: "sample_table_2",
                type: "table",
                fields: fieldsTable2,
                description: "Sample data table 2")
        };
        
        return Task.FromResult<IEnumerable<DataStructureInfo>>(structures);
    }
    
    public Task<ExtractionResult> ExtractDataAsync(
        ExtractionParameters extractionParams,
        CancellationToken cancellationToken = default)
    {
        // Simulate data extraction
        var data = new List<IDictionary<string, object>>();
        for (int i = 0; i < 10; i++)
        {
            // Report progress
            OnProgressChanged("extraction", i * 10, 100, $"Extracting record {i+1} of 10");
            
            // Create sample data
            var record = new Dictionary<string, object>
            {
                ["id"] = i,
                ["name"] = $"Sample {i}",
                ["value"] = i * 10.5,
                ["date"] = DateTime.UtcNow.AddDays(-i)
            };
            
            data.Add(record);
            
            // Simulate some processing time
            Thread.Sleep(10);
            
            // Check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(ExtractionResult.Failure("Extraction was cancelled"));
            }
        }
        
        // Final progress
        OnProgressChanged("extraction", 100, 100, "Extraction complete");
        
        // Return successful result with execution time of 200ms
        return Task.FromResult(ExtractionResult.Success(data, 10, 200));
    }
    
    public Task<TransformationResult> TransformDataAsync(
        IEnumerable<IDictionary<string, object>> data,
        TransformationParameters transformationParams,
        CancellationToken cancellationToken = default)
    {
        // Create a new list to hold transformed data
        var transformedData = new List<IDictionary<string, object>>();
        
        // Apply simple transformations (uppercasing string values)
        int i = 0, total = 0;
        
        // Count total items first
        foreach (var _ in data) { total++; }
        
        foreach (var record in data)
        {
            // Report progress
            OnProgressChanged("transform", i, total, $"Transforming record {i+1} of {total}");
            
            var transformedRecord = new Dictionary<string, object>();
            
            // Apply transformations to each field
            foreach (var field in record)
            {
                if (field.Value is string stringValue)
                {
                    transformedRecord[field.Key] = stringValue.ToUpperInvariant();
                }
                else
                {
                    transformedRecord[field.Key] = field.Value;
                }
            }
            
            transformedData.Add(transformedRecord);
            i++;
            
            // Simulate some processing time
            Thread.Sleep(5);
            
            // Check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(TransformationResult.Failure("Transformation was cancelled"));
            }
        }
        
        // Final progress
        OnProgressChanged("transform", total, total, "Transformation complete");
        
        // Return successful result
        return Task.FromResult(TransformationResult.Success(transformedData, 100));
    }
    
    public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (ConnectionState == ConnectionState.Connected || ConnectionState == ConnectionState.Error)
        {
            ConnectionState = ConnectionState.Disconnecting;
            OnStateChanged(ConnectionState.Connected, ConnectionState);
            
            // Simulate disconnection
            Thread.Sleep(50);
            
            ConnectionState = ConnectionState.Disconnected;
            OnStateChanged(ConnectionState.Disconnecting, ConnectionState);
            
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
    
    public Task DisposeAsync(CancellationToken cancellationToken = default)
    {
        // Cleanup any resources
        _configuration = null;
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
            supportedAuthentications: new[] { "apikey" },
            supportedSourceTypes: new[] { "sample" });
    }
    
    public IDictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["Description"] = Description,
            ["Version"] = Version,
            ["Author"] = "SmartInsight Team",
            ["Documentation"] = "https://docs.example.com/connectors/sample",
            ["SupportedFeatures"] = new[] { "read", "extract", "transform" }
        };
    }
    
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
    
    // Add private helper methods

    /// <summary>
    /// Validates the connector configuration
    /// </summary>
    private async Task<ValidationResult> ValidateConfigurationAsync(IConnectorConfiguration configuration)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<string>();
        
        if (string.IsNullOrWhiteSpace(configuration.ConnectorId))
        {
            errors.Add(new ValidationError("ConnectorId", "Connector ID cannot be empty"));
        }
        else if (configuration.ConnectorId != Id)
        {
            errors.Add(new ValidationError("ConnectorId", $"Configuration is for connector '{configuration.ConnectorId}', expected '{Id}'"));
        }
        
        // Validate connection parameters from the configuration
        var connectionParams = configuration.GetConnectionParameters();
        var result = await ValidateConnectionAsync(connectionParams);
        
        if (!result.IsValid)
        {
            errors.AddRange(result.Errors);
        }
        
        if (result.Warnings.Count > 0)
        {
            warnings.AddRange(result.Warnings);
        }
        
        // Validate additional settings
        if (configuration.HasValue("maxRecords"))
        {
            int maxRecords = configuration.GetValue<int>("maxRecords", 1000);
            if (maxRecords < 1 || maxRecords > 10000)
            {
                errors.Add(new ValidationError("maxRecords", "Max records must be between 1 and 10000"));
            }
        }
        
        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors, warnings);
        }
        
        return ValidationResult.Success(warnings);
    }
    
    /// <summary>
    /// Validates a server name or IP address
    /// </summary>
    private bool IsValidServerName(string server)
    {
        // Simple validation for demonstration purposes
        return !string.IsNullOrWhiteSpace(server) && 
               (System.Text.RegularExpressions.Regex.IsMatch(server, @"^[a-zA-Z0-9_.-]+$") || 
                System.Text.RegularExpressions.Regex.IsMatch(server, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"));
    }
    
    /// <summary>
    /// Disposes of managed and unmanaged resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Disposes of managed and unmanaged resources
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        
        if (disposing)
        {
            // Dispose managed resources
            _connectionLock.Dispose();
            _client?.Dispose();
        }
        
        _disposed = true;
    }
    
    /// <summary>
    /// Mock client simulating connection to a sample server
    /// </summary>
    private class SampleMockClient : IDisposable
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _apiKey;
        private readonly bool _useTls;
        private bool _isConnected;
        private bool _disposed;
        
        public SampleMockClient(string server, int port, string apiKey, bool useTls)
        {
            _server = server;
            _port = port;
            _apiKey = apiKey;
            _useTls = useTls;
        }
        
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SampleMockClient));
                
            // Simulate connection
            await Task.Delay(100, cancellationToken);
            
            // 90% success rate for demo purposes
            bool success = new Random().NextDouble() > 0.1;
            _isConnected = success;
            
            return success;
        }
        
        public async Task<bool> DisconnectAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SampleMockClient));
                
            // Simulate disconnection
            await Task.Delay(50, cancellationToken);
            _isConnected = false;
            
            return true;
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            // Cleanup any resources
            _isConnected = false;
            _disposed = true;
        }
    }
}}
