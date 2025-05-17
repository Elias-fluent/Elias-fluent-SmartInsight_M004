using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System.Data;
using SmartInsight.Core.Interfaces;
using System.Linq;
using System.Text;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Microsoft SQL Server connector implementation
    /// </summary>
    [ConnectorMetadata(
        id: "mssql-connector", 
        name: "SQL Server Connector", 
        sourceType: "MSSQL",
        Description = "Connector for Microsoft SQL Server databases.",
        Version = "1.0.0",
        Author = "SmartInsight Team",
        Capabilities = new[] { "read", "extract", "transform" },
        DocumentationUrl = "https://docs.example.com/connectors/mssql")]
    [ConnectorCategory("Database")]
    [ConnectorCategory("SQL Server")]
    [ConnectionParameter(
        name: "server",
        displayName: "Server",
        description: "Database server hostname or instance name",
        type: "string",
        IsRequired = true)]
    [ConnectionParameter(
        name: "port",
        displayName: "Port",
        description: "Database server port",
        type: "integer",
        IsRequired = false,
        DefaultValue = "1433")]
    [ConnectionParameter(
        name: "database",
        displayName: "Database",
        description: "Database name",
        type: "string",
        IsRequired = true)]
    [ConnectionParameter(
        name: "username",
        displayName: "Username",
        description: "Database username",
        type: "string",
        IsRequired = true)]
    [ConnectionParameter(
        name: "password",
        displayName: "Password",
        description: "Database password",
        type: "password",
        IsRequired = true,
        IsSecret = true)]
    [ConnectionParameter(
        name: "trustServerCertificate",
        displayName: "Trust Server Certificate",
        description: "Trust server certificate without validation",
        type: "boolean",
        IsRequired = false,
        DefaultValue = "false")]
    [ConnectionParameter(
        name: "integratedSecurity",
        displayName: "Integrated Security",
        description: "Use Windows Authentication",
        type: "boolean",
        IsRequired = false,
        DefaultValue = "false")]
    [ConnectionParameter(
        name: "connectionTimeout",
        displayName: "Connection Timeout",
        description: "Timeout in seconds for establishing connection",
        type: "integer",
        IsRequired = false,
        DefaultValue = "30")]
    [ConnectionParameter(
        name: "commandTimeout",
        displayName: "Command Timeout",
        description: "Timeout in seconds for command execution",
        type: "integer",
        IsRequired = false,
        DefaultValue = "30")]
    public class MSSQLConnector : IDataSourceConnector
    {
        private readonly ILogger<MSSQLConnector>? _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private IConnectorConfiguration? _configuration;
        private SqlConnection? _connection;
        private bool _disposed;
        private string? _connectionId;
        
        /// <summary>
        /// Creates a new instance of the SQL Server connector
        /// </summary>
        public MSSQLConnector() 
        {
            // Default constructor for use when logger is not available
        }
        
        /// <summary>
        /// Creates a new instance of the SQL Server connector with logging
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public MSSQLConnector(ILogger<MSSQLConnector> logger)
        {
            _logger = logger;
        }
        
        /// <inheritdoc/>
        public string Id => "mssql-connector";
        
        /// <inheritdoc/>
        public string Name => "SQL Server Connector";
        
        /// <inheritdoc/>
        public string SourceType => "MSSQL";
        
        /// <inheritdoc/>
        public string Description => "Connector for Microsoft SQL Server databases.";
        
        /// <inheritdoc/>
        public string Version => "1.0.0";
        
        /// <inheritdoc/>
        public SmartInsight.Core.Interfaces.ConnectionState ConnectionState { get; private set; } = SmartInsight.Core.Interfaces.ConnectionState.Disconnected;
        
        /// <inheritdoc/>
        public event EventHandler<ConnectorStateChangedEventArgs>? StateChanged;
        
        /// <inheritdoc/>
        public event EventHandler<ConnectorErrorEventArgs>? ErrorOccurred;
        
        /// <inheritdoc/>
        public event EventHandler<ConnectorProgressEventArgs>? ProgressChanged;
        
        /// <inheritdoc/>
        public async Task<bool> InitializeAsync(IConnectorConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Initializing SQL Server connector");
            
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
                
                _logger?.LogInformation("SQL Server connector initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing SQL Server connector");
                OnErrorOccurred("Initialize", $"Error initializing connector: {ex.Message}", ex);
                return false;
            }
        }
        
        private Task<ValidationResult> ValidateConfigurationAsync(IConnectorConfiguration configuration)
        {
            // Basic validation of configuration
            var result = ValidationResult.Success();
            
            if (configuration == null)
            {
                result.AddError("Configuration", "Configuration is null");
                return Task.FromResult(result);
            }
            
            // Ensure required options are present
            if (configuration.ConnectionParameters == null || !configuration.ConnectionParameters.Any())
            {
                result.AddError("ConnectionParameters", "ConnectionParameters is missing or invalid");
                return Task.FromResult(result);
            }
            
            // Validate connection parameters
            return ValidateConnectionAsync(configuration.ConnectionParameters);
        }
        
        /// <inheritdoc/>
        public Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams)
        {
            var result = ValidationResult.Success();
            
            // Check required parameters
            if (!connectionParams.TryGetValue("server", out var server) || string.IsNullOrWhiteSpace(server))
            {
                result.AddError("server", "Server name is required");
            }
            
            if (!connectionParams.TryGetValue("database", out var database) || string.IsNullOrWhiteSpace(database))
            {
                result.AddError("database", "Database name is required");
            }
            
            // Check if integrated security is being used, if not then validate username/password
            if (connectionParams.TryGetValue("integratedSecurity", out var integratedSecurityStr) &&
                bool.TryParse(integratedSecurityStr, out var integratedSecurity) &&
                integratedSecurity)
            {
                // Using integrated security, no need to validate username/password
            }
            else
            {
                // Not using integrated security, validate username/password
                if (!connectionParams.TryGetValue("username", out var username) || string.IsNullOrWhiteSpace(username))
                {
                    result.AddError("username", "Username is required when not using integrated security");
                }
                
                if (!connectionParams.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password))
                {
                    result.AddError("password", "Password is required when not using integrated security");
                }
            }
            
            // Validate port if provided
            if (connectionParams.TryGetValue("port", out var portStr) && 
                !string.IsNullOrWhiteSpace(portStr) && 
                (!int.TryParse(portStr, out var port) || port <= 0 || port > 65535))
            {
                result.AddError("port", "Port must be a valid number between 1 and 65535");
            }
            
            // Validate timeouts if provided
            if (connectionParams.TryGetValue("connectionTimeout", out var connectionTimeoutStr) && 
                !string.IsNullOrWhiteSpace(connectionTimeoutStr) && 
                (!int.TryParse(connectionTimeoutStr, out var connectionTimeout) || connectionTimeout < 0))
            {
                result.AddError("connectionTimeout", "Connection timeout must be a non-negative number");
            }
            
            if (connectionParams.TryGetValue("commandTimeout", out var commandTimeoutStr) && 
                !string.IsNullOrWhiteSpace(commandTimeoutStr) && 
                (!int.TryParse(commandTimeoutStr, out var commandTimeout) || commandTimeout < 0))
            {
                result.AddError("commandTimeout", "Command timeout must be a non-negative number");
            }
            
            return Task.FromResult(result);
        }
        
        /// <inheritdoc/>
        public async Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
        {
            // Ensure connection safety with a lock
            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (ConnectionState == SmartInsight.Core.Interfaces.ConnectionState.Connected)
                {
                    _logger?.LogWarning("Already connected to SQL Server");
                    return ConnectionResult.Success(
                        connectionId: _connectionId,
                        serverVersion: null,
                        connectionInfo: new Dictionary<string, object>
                        {
                            ["status"] = "already_connected"
                        });
                }

                // Update connection state
                ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Connecting;
                OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Disconnected, ConnectionState);

                _logger?.LogDebug("Connecting to SQL Server with parameters: {Parameters}", 
                    string.Join(", ", connectionParams.Keys));
                    
                // Store connection parameters
                _configuration = ConnectorConfigurationFactory.Create(
                    "mssql-connector", 
                    "SQL Server Connection", 
                    Guid.Empty, 
                    connectionParams);

                // Validate configuration
                var validationResult = await ValidateConfigurationAsync(_configuration);
                if (!validationResult.IsValid)
                {
                    string errorMessage = FormatValidationErrors(validationResult.Errors);
                    _logger?.LogError("Configuration validation failed: {Errors}", errorMessage);
                    
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Error;
                    OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Connecting, ConnectionState);
                    
                    return ConnectionResult.Failure(errorMessage);
                }

                // Build connection string
                string connectionString = BuildConnectionString(connectionParams);
                
                return await ExecuteSqlWithErrorHandlingAsync(async () =>
                {
                    // Create and open connection
                    _connection = new SqlConnection(connectionString);
                    await _connection.OpenAsync(cancellationToken);
                    
                    // Get server version and details
                    var serverInfo = new Dictionary<string, object>();
                    
                    using (var cmd = new SqlCommand("SELECT @@VERSION", _connection))
                    {
                        string serverVersion = (string)await cmd.ExecuteScalarAsync(cancellationToken);
                        serverInfo["server_version"] = serverVersion;
                    }
                    
                    // Get additional server properties
                    using (var cmd = new SqlCommand(@"
                        SELECT 
                            SERVERPROPERTY('ProductVersion') AS ProductVersion,
                            SERVERPROPERTY('ProductLevel') AS ProductLevel,
                            SERVERPROPERTY('Edition') AS Edition", _connection))
                    {
                        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            serverInfo["product_version"] = reader.GetString(0);
                            serverInfo["product_level"] = reader.GetString(1);
                            serverInfo["edition"] = reader.GetString(2);
                        }
                    }
                    
                    // Generate a connection ID
                    _connectionId = Guid.NewGuid().ToString();
                    
                    // Update state
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Connected;
                    OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Connecting, ConnectionState);
                    
                    _logger?.LogInformation("Successfully connected to SQL Server: {Version}",
                        serverInfo["product_version"]);
                    
                    return ConnectionResult.Success(
                        connectionId: _connectionId,
                        serverVersion: serverInfo["product_version"]?.ToString(),
                        connectionInfo: serverInfo);
                }, "Connect", $"Server: {(connectionParams.TryGetValue("server", out var server) ? server : "unknown")}, Database: {(connectionParams.TryGetValue("database", out var db) ? db : "unknown")}");
            }
            catch (Exception ex)
            {
                ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Error;
                OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Connecting, ConnectionState);
                
                return HandleConnectionError(ex, "Connect", $"Failed to connect to server");
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        
        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
        {
            _logger?.LogDebug("Testing connection to SQL Server");
            
            try
            {
                // Validate configuration
                var connectorConfig = ConnectorConfigurationFactory.Create(
                    "mssql-connector", 
                    "SQL Server Test Connection", 
                    Guid.Empty, 
                    connectionParams);
                
                var validationResult = await ValidateConfigurationAsync(connectorConfig);
                if (!validationResult.IsValid)
                {
                    string errorMessage = FormatValidationErrors(validationResult.Errors);
                    _logger?.LogWarning("Configuration validation failed for connection test: {Errors}", errorMessage);
                    OnErrorOccurred("TestConnection", $"Configuration validation failed: {errorMessage}");
                    return false;
                }
                
                // Build connection string
                string connectionString = BuildConnectionString(connectionParams);
                
                return await ExecuteSqlWithErrorHandlingAsync(async () =>
                {
                    // Create a new connection just for testing
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    // Simple query to verify connectivity
                    using var cmd = new SqlCommand("SELECT 1", connection);
                    await cmd.ExecuteScalarAsync();
                    
                    _logger?.LogInformation("Connection test successful to SQL Server at {Server}",
                        connectionParams.TryGetValue("server", out var serverName) ? serverName : "unknown");
                    
                    return true;
                }, "TestConnection", $"Server: {(connectionParams.TryGetValue("server", out var srv) ? srv : "unknown")}, Database: {(connectionParams.TryGetValue("database", out var database) ? database : "unknown")}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Connection test failed: {Message}", ex.Message);
                OnErrorOccurred("TestConnection", $"Connection test failed: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <inheritdoc/>
        public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (ConnectionState == SmartInsight.Core.Interfaces.ConnectionState.Connected || 
                ConnectionState == SmartInsight.Core.Interfaces.ConnectionState.Error)
            {
                ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Disconnecting;
                OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Connected, ConnectionState);
                
                try
                {
                    if (_connection != null)
                    {
                        await _connection.CloseAsync();
                        _connection.Dispose();
                        _connection = null;
                    }
                    
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Disconnected;
                    OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Disconnecting, ConnectionState);
                    
                    _logger?.LogInformation("Successfully disconnected from SQL Server");
                    return true;
                }
                catch (Exception ex)
                {
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Error;
                    OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Disconnecting, ConnectionState);
                    
                    _logger?.LogError(ex, "Error disconnecting from SQL Server: {Message}", ex.Message);
                    OnErrorOccurred("Disconnect", $"Failed to disconnect: {ex.Message}", ex);
                    return false;
                }
            }
            
            _logger?.LogDebug("Not connected, nothing to disconnect");
            return true;
        }
        
        /// <inheritdoc/>
        public async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;
                
            // Ensure disconnected
            if (ConnectionState != SmartInsight.Core.Interfaces.ConnectionState.Disconnected)
            {
                await DisconnectAsync(cancellationToken);
            }
            
            try
            {
                // Dispose of connection resources
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }
                
                // Release semaphore resources
                _connectionLock.Dispose();
                
                // Clear other references
                _configuration = null;
                _connectionId = null;
                
                _disposed = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing SQL Server connector resources");
            }
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
                _connection?.Dispose();
                _connection = null;
            }
            
            _disposed = true;
        }
        
        /// <summary>
        /// Builds a SQL Server connection string from the provided parameters
        /// </summary>
        /// <param name="connectionParams">Connection parameters</param>
        /// <returns>SQL Server connection string</returns>
        private string BuildConnectionString(IDictionary<string, string> connectionParams)
        {
            var builder = new SqlConnectionStringBuilder();
            
            // Server name/address (required)
            if (connectionParams.TryGetValue("server", out var server))
            {
                // If port is specified, append it to the server name unless it's a named instance
                if (connectionParams.TryGetValue("port", out var portStr) &&
                    int.TryParse(portStr, out var port) &&
                    port != 1433 &&
                    !server.Contains("\\"))
                {
                    builder.DataSource = $"{server},{port}";
                }
                else
                {
                    builder.DataSource = server;
                }
            }
            
            // Database name (required)
            if (connectionParams.TryGetValue("database", out var database))
            {
                builder.InitialCatalog = database;
            }
            
            // Authentication method
            if (connectionParams.TryGetValue("integratedSecurity", out var integratedSecurityStr) &&
                bool.TryParse(integratedSecurityStr, out var integratedSecurity) &&
                integratedSecurity)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                // Username and password authentication
                if (connectionParams.TryGetValue("username", out var username))
                {
                    builder.UserID = username;
                }
                
                if (connectionParams.TryGetValue("password", out var password))
                {
                    builder.Password = password;
                }
            }
            
            // Optional parameters
            if (connectionParams.TryGetValue("trustServerCertificate", out var trustServerCertificateStr) &&
                bool.TryParse(trustServerCertificateStr, out var trustServerCertificate))
            {
                builder.TrustServerCertificate = trustServerCertificate;
            }
            
            if (connectionParams.TryGetValue("connectionTimeout", out var connectionTimeoutStr) &&
                int.TryParse(connectionTimeoutStr, out var connectionTimeout))
            {
                builder.ConnectTimeout = connectionTimeout;
            }
            
            if (connectionParams.TryGetValue("commandTimeout", out var commandTimeoutStr) &&
                int.TryParse(commandTimeoutStr, out var commandTimeout))
            {
                builder.CommandTimeout = commandTimeout;
            }
            
            // Application name for identification in server logs
            builder.ApplicationName = $"SmartInsight.Knowledge.MSSQLConnector/{Version}";
            
            return builder.ConnectionString;
        }
        
        /// <summary>
        /// Helper methods to raise events
        /// </summary>
        private void OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState oldState, SmartInsight.Core.Interfaces.ConnectionState newState)
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
        
        /// <inheritdoc/>
        public ConnectorCapabilities GetCapabilities()
        {
            return new ConnectorCapabilities(
                supportsIncremental: true,
                supportsSchemaDiscovery: true,
                supportsAdvancedFiltering: true,
                supportsPreview: true,
                maxConcurrentExtractions: 5,
                supportedAuthentications: new[] { "basic", "windows" },
                supportedSourceTypes: new[] { "mssql", "database", "sql-server" });
        }
        
        /// <inheritdoc/>
        public IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Description"] = Description,
                ["Version"] = Version,
                ["Author"] = "SmartInsight Team",
                ["Documentation"] = "https://docs.example.com/connectors/mssql",
                ["SupportedFeatures"] = new[] { "read", "extract", "transform" }
            };
        }
        
        /// <inheritdoc/>
        public async Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(
            IDictionary<string, object>? filter = null,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Discovering SQL Server data structures");
            
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _logger?.LogError("Cannot discover data structures: Not connected");
                throw new InvalidOperationException("Not connected to SQL Server");
            }
            
            var structures = new List<DataStructureInfo>();
            
            try
            {
                // Get schema filter if specified
                string? schemaFilter = null;
                if (filter != null && filter.TryGetValue("schema", out var schemaObj) && schemaObj is string schemaValue)
                {
                    schemaFilter = schemaValue;
                }
                
                // Query to get tables and their columns with data types, primary keys, etc.
                string sql = @"
                    SELECT 
                        s.name AS schema_name,
                        t.name AS table_name,
                        c.name AS column_name,
                        ty.name AS data_type,
                        c.is_nullable AS is_nullable,
                        CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                        ep.value AS column_description,
                        c.max_length,
                        c.precision,
                        c.scale,
                        c.is_identity AS is_identity,
                        c.column_id AS ordinal_position
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    INNER JOIN sys.columns c ON t.object_id = c.object_id
                    INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                    LEFT JOIN (
                        SELECT i.object_id, ic.column_id
                        FROM sys.index_columns ic
                        JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                        WHERE i.is_primary_key = 1
                    ) pk ON t.object_id = pk.object_id AND c.column_id = pk.column_id
                    LEFT JOIN sys.extended_properties ep ON 
                        t.object_id = ep.major_id AND 
                        c.column_id = ep.minor_id AND 
                        ep.name = 'MS_Description' AND 
                        ep.class = 1
                    WHERE t.is_ms_shipped = 0";
                
                // Add schema filter if specified
                if (!string.IsNullOrWhiteSpace(schemaFilter))
                {
                    sql += " AND s.name = @schema";
                }
                
                sql += " ORDER BY s.name, t.name, c.column_id";
                
                using var cmd = new SqlCommand(sql, _connection);
                
                // Add schema parameter if needed
                if (!string.IsNullOrWhiteSpace(schemaFilter))
                {
                    cmd.Parameters.Add(new SqlParameter("@schema", schemaFilter));
                }
                
                // Execute query
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                
                // Dictionary to hold tables and their columns
                var tableFields = new Dictionary<string, List<FieldInfo>>();
                
                // Process each row
                while (await reader.ReadAsync(cancellationToken))
                {
                    string schemaName = reader.GetString(0);
                    string tableName = reader.GetString(1);
                    string columnName = reader.GetString(2);
                    string dataType = reader.GetString(3);
                    bool isNullable = reader.GetBoolean(4);
                    bool isPrimaryKey = reader.GetInt32(5) == 1;
                    string? description = reader.IsDBNull(6) ? null : reader.GetString(6);
                    int maxLength = reader.IsDBNull(7) ? 0 : reader.GetInt16(7);
                    byte precision = reader.IsDBNull(8) ? (byte)0 : reader.GetByte(8);
                    byte scale = reader.IsDBNull(9) ? (byte)0 : reader.GetByte(9);
                    bool isIdentity = reader.GetBoolean(10);
                    
                    // Format full name as schema.table
                    string fullTableName = $"{schemaName}.{tableName}";
                    
                    // Initialize list if needed
                    if (!tableFields.ContainsKey(fullTableName))
                    {
                        tableFields[fullTableName] = new List<FieldInfo>();
                    }
                    
                    // Map SQL Server data type to generic type
                    string genericType = MapSqlServerDataType(dataType);
                    
                    // Create field metadata
                    var metadata = new Dictionary<string, object>
                    {
                        ["sql_data_type"] = dataType
                    };
                    
                    if (isIdentity)
                    {
                        metadata["is_identity"] = true;
                    }
                    
                    // Add field info
                    tableFields[fullTableName].Add(new FieldInfo(
                        name: columnName,
                        dataType: genericType,
                        isNullable: isNullable,
                        description: description ?? $"{columnName} ({dataType})",
                        isPrimaryKey: isPrimaryKey,
                        maxLength: maxLength > 0 ? maxLength : null,
                        precision: precision > 0 ? precision : null,
                        scale: scale > 0 ? scale : null,
                        metadata: metadata));
                    
                    // Report progress periodically
                    if (tableFields.Count % 10 == 0)
                    {
                        OnProgressChanged("discovery", tableFields.Count, 100, $"Discovered {tableFields.Count} tables");
                    }
                }
                
                // Get approximate row counts for each table
                if (tableFields.Count > 0)
                {
                    await GetApproximateRowCounts(tableFields.Keys, cancellationToken);
                }
                
                // Convert to data structure info objects
                foreach (var (fullTableName, fields) in tableFields)
                {
                    var parts = fullTableName.Split('.');
                    string tableSchema = parts[0];
                    string tableName = parts[1];
                    
                    // Try to get row count if available
                    long? rowCount = null;
                    if (_tableRowCounts.TryGetValue(fullTableName, out var count))
                    {
                        rowCount = count;
                    }
                    
                    structures.Add(new DataStructureInfo(
                        name: tableName,
                        type: "table",
                        fields: fields,
                        schema: tableSchema,
                        description: $"Table {tableName} in schema {tableSchema}",
                        recordCount: rowCount,
                        metadata: new Dictionary<string, object>
                        {
                            ["schema"] = tableSchema,
                            ["table"] = tableName,
                            ["full_name"] = fullTableName
                        }));
                }
                
                // Final progress
                OnProgressChanged("discovery", 100, 100, $"Discovery complete, found {structures.Count} tables");
                
                return structures;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error discovering SQL Server data structures: {Message}", ex.Message);
                OnErrorOccurred("Discover", $"Error discovering data structures: {ex.Message}", ex);
                throw;
            }
        }
        
        // Dictionary to cache row counts
        private readonly Dictionary<string, long> _tableRowCounts = new Dictionary<string, long>();
        
        /// <summary>
        /// Gets approximate row counts for tables in SQL Server
        /// </summary>
        private async Task GetApproximateRowCounts(IEnumerable<string> tableNames, CancellationToken cancellationToken)
        {
            try
            {
                // Clear existing counts
                _tableRowCounts.Clear();
                
                // Query to get approximate row counts from system views
                string sql = @"
                    SELECT 
                        SCHEMA_NAME(o.schema_id) + '.' + o.name AS full_table_name,
                        CASE 
                            WHEN p.rows > 0 THEN p.rows
                            ELSE 0
                        END AS row_count
                    FROM sys.objects o
                    JOIN sys.partitions p ON o.object_id = p.object_id
                    WHERE o.type = 'U' AND p.index_id IN (0, 1)";
                
                using var cmd = new SqlCommand(sql, _connection);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    string fullName = reader.GetString(0);
                    long rowCount = reader.GetInt64(1);
                    
                    _tableRowCounts[fullName] = rowCount;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving table row counts: {Message}", ex.Message);
                // We'll continue without row counts if this fails
            }
        }
        
        /// <summary>
        /// Maps SQL Server data types to generic data types
        /// </summary>
        private string MapSqlServerDataType(string sqlType)
        {
            return sqlType.ToLowerInvariant() switch
            {
                "bit" => "boolean",
                "tinyint" or "smallint" or "int" or "bigint" => "integer",
                "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
                "float" or "real" => "double",
                "date" => "date",
                "time" => "time",
                "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "datetime",
                "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => "string",
                "binary" or "varbinary" or "image" => "binary",
                "uniqueidentifier" => "uuid",
                "xml" => "xml",
                "geography" or "geometry" => "geometry",
                "hierarchyid" => "string",
                "sql_variant" => "object",
                _ => "string" // Default for unknown types
            };
        }
        
        /// <inheritdoc/>
        public async Task<ExtractionResult> ExtractDataAsync(
            ExtractionParameters extractionParams,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Extracting data from SQL Server");
            
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _logger?.LogError("Cannot extract data: Not connected to SQL Server");
                throw new InvalidOperationException("Not connected to SQL Server");
            }
            
            try
            {
                // Check if target structure is specified
                if (extractionParams.TargetStructures == null || extractionParams.TargetStructures.Count == 0)
                {
                    _logger?.LogError("No target structures specified for extraction");
                    return ExtractionResult.Failure("No target structures specified");
                }
                
                var startTime = DateTime.UtcNow;
                var data = new List<IDictionary<string, object>>();
                int rowCount = 0;
                
                foreach (var structureName in extractionParams.TargetStructures)
                {
                    // Split the structure name to get schema and table
                    string schema = "dbo";  // Default schema
                    string table = structureName;
                    
                    if (structureName.Contains('.'))
                    {
                        var parts = structureName.Split('.');
                        schema = parts[0];
                        table = parts[1];
                    }
                    
                    _logger?.LogDebug("Extracting data from {Schema}.{Table}", schema, table);
                    
                    // Special handling for incremental extraction using SQL Server change tracking
                    if (extractionParams.IncrementalExtraction && 
                        extractionParams.ChangesFromDate.HasValue &&
                        string.IsNullOrEmpty(extractionParams.ChangeTrackingField))
                    {
                        _logger?.LogInformation("Using SQL Server native change tracking for incremental extraction");
                        
                        // Calculate last sync version from date (assuming version is stored as unix timestamp)
                        long lastSyncVersion = 0;
                        
                        // If ChangesFromDate is provided but we don't have a tracking field, 
                        // treat it as a potential version number
                        var versionOption = extractionParams.Options?.TryGetValue("SYS_CHANGE_VERSION", out var versionValue) == true ? versionValue : null;
                        if (versionOption != null)
                        {
                            if (versionOption is long longVersion)
                            {
                                lastSyncVersion = longVersion;
                            }
                            else if (versionOption is int intVersion)
                            {
                                lastSyncVersion = intVersion;
                            }
                            else if (long.TryParse(versionOption.ToString(), out var parsedVersion))
                            {
                                lastSyncVersion = parsedVersion;
                            }
                        }
                        
                        _logger?.LogDebug("Using SYS_CHANGE_VERSION: {Version} for incremental extraction", lastSyncVersion);
                        
                        // Get changed rows using change tracking
                        var changedRows = await GetChangedRowsAsync(schema, table, lastSyncVersion, cancellationToken);
                        
                        // Add to result set
                        data.AddRange(changedRows);
                        rowCount += changedRows.Count;
                        
                        // Get the latest change tracking version
                        var ctInfo = await GetChangeTrackingInfoAsync(schema, table, cancellationToken);
                        
                        // Create result with continuation token containing the current version
                        return ExtractionResult.Success(
                            data,
                            rowCount,
                            (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                            hasMoreRecords: false,
                            continuationToken: ctInfo.CurrentVersion.ToString());
                    }
                    
                    // Standard extraction with date-based incremental approach
                    var sqlBuilder = new StringBuilder();
                    
                    // Build SELECT clause
                    if (extractionParams.IncludeFields != null && extractionParams.IncludeFields.Count > 0)
                    {
                        sqlBuilder.Append("SELECT ");
                        for (int i = 0; i < extractionParams.IncludeFields.Count; i++)
                        {
                            if (i > 0) sqlBuilder.Append(", ");
                            sqlBuilder.Append("[").Append(EscapeIdentifier(extractionParams.IncludeFields[i])).Append("]");
                        }
                    }
                    else
                    {
                        sqlBuilder.Append("SELECT *");
                    }
                    
                    // Add FROM clause with schema and table
                    sqlBuilder.Append(" FROM [").Append(EscapeIdentifier(schema)).Append("].[")
                              .Append(EscapeIdentifier(table)).Append("]");
                    
                    // Add filtering if specified
                    if (extractionParams.FilterCriteria != null && extractionParams.FilterCriteria.Count > 0)
                    {
                        sqlBuilder.Append(" WHERE ");
                        int filterCount = 0;
                        
                        foreach (var filter in extractionParams.FilterCriteria)
                        {
                            if (filterCount > 0) sqlBuilder.Append(" AND ");
                            sqlBuilder.Append("[").Append(EscapeIdentifier(filter.Key)).Append("] = @")
                                      .Append(filter.Key);
                            filterCount++;
                        }
                    }
                    
                    // Add incremental extraction filter if specified
                    if (extractionParams.IncrementalExtraction && 
                        !string.IsNullOrEmpty(extractionParams.ChangeTrackingField) && 
                        extractionParams.ChangesFromDate.HasValue)
                    {
                        if (extractionParams.FilterCriteria == null || extractionParams.FilterCriteria.Count == 0)
                        {
                            sqlBuilder.Append(" WHERE ");
                        }
                        else
                        {
                            sqlBuilder.Append(" AND ");
                        }
                        
                        sqlBuilder.Append("[").Append(EscapeIdentifier(extractionParams.ChangeTrackingField))
                                  .Append("] >= @changesFromDate");
                    }
                    
                    // Add ORDER BY clause for the primary key if available
                    // This ensures consistent results for pagination
                    var primaryKeyFields = await GetPrimaryKeyFieldsAsync(schema, table, cancellationToken);
                    if (primaryKeyFields.Count > 0)
                    {
                        sqlBuilder.Append(" ORDER BY ");
                        
                        for (int i = 0; i < primaryKeyFields.Count; i++)
                        {
                            if (i > 0) sqlBuilder.Append(", ");
                            sqlBuilder.Append("[").Append(EscapeIdentifier(primaryKeyFields[i])).Append("]");
                        }
                    }
                    
                    // Add OFFSET/FETCH for pagination if supported (SQL Server 2012+)
                    int offset = 0;
                    int maxRecords = extractionParams.MaxRecords > 0 ? extractionParams.MaxRecords : 1000;
                    
                    // Add pagination with OFFSET/FETCH NEXT syntax
                    // Note: ORDER BY is required for OFFSET/FETCH to work
                    if (primaryKeyFields.Count > 0) 
                    {
                        sqlBuilder.Append($" OFFSET {offset} ROWS FETCH NEXT {maxRecords} ROWS ONLY");
                    }
                    else
                    {
                        // If no primary keys, add a TOP clause instead (less ideal for pagination)
                        string currentSql = sqlBuilder.ToString();
                        sqlBuilder.Clear();
                        sqlBuilder.Append(currentSql.Replace("SELECT ", $"SELECT TOP {maxRecords} "));
                    }
                    
                    string sqlQuery = sqlBuilder.ToString();
                    _logger?.LogDebug("Executing SQL query: {Sql}", sqlQuery);
                    
                    // Create command
                    using var cmd = new SqlCommand(sqlQuery, _connection);
                    
                    // Add parameters for filters
                    if (extractionParams.FilterCriteria != null)
                    {
                        foreach (var filter in extractionParams.FilterCriteria)
                        {
                            var param = new SqlParameter($"@{filter.Key}", filter.Value ?? DBNull.Value);
                            cmd.Parameters.Add(param);
                        }
                    }
                    
                    // Add parameter for incremental extraction
                    if (extractionParams.IncrementalExtraction && 
                        !string.IsNullOrEmpty(extractionParams.ChangeTrackingField) && 
                        extractionParams.ChangesFromDate.HasValue)
                    {
                        var param = new SqlParameter("@changesFromDate", extractionParams.ChangesFromDate.Value);
                        cmd.Parameters.Add(param);
                    }
                    
                    // Execute query
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    
                    // Process rows
                    int tableRowCount = 0;
                    
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        tableRowCount++;
                        
                        // Report progress periodically
                        if (tableRowCount % 100 == 0 || tableRowCount == 1)
                        {
                            OnProgressChanged("extraction", tableRowCount, maxRecords, $"Extracted {tableRowCount} of {maxRecords} records");
                        }
                        
                        var rowData = new Dictionary<string, object>();
                        
                        // Process each column
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            object value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                            
                            // Convert SQL Server specific types if needed
                            if (value is DateTime dateTime)
                            {
                                // Ensure consistent DateTime formatting
                                value = dateTime;
                            }
                            else if (value is byte[] bytes)
                            {
                                value = Convert.ToBase64String(bytes);
                            }
                            // Let the serializer handle any other SQL Server specific types
                            
                            rowData[columnName] = value;
                        }
                        
                        data.Add(rowData);
                    }
                    
                    rowCount += tableRowCount;
                    
                    // Final progress
                    OnProgressChanged("extraction", tableRowCount, tableRowCount, $"Extraction complete, extracted {tableRowCount} records");
                }
                
                // Calculate execution time
                var endTime = DateTime.UtcNow;
                long executionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
                
                // Determine if there are more records based on requested limit
                bool hasMoreRecords = rowCount >= extractionParams.MaxRecords && extractionParams.MaxRecords > 0;
                
                _logger?.LogInformation("Extracted {Count} records in {Time}ms", 
                    rowCount, executionTimeMs);
                
                return ExtractionResult.Success(
                    data, 
                    rowCount, 
                    executionTimeMs,
                    hasMoreRecords: hasMoreRecords);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error extracting data from SQL Server: {Message}", ex.Message);
                OnErrorOccurred("Extract", $"Error extracting data: {ex.Message}", ex);
                return ExtractionResult.Failure(ex.Message);
            }
        }
        
        /// <summary>
        /// Gets the primary key fields for a table
        /// </summary>
        private async Task<List<string>> GetPrimaryKeyFieldsAsync(string schema, string table, CancellationToken cancellationToken)
        {
            var result = new List<string>();
            
            try
            {
                string sql = @"
                    SELECT c.name AS column_name
                    FROM sys.indexes i
                    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    JOIN sys.tables t ON i.object_id = t.object_id
                    JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE i.is_primary_key = 1
                    AND s.name = @schema
                    AND t.name = @table
                    ORDER BY ic.key_ordinal";
                
                using var cmd = new SqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving primary key fields: {Message}", ex.Message);
                // Continue without primary key information
            }
            
            return result;
        }
        
        /// <summary>
        /// Escapes SQL Server identifiers
        /// </summary>
        private string EscapeIdentifier(string identifier)
        {
            // SQL Server identifiers are escaped by replacing ] with ]]
            return identifier.Replace("]", "]]");
        }
        
        /// <inheritdoc/>
        public async Task<TransformationResult> TransformDataAsync(
            IEnumerable<IDictionary<string, object>> data,
            TransformationParameters transformationParams,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Transforming SQL Server data");
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Create a new list to hold transformed data
                var transformedData = new List<IDictionary<string, object>>();
                var ruleResults = new List<RuleExecutionResult>();
                
                // Count total items first
                int total = 0;
                foreach (var _ in data) { total++; }
                
                int successCount = 0;
                int failureCount = 0;
                
                // Process each record
                int i = 0;
                foreach (var record in data)
                {
                    // Report progress periodically
                    if (i % 50 == 0 || i == 0)
                    {
                        OnProgressChanged("transform", i, total, $"Transforming record {i+1} of {total}");
                    }
                    
                    Dictionary<string, object> transformedRecord;
                    
                    // If preserving original data, start with a copy of the original record
                    if (transformationParams.PreserveOriginalData)
                    {
                        transformedRecord = new Dictionary<string, object>(record);
                    }
                    else
                    {
                        transformedRecord = new Dictionary<string, object>();
                        
                        // Copy the record as is for initial state
                        foreach (var field in record)
                        {
                            transformedRecord[field.Key] = field.Value;
                        }
                    }
                    
                    bool skipRecord = false;
                    bool recordSuccess = true;
                    
                    // Apply rules if specified and record is not skipped
                    if (transformationParams.Rules != null && transformationParams.Rules.Count > 0)
                    {
                        foreach (var rule in transformationParams.Rules.OrderBy(r => r.Order))
                        {
                            var ruleStartTime = DateTime.UtcNow;
                            bool ruleSuccess = true;
                            string? errorMessage = null;
                            
                            try
                            {
                                // Check condition if specified
                                if (!string.IsNullOrEmpty(rule.Condition))
                                {
                                    // Simple condition evaluation
                                    if (rule.Parameters != null && 
                                        rule.Parameters.TryGetValue("field", out var condField) && 
                                        rule.Parameters.TryGetValue("value", out var condValue) &&
                                        rule.Parameters.TryGetValue("operator", out var condOperator))
                                    {
                                        string fieldName = condField.ToString() ?? "";
                                        string op = condOperator.ToString()?.ToLowerInvariant() ?? "eq";
                                        
                                        if (!record.TryGetValue(fieldName, out var fieldValue))
                                        {
                                            // Skip rule if field doesn't exist
                                            continue;
                                        }
                                        
                                        bool conditionMet = false;
                                        
                                        switch (op)
                                        {
                                            case "eq":
                                                conditionMet = Equals(fieldValue, condValue);
                                                break;
                                            case "ne":
                                                conditionMet = !Equals(fieldValue, condValue);
                                                break;
                                            case "gt":
                                                if (fieldValue is IComparable compGt && condValue is IComparable compValueGt)
                                                {
                                                    conditionMet = compGt.CompareTo(compValueGt) > 0;
                                                }
                                                break;
                                            case "lt":
                                                if (fieldValue is IComparable compLt && condValue is IComparable compValueLt)
                                                {
                                                    conditionMet = compLt.CompareTo(compValueLt) < 0;
                                                }
                                                break;
                                            case "contains":
                                                if (fieldValue is string strValueCond && condValue is string strCondValue)
                                                {
                                                    conditionMet = strValueCond.Contains(strCondValue, StringComparison.OrdinalIgnoreCase);
                                                }
                                                break;
                                            default:
                                                conditionMet = false;
                                                break;
                                        }
                                        
                                        if (!conditionMet)
                                        {
                                            // Skip rule if condition not met
                                            continue;
                                        }
                                    }
                                }
                                
                                // Apply transformation based on rule type
                                switch (rule.Type.ToLowerInvariant())
                                {
                                    case "map":
                                        // Map source field to target field
                                        if (rule.SourceFields.Count > 0 && rule.TargetFields.Count > 0)
                                        {
                                            string sourceField = rule.SourceFields[0];
                                            string targetField = rule.TargetFields[0];
                                            
                                            if (record.TryGetValue(sourceField, out var sourceValue))
                                            {
                                                transformedRecord[targetField] = sourceValue;
                                            }
                                        }
                                        break;
                                        
                                    case "filter":
                                        // Filter record (exclude from result if condition met)
                                        if (rule.SourceFields.Count > 0 && rule.Parameters != null)
                                        {
                                            string fieldName = rule.SourceFields[0];
                                            
                                            if (record.TryGetValue(fieldName, out var fieldValue) &&
                                                rule.Parameters.TryGetValue("value", out var expectedValue))
                                            {
                                                string? comparisonOperator = null;
                                                if (rule.Parameters.TryGetValue("operator", out var operatorObj))
                                                {
                                                    comparisonOperator = operatorObj.ToString()?.ToLowerInvariant();
                                                }
                                                
                                                bool excludeRecord = false;
                                                
                                                // Apply comparison based on operator
                                                switch (comparisonOperator)
                                                {
                                                    case "eq":
                                                    case null: // Default to equals
                                                        excludeRecord = Equals(fieldValue, expectedValue);
                                                        break;
                                                    case "ne":
                                                        excludeRecord = !Equals(fieldValue, expectedValue);
                                                        break;
                                                    case "gt":
                                                        if (fieldValue is IComparable compFilter && expectedValue is IComparable compValueFilter)
                                                        {
                                                            excludeRecord = compFilter.CompareTo(compValueFilter) > 0;
                                                        }
                                                        break;
                                                    case "lt":
                                                        if (fieldValue is IComparable compFilterLt && expectedValue is IComparable compValueFilterLt)
                                                        {
                                                            excludeRecord = compFilterLt.CompareTo(compValueFilterLt) < 0;
                                                        }
                                                        break;
                                                    case "contains":
                                                        if (fieldValue is string strValueFilter && expectedValue is string strExpValue)
                                                        {
                                                            excludeRecord = strValueFilter.Contains(strExpValue, StringComparison.OrdinalIgnoreCase);
                                                        }
                                                        break;
                                                }
                                                
                                                if (excludeRecord)
                                                {
                                                    skipRecord = true;
                                                    break; // Stop processing further rules for this record
                                                }
                                            }
                                        }
                                        break;
                                        
                                    case "transform":
                                        // Apply transformation to field(s)
                                        if (rule.SourceFields.Count > 0 && rule.TargetFields.Count > 0 && 
                                            rule.Parameters != null && rule.Parameters.TryGetValue("function", out var funcObj))
                                        {
                                            string function = funcObj.ToString()?.ToLowerInvariant() ?? "";
                                            string sourceField = rule.SourceFields[0];
                                            string targetField = rule.TargetFields[0];
                                            
                                            if (record.TryGetValue(sourceField, out var fieldValue))
                                            {
                                                switch (function)
                                                {
                                                    case "uppercase":
                                                        if (fieldValue is string strValueUp)
                                                        {
                                                            transformedRecord[targetField] = strValueUp.ToUpperInvariant();
                                                        }
                                                        break;
                                                    case "lowercase":
                                                        if (fieldValue is string strValueLow)
                                                        {
                                                            transformedRecord[targetField] = strValueLow.ToLowerInvariant();
                                                        }
                                                        break;
                                                    case "trim":
                                                        if (fieldValue is string strValueTrim)
                                                        {
                                                            transformedRecord[targetField] = strValueTrim.Trim();
                                                        }
                                                        break;
                                                    case "replace":
                                                        if (fieldValue is string strValueReplace && 
                                                            rule.Parameters.TryGetValue("find", out var findObj) &&
                                                            rule.Parameters.TryGetValue("replace", out var replaceObj))
                                                        {
                                                            string find = findObj.ToString() ?? "";
                                                            string replace = replaceObj.ToString() ?? "";
                                                            transformedRecord[targetField] = strValueReplace.Replace(find, replace);
                                                        }
                                                        break;
                                                    case "substring":
                                                        if (fieldValue is string strValueSub && 
                                                            rule.Parameters.TryGetValue("start", out var startObj) &&
                                                            int.TryParse(startObj.ToString(), out int start))
                                                        {
                                                            int length = strValueSub.Length - start;
                                                            if (rule.Parameters.TryGetValue("length", out var lengthObj) &&
                                                                int.TryParse(lengthObj.ToString(), out int specifiedLength))
                                                            {
                                                                length = specifiedLength;
                                                            }
                                                            
                                                            if (start >= 0 && start < strValueSub.Length)
                                                            {
                                                                length = Math.Min(length, strValueSub.Length - start);
                                                                transformedRecord[targetField] = strValueSub.Substring(start, length);
                                                            }
                                                        }
                                                        break;
                                                    case "formatdate":
                                                        if (fieldValue is DateTime dateValue && 
                                                            rule.Parameters.TryGetValue("format", out var formatObj))
                                                        {
                                                            string format = formatObj.ToString() ?? "";
                                                            transformedRecord[targetField] = dateValue.ToString(format);
                                                        }
                                                        break;
                                                    case "add":
                                                        if (fieldValue is int intValue && 
                                                            rule.Parameters.TryGetValue("value", out var addValue) &&
                                                            int.TryParse(addValue.ToString(), out int valueToAdd))
                                                        {
                                                            transformedRecord[targetField] = intValue + valueToAdd;
                                                        }
                                                        else if (fieldValue is double doubleValue && 
                                                                rule.Parameters.TryGetValue("value", out var doubleAddValue) &&
                                                                double.TryParse(doubleAddValue.ToString(), out double valueToAddDbl))
                                                        {
                                                            transformedRecord[targetField] = doubleValue + valueToAddDbl;
                                                        }
                                                        break;
                                                    case "multiply":
                                                        if (fieldValue is int intValueMul && 
                                                            rule.Parameters.TryGetValue("value", out var mulValue) &&
                                                            int.TryParse(mulValue.ToString(), out int valueToMul))
                                                        {
                                                            transformedRecord[targetField] = intValueMul * valueToMul;
                                                        }
                                                        else if (fieldValue is double doubleValue && 
                                                                rule.Parameters.TryGetValue("value", out var doubleMulValue) &&
                                                                double.TryParse(doubleMulValue.ToString(), out double valueToMulDbl))
                                                        {
                                                            transformedRecord[targetField] = doubleValue * valueToMulDbl;
                                                        }
                                                        break;
                                                    case "parsedate":
                                                        if (fieldValue is string strValueDate && 
                                                            DateTime.TryParse(strValueDate, out DateTime parsedDate))
                                                        {
                                                            transformedRecord[targetField] = parsedDate;
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                        
                                    case "compute":
                                        // Compute a value based on other fields
                                        if (rule.SourceFields.Count > 0 && rule.TargetFields.Count > 0 && 
                                            rule.Parameters != null && rule.Parameters.TryGetValue("operation", out var opObj))
                                        {
                                            string operation = opObj.ToString()?.ToLowerInvariant() ?? "";
                                            string targetField = rule.TargetFields[0];
                                            
                                            // Get values from source fields
                                            var sourceValues = new List<object?>();
                                            foreach (var sourceField in rule.SourceFields)
                                            {
                                                if (record.TryGetValue(sourceField, out var sourceValue))
                                                {
                                                    sourceValues.Add(sourceValue);
                                                }
                                                else
                                                {
                                                    sourceValues.Add(null);
                                                }
                                            }
                                            
                                            // Skip if no valid source values
                                            if (sourceValues.Count == 0 || sourceValues.All(v => v == null))
                                            {
                                                continue;
                                            }
                                            
                                            switch (operation)
                                            {
                                                case "concat":
                                                    var concatenated = string.Join(
                                                        rule.Parameters.TryGetValue("separator", out var sepObj) ? sepObj.ToString() ?? "" : "",
                                                        sourceValues.Select(v => v?.ToString() ?? "")
                                                    );
                                                    transformedRecord[targetField] = concatenated;
                                                    break;
                                                case "sum":
                                                    if (sourceValues.All(v => v is int || v is double || v is decimal))
                                                    {
                                                        double sum = 0;
                                                        foreach (var val in sourceValues)
                                                        {
                                                            if (val is int intVal)
                                                                sum += intVal;
                                                            else if (val is double dblVal)
                                                                sum += dblVal;
                                                            else if (val is decimal decVal)
                                                                sum += (double)decVal;
                                                        }
                                                        transformedRecord[targetField] = sum;
                                                    }
                                                    break;
                                                case "avg":
                                                    if (sourceValues.Any() && sourceValues.All(v => v is int || v is double || v is decimal))
                                                    {
                                                        double sum = 0;
                                                        int count = 0;
                                                        foreach (var val in sourceValues)
                                                        {
                                                            if (val != null)
                                                            {
                                                                if (val is int intVal)
                                                                    sum += intVal;
                                                                else if (val is double dblVal)
                                                                    sum += dblVal;
                                                                else if (val is decimal decVal)
                                                                    sum += (double)decVal;
                                                                count++;
                                                            }
                                                        }
                                                        transformedRecord[targetField] = count > 0 ? sum / count : 0;
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                        
                                    case "delete":
                                        // Delete specified fields
                                        foreach (var field in rule.SourceFields)
                                        {
                                            transformedRecord.Remove(field);
                                        }
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                ruleSuccess = false;
                                errorMessage = ex.Message;
                                _logger?.LogError(ex, "Error applying transformation rule {RuleId}: {Message}", rule.Id, ex.Message);
                                
                                // If fail on error is set, consider the whole record as failed
                                if (transformationParams.FailOnError)
                                {
                                    recordSuccess = false;
                                    throw; // Rethrow to be caught by outer try/catch
                                }
                            }
                            finally
                            {
                                var ruleEndTime = DateTime.UtcNow;
                                long ruleExecutionTimeMs = (long)(ruleEndTime - ruleStartTime).TotalMilliseconds;
                                
                                // Add rule execution result
                                ruleResults.Add(new RuleExecutionResult(
                                    ruleId: rule.Id,
                                    isSuccess: ruleSuccess,
                                    executionTimeMs: ruleExecutionTimeMs,
                                    recordCount: 1, // One record at a time
                                    successCount: ruleSuccess ? 1 : 0,
                                    failureCount: ruleSuccess ? 0 : 1,
                                    errorMessage: errorMessage
                                ));
                            }
                            
                            // Skip the rest of the rules if this record should be skipped
                            if (skipRecord)
                            {
                                break;
                            }
                        }
                    }
                    
                    // Add the transformed record to the result if not skipped
                    if (!skipRecord)
                    {
                        transformedData.Add(transformedRecord);
                        
                        if (recordSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    
                    i++;
                    
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger?.LogWarning("Transformation was cancelled after processing {Count} records", i);
                        return TransformationResult.Failure("Transformation was cancelled", 
                            executionTimeMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                            successCount: successCount,
                            failureCount: failureCount,
                            ruleResults: ruleResults);
                    }
                }
                
                // Final progress
                OnProgressChanged("transform", total, total, "Transformation complete");
                
                var endTime = DateTime.UtcNow;
                long executionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
                
                _logger?.LogInformation("Transformed {Count} records in {Time}ms", transformedData.Count, executionTimeMs);
                
                // Return successful result
                return TransformationResult.Success(
                    transformedData, 
                    executionTimeMs,
                    successCount,
                    failureCount,
                    ruleResults);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error transforming SQL Server data: {Message}", ex.Message);
                OnErrorOccurred("Transform", $"Error transforming data: {ex.Message}", ex);
                return TransformationResult.Failure(ex.Message);
            }
        }
        
        /// <summary>
        /// Gets information about change tracking support for a table
        /// </summary>
        private async Task<ChangeTrackingInfo> GetChangeTrackingInfoAsync(string schema, string table, CancellationToken cancellationToken)
        {
            try
            {
                string sql = @"
                    SELECT 
                        t.object_id, 
                        CASE WHEN OBJECTPROPERTYEX(t.object_id, 'TableHasChangeTracking') = 1 THEN 1 ELSE 0 END AS change_tracking_enabled,
                        CHANGE_TRACKING_CURRENT_VERSION() AS current_version,
                        CASE WHEN OBJECTPROPERTYEX(t.object_id, 'TableHasTrackingColumnsUpdated') = 1 THEN 1 ELSE 0 END AS track_columns_updated
                    FROM sys.tables t
                    WHERE SCHEMA_NAME(t.schema_id) = @schema AND t.name = @table";

                using var cmd = new SqlCommand(sql, _connection);
                cmd.Parameters.Add(new SqlParameter("@schema", schema));
                cmd.Parameters.Add(new SqlParameter("@table", table));

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    return new ChangeTrackingInfo
                    {
                        ObjectId = reader.GetInt32(0),
                        IsEnabled = reader.GetInt32(1) == 1,
                        CurrentVersion = reader.GetInt64(2),
                        TrackColumnsUpdated = reader.GetInt32(3) == 1
                    };
                }

                return new ChangeTrackingInfo { IsEnabled = false };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving change tracking information for {Schema}.{Table}: {Message}", 
                    schema, table, ex.Message);
                return new ChangeTrackingInfo { IsEnabled = false };
            }
        }

        /// <summary>
        /// Information about change tracking for a table
        /// </summary>
        private class ChangeTrackingInfo
        {
            public int ObjectId { get; set; }
            public bool IsEnabled { get; set; }
            public long CurrentVersion { get; set; }
            public bool TrackColumnsUpdated { get; set; }
        }
        
        /// <inheritdoc/>
        ~MSSQLConnector()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets changed rows for a table since the specified version
        /// </summary>
        private async Task<List<Dictionary<string, object>>> GetChangedRowsAsync(
            string schema, 
            string table, 
            long lastSyncVersion, 
            CancellationToken cancellationToken)
        {
            var result = new List<Dictionary<string, object>>();
            
            try
            {
                // Get change tracking info for the table
                var ctInfo = await GetChangeTrackingInfoAsync(schema, table, cancellationToken);
                if (!ctInfo.IsEnabled)
                {
                    _logger?.LogWarning("Change tracking is not enabled for table {Schema}.{Table}", schema, table);
                    return result;
                }
                
                // Check if the sync version is valid
                string validVersionSql = "SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(@tableName))";
                using (var validationCmd = new SqlCommand(validVersionSql, _connection))
                {
                    validationCmd.Parameters.Add(new SqlParameter("@tableName", $"{schema}.{table}"));
                    var minValidVersion = (long)await validationCmd.ExecuteScalarAsync(cancellationToken);
                    
                    if (lastSyncVersion < minValidVersion)
                    {
                        _logger?.LogWarning("Change tracking data has been cleaned up. Last sync version {LastVersion} is " +
                                           "less than minimum valid version {MinVersion}. Full reload is required.",
                            lastSyncVersion, minValidVersion);
                        
                        // Return empty result to indicate sync version is invalid
                        return result;
                    }
                }
                
                // Get all primary key columns for the table
                var primaryKeys = await GetPrimaryKeyFieldsAsync(schema, table, cancellationToken);
                
                if (primaryKeys.Count == 0)
                {
                    _logger?.LogWarning("Cannot detect changes for table {Schema}.{Table} because it has no primary key", 
                        schema, table);
                    return result;
                }
                
                // Build dynamic SQL that will join the user table with CHANGETABLE
                // This gets both the change metadata and the current data
                var sql = new StringBuilder();
                sql.Append("SELECT t.*, " +
                           "CT.SYS_CHANGE_VERSION, " +
                           "CT.SYS_CHANGE_OPERATION, " +
                           "CT.SYS_CHANGE_CONTEXT ");
                
                if (ctInfo.TrackColumnsUpdated)
                {
                    sql.Append(", CT.SYS_CHANGE_COLUMNS ");
                }
                
                sql.Append($"FROM [{EscapeIdentifier(schema)}].[{EscapeIdentifier(table)}] AS t ");
                sql.Append($"RIGHT OUTER JOIN CHANGETABLE(CHANGES [{EscapeIdentifier(schema)}].[{EscapeIdentifier(table)}], @lastSyncVersion) AS CT ");
                sql.Append("ON ");
                
                // Add primary key join conditions
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    if (i > 0) sql.Append(" AND ");
                    sql.Append($"t.[{EscapeIdentifier(primaryKeys[i])}] = CT.[{EscapeIdentifier(primaryKeys[i])}]");
                }
                
                sql.Append(" WHERE CT.SYS_CHANGE_VERSION <= @currentVersion");
                
                _logger?.LogDebug("Executing incremental change detection query: {Query}", sql.ToString());
                
                using var cmd = new SqlCommand(sql.ToString(), _connection);
                cmd.Parameters.Add(new SqlParameter("@lastSyncVersion", lastSyncVersion));
                cmd.Parameters.Add(new SqlParameter("@currentVersion", ctInfo.CurrentVersion));
                
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                
                // Process the results
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object>();
                    
                    // Add all columns from the table or NULL for deleted rows
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        object value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                        
                        // Convert SQL Server specific types if needed
                        if (value is DateTime dateTime)
                        {
                            value = dateTime;
                        }
                        else if (value is byte[] bytes)
                        {
                            value = Convert.ToBase64String(bytes);
                        }
                        
                        row[columnName] = value;
                    }
                    
                    result.Add(row);
                }
                
                _logger?.LogInformation("Detected {Count} changed rows in {Schema}.{Table} since version {Version}",
                    result.Count, schema, table, lastSyncVersion);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error detecting changed rows for {Schema}.{Table} since version {Version}: {Message}",
                    schema, table, lastSyncVersion, ex.Message);
                throw;
            }
        }
        
        #region Error Handling and Logging

        // Common SQL Server error codes
        private static class SqlErrorCodes
        {
            // Connection errors
            public const int LoginFailed = 18456;
            public const int ConnectionTimeout = -2;
            public const int NetworkError = 10060;
            public const int ServerNotFound = 17;
            
            // Permission errors
            public const int PermissionDenied = 229;
            public const int ObjectNotFound = 208;
            
            // Resource errors
            public const int DeadlockVictim = 1205;
            public const int ConnectionKilled = 233;
            
            // Constraint errors
            public const int UniqueConstraintViolation = 2627;
            public const int ForeignKeyViolation = 547;
            public const int CheckConstraintViolation = 547;
        }

        /// <summary>
        /// Logs a SQL exception with appropriate details depending on error type
        /// </summary>
        private void LogSqlException(SqlException ex, string operation, string details = null)
        {
            // Get the actual SQL Server error number
            int errorNumber = ex.Number;
            
            // Log differently based on error category
            if (IsConnectionError(errorNumber))
            {
                _logger?.LogError(ex, "SQL Server connection error during {Operation}: Error {ErrorCode} - {Message}. {Details}", 
                    operation, errorNumber, ex.Message, details);
                
                OnErrorOccurred("Connection", $"SQL Server connection error: {ex.Message}", ex);
            }
            else if (IsPermissionError(errorNumber))
            {
                _logger?.LogError(ex, "SQL Server permission error during {Operation}: Error {ErrorCode} - {Message}. {Details}", 
                    operation, errorNumber, ex.Message, details);
                
                OnErrorOccurred("Permission", $"SQL Server permission error: {ex.Message}", ex);
            }
            else if (IsResourceError(errorNumber))
            {
                _logger?.LogError(ex, "SQL Server resource error during {Operation}: Error {ErrorCode} - {Message}. {Details}", 
                    operation, errorNumber, ex.Message, details);
                
                OnErrorOccurred("Resource", $"SQL Server resource error: {ex.Message}", ex);
            }
            else if (IsConstraintError(errorNumber))
            {
                _logger?.LogError(ex, "SQL Server constraint violation during {Operation}: Error {ErrorCode} - {Message}. {Details}", 
                    operation, errorNumber, ex.Message, details);
                
                OnErrorOccurred("Constraint", $"SQL Server constraint violation: {ex.Message}", ex);
            }
            else
            {
                _logger?.LogError(ex, "SQL Server error during {Operation}: Error {ErrorCode} - {Message}. {Details}", 
                    operation, errorNumber, ex.Message, details);
                
                OnErrorOccurred(operation, $"SQL Server error: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Checks if the SQL error is a connection-related error
        /// </summary>
        private bool IsConnectionError(int errorNumber)
        {
            return errorNumber == SqlErrorCodes.LoginFailed ||
                   errorNumber == SqlErrorCodes.ConnectionTimeout ||
                   errorNumber == SqlErrorCodes.NetworkError ||
                   errorNumber == SqlErrorCodes.ServerNotFound;
        }
        
        /// <summary>
        /// Checks if the SQL error is a permission-related error
        /// </summary>
        private bool IsPermissionError(int errorNumber)
        {
            return errorNumber == SqlErrorCodes.PermissionDenied ||
                   errorNumber == SqlErrorCodes.ObjectNotFound;
        }
        
        /// <summary>
        /// Checks if the SQL error is a resource-related error
        /// </summary>
        private bool IsResourceError(int errorNumber)
        {
            return errorNumber == SqlErrorCodes.DeadlockVictim ||
                   errorNumber == SqlErrorCodes.ConnectionKilled;
        }
        
        /// <summary>
        /// Checks if the SQL error is a constraint violation error
        /// </summary>
        private bool IsConstraintError(int errorNumber)
        {
            return errorNumber == SqlErrorCodes.UniqueConstraintViolation ||
                   errorNumber == SqlErrorCodes.ForeignKeyViolation ||
                   errorNumber == SqlErrorCodes.CheckConstraintViolation;
        }
        
        /// <summary>
        /// Safely executes a SQL query with proper error handling
        /// </summary>
        private async Task<T> ExecuteSqlWithErrorHandlingAsync<T>(
            Func<Task<T>> sqlOperation, 
            string operationName, 
            string details = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await sqlOperation();
            }
            catch (SqlException ex)
            {
                LogSqlException(ex, operationName, details);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during {Operation}: {Message}. {Details}", 
                    operationName, ex.Message, details);
                
                OnErrorOccurred(operationName, $"Error: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes a validation operation with standardized error handling
        /// </summary>
        private async Task<ValidationResult> ExecuteValidationWithErrorHandlingAsync(
            Func<Task<ValidationResult>> validationOperation,
            string operationName,
            string details = null)
        {
            try
            {
                return await validationOperation();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Validation error during {Operation}: {Message}. {Details}", 
                    operationName, ex.Message, details);
                
                OnErrorOccurred(operationName, $"Validation error: {ex.Message}", ex);
                
                // Return a failure result with the exception message
                return ValidationResult.Failure("", $"Validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles SQL connection errors in a standardized way
        /// </summary>
        private ConnectionResult HandleConnectionError(Exception ex, string operationName, string details = null)
        {
            ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Error;
            
            if (ex is SqlException sqlEx)
            {
                LogSqlException(sqlEx, operationName, details);
            }
            else
            {
                _logger?.LogError(ex, "Error during {Operation}: {Message}. {Details}", 
                    operationName, ex.Message, details);
                
                OnErrorOccurred(operationName, $"Error: {ex.Message}", ex);
            }
            
            return ConnectionResult.Failure(ex.Message);
        }

        /// <summary>
        /// Logs detailed metrics about a SQL operation
        /// </summary>
        private void LogOperationMetrics(string operation, int rowsAffected, long executionTimeMs, string details = null)
        {
            _logger?.LogInformation("{Operation} completed: {RowsAffected} rows affected, took {ExecutionTime}ms. {Details}",
                operation, rowsAffected, executionTimeMs, details);
        }
        
        /// <summary>
        /// Formats validation errors into a readable string
        /// </summary>
        /// <param name="errors">Collection of validation errors</param>
        /// <returns>Formatted error message string</returns>
        private string FormatValidationErrors(IEnumerable<ValidationError> errors)
        {
            return string.Join(", ", errors.Select(e => $"{e.FieldName}: {e.ErrorMessage}"));
        }
        
        #endregion
    }
} 