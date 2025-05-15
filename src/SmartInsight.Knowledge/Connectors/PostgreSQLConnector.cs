using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using SmartInsight.Core.Interfaces;
using System.Linq;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// PostgreSQL connector implementation
    /// </summary>
    [ConnectorMetadata(
        id: "postgresql-connector", 
        name: "PostgreSQL Connector", 
        sourceType: "PostgreSQL",
        Description = "Connector for PostgreSQL databases.",
        Version = "1.0.0",
        Author = "SmartInsight Team",
        Capabilities = new[] { "read", "extract", "transform" },
        DocumentationUrl = "https://docs.example.com/connectors/postgresql")]
    [ConnectorCategory("Database")]
    [ConnectorCategory("PostgreSQL")]
    [ConnectionParameter(
        name: "host",
        displayName: "Host",
        description: "Database server hostname or IP address",
        type: "string",
        IsRequired = true)]
    [ConnectionParameter(
        name: "port",
        displayName: "Port",
        description: "Database server port",
        type: "integer",
        IsRequired = false,
        DefaultValue = "5432")]
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
        name: "useSsl",
        displayName: "Use SSL",
        description: "Whether to use SSL for connections",
        type: "boolean",
        IsRequired = false,
        DefaultValue = "true")]
    [ConnectionParameter(
        name: "commandTimeout",
        displayName: "Command Timeout",
        description: "Command timeout in seconds",
        type: "integer",
        IsRequired = false,
        DefaultValue = "30")]
    [ConnectionParameter(
        name: "maxPoolSize",
        displayName: "Max Pool Size",
        description: "Maximum connection pool size",
        type: "integer",
        IsRequired = false,
        DefaultValue = "100")]
    [ParameterValidation(
        parameterName: "host",
        validationType: "regex",
        validationRule: @"^[a-zA-Z0-9_.-]+$|^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")]
    [ParameterValidation(
        parameterName: "port",
        validationType: "range",
        validationRule: "1-65535")]
    [ParameterValidation(
        parameterName: "commandTimeout",
        validationType: "range",
        validationRule: "1-3600")]
    [ParameterValidation(
        parameterName: "maxPoolSize",
        validationType: "range",
        validationRule: "1-1000")]
    public class PostgreSQLConnector : IDataSourceConnector, IDisposable
    {
        private readonly ILogger<PostgreSQLConnector>? _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private IConnectorConfiguration? _configuration;
        private NpgsqlConnection? _connection;
        private bool _disposed;
        private string? _connectionId;
        
        /// <summary>
        /// Creates a new instance of the PostgreSQL connector
        /// </summary>
        public PostgreSQLConnector() 
        {
            // Default constructor for use when logger is not available
        }
        
        /// <summary>
        /// Creates a new instance of the PostgreSQL connector with logging
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public PostgreSQLConnector(ILogger<PostgreSQLConnector> logger)
        {
            _logger = logger;
        }
        
        /// <inheritdoc/>
        public string Id => "postgresql-connector";
        
        /// <inheritdoc/>
        public string Name => "PostgreSQL Connector";
        
        /// <inheritdoc/>
        public string SourceType => "PostgreSQL";
        
        /// <inheritdoc/>
        public string Description => "Connector for PostgreSQL databases.";
        
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
            _logger?.LogDebug("Initializing PostgreSQL connector");
            
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
                
                _logger?.LogInformation("PostgreSQL connector initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing PostgreSQL connector");
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
                if (!connectionParams.TryGetValue("host", out var host) || string.IsNullOrWhiteSpace(host))
                {
                    errors.Add(new ValidationError("host", "Host is required"));
                }
                else if (!IsValidHostName(host))
                {
                    errors.Add(new ValidationError("host", "Host must be a valid hostname or IP address"));
                }
                
                if (!connectionParams.TryGetValue("database", out var database) || string.IsNullOrWhiteSpace(database))
                {
                    errors.Add(new ValidationError("database", "Database name is required"));
                }
                
                if (!connectionParams.TryGetValue("username", out var username) || string.IsNullOrWhiteSpace(username))
                {
                    errors.Add(new ValidationError("username", "Username is required"));
                }
                
                if (!connectionParams.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password))
                {
                    errors.Add(new ValidationError("password", "Password is required"));
                }
                
                // Validate optional parameters
                if (connectionParams.TryGetValue("port", out var portStr) && !string.IsNullOrWhiteSpace(portStr))
                {
                    if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
                    {
                        errors.Add(new ValidationError("port", "Port must be between 1 and 65535"));
                    }
                }
                
                if (connectionParams.TryGetValue("commandTimeout", out var commandTimeoutStr) && !string.IsNullOrWhiteSpace(commandTimeoutStr))
                {
                    if (!int.TryParse(commandTimeoutStr, out int commandTimeout) || commandTimeout < 1 || commandTimeout > 3600)
                    {
                        errors.Add(new ValidationError("commandTimeout", "Command timeout must be between 1 and 3600 seconds"));
                    }
                }
                
                if (connectionParams.TryGetValue("maxPoolSize", out var poolSizeStr) && !string.IsNullOrWhiteSpace(poolSizeStr))
                {
                    if (!int.TryParse(poolSizeStr, out int poolSize) || poolSize < 1 || poolSize > 1000)
                    {
                        errors.Add(new ValidationError("maxPoolSize", "Max pool size must be between 1 and 1000"));
                    }
                }
                
                // Add warnings for optional parameters
                if (!connectionParams.ContainsKey("useSsl") || string.IsNullOrWhiteSpace(connectionParams["useSsl"]))
                {
                    warnings.Add("useSsl parameter not specified, using default value (true)");
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
            if (configuration.HasValue("commandTimeout"))
            {
                int commandTimeout = configuration.GetValue<int>("commandTimeout", 30);
                if (commandTimeout < 1 || commandTimeout > 3600)
                {
                    errors.Add(new ValidationError("commandTimeout", "Command timeout must be between 1 and 3600 seconds"));
                }
            }
            
            if (configuration.HasValue("maxPoolSize"))
            {
                int maxPoolSize = configuration.GetValue<int>("maxPoolSize", 100);
                if (maxPoolSize < 1 || maxPoolSize > 1000)
                {
                    errors.Add(new ValidationError("maxPoolSize", "Max pool size must be between 1 and 1000"));
                }
            }
            
            if (errors.Count > 0)
            {
                return ValidationResult.Failure(errors, warnings);
            }
            
            return ValidationResult.Success(warnings);
        }
        
        /// <summary>
        /// Validates a hostname or IP address
        /// </summary>
        private bool IsValidHostName(string host)
        {
            // Simple validation for hostname or IP address
            return !string.IsNullOrWhiteSpace(host) && 
                   (System.Text.RegularExpressions.Regex.IsMatch(host, @"^[a-zA-Z0-9_.-]+$") || 
                    System.Text.RegularExpressions.Regex.IsMatch(host, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"));
        }
        
        /// <summary>
        /// Builds a connection string from connection parameters
        /// </summary>
        private string BuildConnectionString(IDictionary<string, string> connectionParams)
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = connectionParams["host"],
                Database = connectionParams["database"],
                Username = connectionParams["username"],
                Password = connectionParams["password"]
            };
            
            // Set optional parameters if provided
            if (connectionParams.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var port))
            {
                builder.Port = port;
            }
            
            if (connectionParams.TryGetValue("useSsl", out var sslStr) && bool.TryParse(sslStr, out var useSsl))
            {
                builder.SslMode = useSsl ? Npgsql.SslMode.Require : Npgsql.SslMode.Disable;
            }
            else
            {
                // Default to using SSL
                builder.SslMode = Npgsql.SslMode.Require;
            }
            
            if (connectionParams.TryGetValue("commandTimeout", out var timeoutStr) && int.TryParse(timeoutStr, out var timeout))
            {
                builder.CommandTimeout = timeout;
            }
            
            if (connectionParams.TryGetValue("maxPoolSize", out var poolSizeStr) && int.TryParse(poolSizeStr, out var poolSize))
            {
                builder.MaxPoolSize = poolSize;
            }
            
            return builder.ConnectionString;
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
        
        /// <inheritdoc/>
        public async Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
        {
            try
            {
                await _connectionLock.WaitAsync(cancellationToken);
                
                try
                {
                    _logger?.LogInformation("Connecting to PostgreSQL server: {Host}, Database: {Database}", 
                        connectionParams.ContainsKey("host") ? connectionParams["host"] : "unknown",
                        connectionParams.ContainsKey("database") ? connectionParams["database"] : "unknown");
                    
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
                    
                    // Build connection string
                    string connectionString = BuildConnectionString(connectionParams);
                    
                    try
                    {
                        // Close existing connection if any
                        if (_connection != null)
                        {
                            await _connection.CloseAsync();
                            await _connection.DisposeAsync();
                            _connection = null;
                        }
                        
                        // Create new connection
                        _connection = new NpgsqlConnection(connectionString);
                        
                        // Open connection
                        await _connection.OpenAsync(cancellationToken);
                        
                        // Get server information
                        string serverVersion = _connection.PostgreSqlVersion.ToString();
                        
                        // Generate a unique connection ID
                        _connectionId = $"pg-{Guid.NewGuid():N}";
                        
                        // Update state
                        ConnectionState = ConnectionState.Connected;
                        OnStateChanged(ConnectionState.Connecting, ConnectionState);
                        
                        _logger?.LogInformation("Successfully connected to PostgreSQL server: {ConnectionId}, Version: {ServerVersion}", 
                            _connectionId, serverVersion);
                        
                        // Return connection result
                        return ConnectionResult.Success(
                            connectionId: _connectionId,
                            serverVersion: serverVersion,
                            connectionInfo: new Dictionary<string, object>
                            {
                                ["connected_at"] = DateTime.UtcNow,
                                ["server"] = connectionParams["host"],
                                ["database"] = connectionParams["database"],
                                ["server_version"] = serverVersion,
                                ["use_ssl"] = GetSslInUse(_connection)
                            });
                    }
                    catch (NpgsqlException ex)
                    {
                        _logger?.LogError(ex, "Error connecting to PostgreSQL server");
                        ConnectionState = ConnectionState.Error;
                        OnStateChanged(ConnectionState.Connecting, ConnectionState);
                        OnErrorOccurred("Connect", ex.Message, ex);
                        
                        return ConnectionResult.Failure(ex.Message);
                    }
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
                _logger?.LogError(ex, "Error connecting to PostgreSQL server");
                ConnectionState = ConnectionState.Error;
                OnStateChanged(ConnectionState.Connecting, ConnectionState);
                OnErrorOccurred("Connect", ex.Message, ex);
                
                return ConnectionResult.Failure(ex.Message);
            }
        }
        
        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
        {
            _logger?.LogDebug("Testing connection to PostgreSQL server");
            
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
        
        // Helper method to check if SSL is in use
        private bool GetSslInUse(NpgsqlConnection connection)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SHOW ssl";
                var result = cmd.ExecuteScalar()?.ToString();
                return string.Equals(result, "on", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Default to false if we can't determine
                return false;
            }
        }
        
        /// <inheritdoc/>
        public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (ConnectionState == ConnectionState.Connected || ConnectionState == ConnectionState.Error)
            {
                ConnectionState = ConnectionState.Disconnecting;
                OnStateChanged(ConnectionState.Connected, ConnectionState);
                
                try
                {
                    if (_connection != null)
                    {
                        await _connection.CloseAsync();
                    }
                    
                    ConnectionState = ConnectionState.Disconnected;
                    OnStateChanged(ConnectionState.Disconnecting, ConnectionState);
                    
                    _logger?.LogInformation("Disconnected from PostgreSQL server");
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disconnecting from PostgreSQL server");
                    OnErrorOccurred("Disconnect", ex.Message, ex);
                    
                    ConnectionState = ConnectionState.Error;
                    OnStateChanged(ConnectionState.Disconnecting, ConnectionState);
                    
                    return false;
                }
            }
            
            return false;
        }
        
        /// <inheritdoc/>
        public async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            // Ensure disconnected
            if (ConnectionState != ConnectionState.Disconnected)
            {
                await DisconnectAsync(cancellationToken);
            }
            
            // Cleanup any resources
            _configuration = null;
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
                supportedAuthentications: new[] { "basic" },
                supportedSourceTypes: new[] { "postgresql", "database" });
        }
        
        /// <inheritdoc/>
        public IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Description"] = Description,
                ["Version"] = Version,
                ["Author"] = "SmartInsight Team",
                ["Documentation"] = "https://docs.example.com/connectors/postgresql",
                ["SupportedFeatures"] = new[] { "read", "extract", "transform" }
            };
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
        
        /// <inheritdoc/>
        public async Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(
            IDictionary<string, object>? filter = null,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Discovering PostgreSQL data structures");
            
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _logger?.LogError("Cannot discover data structures: Not connected");
                throw new InvalidOperationException("Not connected to PostgreSQL server");
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
                
                // Query to get tables and their columns
                string sql = @"
                    SELECT
                        n.nspname AS schema_name,
                        c.relname AS table_name,
                        a.attname AS column_name,
                        t.typname AS data_type,
                        a.attnotnull AS not_null,
                        CASE WHEN con.contype = 'p' THEN true ELSE false END AS is_primary_key,
                        d.description AS column_description
                    FROM
                        pg_catalog.pg_class c
                        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                        JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid
                        JOIN pg_catalog.pg_type t ON t.oid = a.atttypid
                        LEFT JOIN pg_catalog.pg_description d ON d.objoid = c.oid AND d.objsubid = a.attnum
                        LEFT JOIN pg_catalog.pg_constraint con ON con.conrelid = c.oid AND a.attnum = ANY(con.conkey) AND con.contype = 'p'
                    WHERE
                        c.relkind = 'r' AND
                        a.attnum > 0 AND
                        n.nspname NOT LIKE 'pg_%' AND
                        n.nspname <> 'information_schema'";
                
                if (!string.IsNullOrWhiteSpace(schemaFilter))
                {
                    sql += " AND n.nspname = @schema";
                }
                
                sql += " ORDER BY n.nspname, c.relname, a.attnum";
                
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = sql;
                
                if (!string.IsNullOrWhiteSpace(schemaFilter))
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = "schema";
                    param.Value = schemaFilter;
                    cmd.Parameters.Add(param);
                }
                
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                
                var tableFields = new Dictionary<string, List<FieldInfo>>();
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    string schemaName = reader.GetString(0);
                    string tableName = reader.GetString(1);
                    string columnName = reader.GetString(2);
                    string dataType = reader.GetString(3);
                    bool notNull = reader.GetBoolean(4);
                    bool isPrimaryKey = reader.GetBoolean(5);
                    string? description = reader.IsDBNull(6) ? null : reader.GetString(6);
                    
                    // Format full name as schema.table
                    string fullTableName = $"{schemaName}.{tableName}";
                    
                    // Initialize list if needed
                    if (!tableFields.ContainsKey(fullTableName))
                    {
                        tableFields[fullTableName] = new List<FieldInfo>();
                    }
                    
                    // Map PostgreSQL data type to generic type
                    string genericType = MapPostgreSQLDataType(dataType);
                    
                    // Add field info
                    tableFields[fullTableName].Add(new FieldInfo(
                        name: columnName,
                        dataType: genericType,
                        isNullable: !notNull,
                        description: description ?? $"{columnName} ({dataType})",
                        isPrimaryKey: isPrimaryKey));
                        
                    // Report progress periodically
                    if (tableFields.Count % 10 == 0)
                    {
                        OnProgressChanged("discovery", tableFields.Count, 100, $"Discovered {tableFields.Count} tables");
                    }
                }
                
                // Convert to data structure info
                foreach (var (fullTableName, fields) in tableFields)
                {
                    var parts = fullTableName.Split('.');
                    string tableSchema = parts[0];
                    string tableName = parts[1];
                    
                    structures.Add(new DataStructureInfo(
                        name: fullTableName,
                        type: "table",
                        fields: fields,
                        description: $"Table {tableName} in schema {tableSchema}",
                        metadata: new Dictionary<string, object>
                        {
                            ["schema"] = tableSchema,
                            ["table"] = tableName
                        }));
                }
                
                // Final progress
                OnProgressChanged("discovery", 100, 100, $"Discovery complete, found {structures.Count} tables");
                
                return structures;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error discovering PostgreSQL data structures");
                OnErrorOccurred("Discover", $"Error discovering data structures: {ex.Message}", ex);
                throw;
            }
        }
        
        private string MapPostgreSQLDataType(string pgType)
        {
            return pgType.ToLowerInvariant() switch
            {
                "int2" or "int4" or "int8" or "serial" or "serial4" or "serial8" or "smallint" or "integer" or "bigint" => "integer",
                "decimal" or "numeric" or "real" or "double precision" or "float4" or "float8" => "double",
                "money" => "decimal",
                "bool" or "boolean" => "boolean",
                "date" => "date",
                "time" or "timetz" => "time",
                "timestamp" or "timestamptz" => "datetime",
                "interval" => "timespan",
                "uuid" => "uuid",
                "json" or "jsonb" => "json",
                "text" or "varchar" or "character varying" or "char" or "character" or "name" => "string",
                "bytea" => "binary",
                "cidr" or "inet" or "macaddr" => "string",
                "point" or "line" or "lseg" or "box" or "path" or "polygon" or "circle" => "geometry",
                "bit" or "bit varying" => "bitarray",
                // Array types
                var t when t.StartsWith("_") => $"array<{MapPostgreSQLDataType(t.Substring(1))}>",
                // Default/unknown
                _ => "string" 
            };
        }
        
        /// <inheritdoc/>
        public async Task<ExtractionResult> ExtractDataAsync(
            ExtractionParameters extractionParams,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Extracting data from PostgreSQL");
            
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _logger?.LogError("Cannot extract data: Not connected");
                throw new InvalidOperationException("Not connected to PostgreSQL server");
            }
            
            try
            {
                // Check if target structure is specified
                if (extractionParams.TargetStructures == null || extractionParams.TargetStructures.Count == 0)
                {
                    return ExtractionResult.Failure("No target structures specified");
                }
                
                string structureName = extractionParams.TargetStructures[0];
                var data = new List<IDictionary<string, object>>();
                var startTime = DateTime.UtcNow;
                
                // Parse structure name (should be in format schema.table)
                string schema = "public";
                string table = structureName;
                
                if (structureName.Contains('.'))
                {
                    var parts = structureName.Split('.');
                    schema = parts[0];
                    table = parts[1];
                }
                
                // Build SQL query
                string sql = $"SELECT * FROM \"{EscapeIdentifier(schema)}\".\"{EscapeIdentifier(table)}\"";
                
                // Add filtering if specified
                if (extractionParams.FilterCriteria != null && extractionParams.FilterCriteria.Count > 0)
                {
                    var whereConditions = new List<string>();
                    
                    foreach (var filter in extractionParams.FilterCriteria)
                    {
                        whereConditions.Add($"\"{EscapeIdentifier(filter.Key)}\" = @{filter.Key}");
                    }
                    
                    if (whereConditions.Count > 0)
                    {
                        sql += " WHERE " + string.Join(" AND ", whereConditions);
                    }
                }
                
                // Add limit if specified
                int limit = extractionParams.MaxRecords > 0 ? extractionParams.MaxRecords : 1000;
                sql += $" LIMIT {limit}";
                
                // Create command
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = sql;
                
                // Add parameters for filters
                if (extractionParams.FilterCriteria != null)
                {
                    foreach (var filter in extractionParams.FilterCriteria)
                    {
                        var param = cmd.CreateParameter();
                        param.ParameterName = filter.Key;
                        param.Value = filter.Value ?? DBNull.Value;
                        cmd.Parameters.Add(param);
                    }
                }
                
                // Execute query
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                
                // Get the schema information
                var schemaTable = reader.GetSchemaTable();
                if (schemaTable == null)
                {
                    return ExtractionResult.Failure("Failed to retrieve schema information");
                }
                
                // Process rows
                int rowCount = 0;
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    rowCount++;
                    
                    // Report progress periodically
                    if (rowCount % 100 == 0 || rowCount == 1)
                    {
                        OnProgressChanged("extraction", rowCount, limit, $"Extracted {rowCount} of {limit} records");
                    }
                    
                    var rowData = new Dictionary<string, object>();
                    
                    // Process each column
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        object value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                        
                        // Convert types if needed
                        if (value is DateTimeOffset dto)
                        {
                            value = dto.DateTime;
                        }
                        else if (value is byte[] bytes)
                        {
                            value = Convert.ToBase64String(bytes);
                        }
                        
                        rowData[columnName] = value;
                    }
                    
                    data.Add(rowData);
                }
                
                // Final progress
                OnProgressChanged("extraction", limit, limit, $"Extraction complete, extracted {rowCount} records");
                
                // Calculate execution time
                var endTime = DateTime.UtcNow;
                int executionTimeMs = (int)(endTime - startTime).TotalMilliseconds;
                
                return ExtractionResult.Success(data, rowCount, executionTimeMs);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error extracting data from PostgreSQL");
                OnErrorOccurred("Extract", $"Error extracting data: {ex.Message}", ex);
                return ExtractionResult.Failure(ex.Message);
            }
        }
        
        private string EscapeIdentifier(string identifier)
        {
            // PostgreSQL identifiers are escaped by doubling double quotes
            return identifier.Replace("\"", "\"\"");
        }
        
        /// <inheritdoc/>
        public Task<TransformationResult> TransformDataAsync(
            IEnumerable<IDictionary<string, object>> data,
            TransformationParameters transformationParams,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Transforming PostgreSQL data");
            
            try
            {
                // Create a new list to hold transformed data
                var transformedData = new List<IDictionary<string, object>>();
                
                // Count total items first
                int total = 0;
                foreach (var _ in data) { total++; }
                
                // Process each record
                int i = 0;
                foreach (var record in data)
                {
                    // Report progress
                    OnProgressChanged("transform", i, total, $"Transforming record {i+1} of {total}");
                    
                    var transformedRecord = new Dictionary<string, object>();
                    
                    // Apply transforms based on rules
                    bool skipRecord = false;
                    
                    // Copy the record as is for now (with custom transformations coming from rules later)
                    foreach (var field in record)
                    {
                        transformedRecord[field.Key] = field.Value;
                    }
                    
                    // Apply rules if specified
                    if (transformationParams.Rules != null)
                    {
                        foreach (var rule in transformationParams.Rules.OrderBy(r => r.Order))
                        {
                            // Apply rule-based transformations
                            if (rule.Type == "map" && rule.SourceFields.Count > 0 && rule.TargetFields.Count > 0)
                            {
                                // Map source to target fields
                                transformedRecord[rule.TargetFields[0]] = record[rule.SourceFields[0]];
                            }
                            else if (rule.Type == "filter" && !string.IsNullOrEmpty(rule.Condition))
                            {
                                // Simple condition check (exact value match)
                                if (rule.Parameters != null && 
                                    rule.Parameters.TryGetValue("field", out var fieldName) && 
                                    rule.Parameters.TryGetValue("value", out var expectedValue))
                                {
                                    if (record.TryGetValue(fieldName.ToString() ?? "", out var fieldValue) && 
                                        !Equals(fieldValue, expectedValue))
                                    {
                                        skipRecord = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (!skipRecord)
                    {
                        transformedData.Add(transformedRecord);
                    }
                    
                    i++;
                    
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error transforming PostgreSQL data");
                OnErrorOccurred("Transform", $"Error transforming data: {ex.Message}", ex);
                return Task.FromResult(TransformationResult.Failure(ex.Message));
            }
        }
    }
} 