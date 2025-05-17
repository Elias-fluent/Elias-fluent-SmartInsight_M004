using System;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using Microsoft.Extensions.Logging;using MySql.Data.MySqlClient;using System.Data;using SmartInsight.Core.Interfaces;using System.Linq;using System.Text;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// MySQL connector implementation
    /// </summary>
    [ConnectorMetadata(
        id: "mysql-connector", 
        name: "MySQL Connector", 
        sourceType: "MySQL",
        Description = "Connector for MySQL and MariaDB databases.",
        Version = "1.0.0",
        Author = "SmartInsight Team",
        Capabilities = new[] { "read", "extract", "transform" },
        DocumentationUrl = "https://docs.example.com/connectors/mysql")]
    [ConnectorCategory("Database")]
    [ConnectorCategory("MySQL")]
    [ConnectionParameter(
        name: "server",
        displayName: "Server",
        description: "Database server hostname",
        type: "string",
        IsRequired = true)]
    [ConnectionParameter(
        name: "port",
        displayName: "Port",
        description: "Database server port",
        type: "integer",
        IsRequired = false,
        DefaultValue = "3306")]
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
        name: "sslMode",
        displayName: "SSL Mode",
        description: "SSL connection mode",
        type: "string",
        IsRequired = false,
        DefaultValue = "preferred")]
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
    public class MySQLConnector : IDataSourceConnector
    {
        private readonly ILogger<MySQLConnector>? _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private IConnectorConfiguration? _configuration;
        private MySqlConnection? _connection;
        private bool _disposed;
        private string? _connectionId;
        
        /// <summary>
        /// Creates a new instance of the MySQL connector
        /// </summary>
        public MySQLConnector() 
        {
            // Default constructor for use when logger is not available
        }
        
        /// <summary>
        /// Creates a new instance of the MySQL connector with logging
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public MySQLConnector(ILogger<MySQLConnector> logger)
        {
            _logger = logger;
        }
        
        /// <inheritdoc/>
        public string Id => "mysql-connector";
        
        /// <inheritdoc/>
        public string Name => "MySQL Connector";
        
        /// <inheritdoc/>
        public string SourceType => "MySQL";
        
        /// <inheritdoc/>
        public string Description => "Connector for MySQL and MariaDB databases.";
        
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
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogDebug("[Operation:{OperationId}] Initializing MySQL connector", operationId);
            
            if (_disposed)
            {
                _logger?.LogError("[Operation:{OperationId}] Cannot initialize: connector is disposed", operationId);
                OnErrorOccurred("Initialize", "Cannot initialize: connector is disposed");
                return false;
            }
            
            if (configuration == null)
            {
                _logger?.LogError("[Operation:{OperationId}] Configuration cannot be null", operationId);
                OnErrorOccurred("Initialize", "Configuration cannot be null");
                return false;
            }
                
            // Store configuration for later use
            _configuration = configuration;
            
            try
            {
                // Validate configuration
                var validationResult = await ValidateConfigurationAsync(configuration);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e => $"{e.FieldName}: {e.ErrorMessage}"));
                    _logger?.LogError("[Operation:{OperationId}] Configuration validation failed: {Errors}", operationId, errorMessages);
                    OnErrorOccurred("Initialize", $"Configuration validation failed: {errorMessages}");
                    return false;
                }
                
                _logger?.LogInformation("[Operation:{OperationId}] MySQL connector initialized successfully", operationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[Operation:{OperationId}] Error initializing MySQL connector: {Message}", operationId, ex.Message);
                OnErrorOccurred("Initialize", $"Error initializing connector: {ex.Message}", ex);
                return false;
            }
        }
        
        private Task<ValidationResult> ValidateConfigurationAsync(IConnectorConfiguration configuration)
        {
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogDebug("[Operation:{OperationId}] Validating connector configuration", operationId);
            
            // Basic validation of configuration
            var result = ValidationResult.Success();
            
            if (configuration == null)
            {
                result.AddError("Configuration", "Configuration is null");
                _logger?.LogError("[Operation:{OperationId}] Configuration validation failed: Configuration is null", operationId);
                return Task.FromResult(result);
            }
            
            // Ensure required options are present
            if (configuration.ConnectionParameters == null || !configuration.ConnectionParameters.Any())
            {
                result.AddError("ConnectionParameters", "ConnectionParameters is missing or invalid");
                _logger?.LogError("[Operation:{OperationId}] Configuration validation failed: ConnectionParameters is missing or invalid", operationId);
                return Task.FromResult(result);
            }
            
            // Validate connection parameters
            return ValidateConnectionAsync(configuration.ConnectionParameters);
        }
        
        /// <inheritdoc/>
        public Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams)
        {
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogDebug("[Operation:{OperationId}] Validating connection parameters", operationId);
            
            var result = ValidationResult.Success();
            
            try
            {
                // Check required parameters
                if (!connectionParams.TryGetValue("server", out var server) || string.IsNullOrWhiteSpace(server))
                {
                    result.AddError("server", "Server name is required");
                    _logger?.LogError("[Operation:{OperationId}] Validation failed: Server name is required", operationId);
                }
                
                if (!connectionParams.TryGetValue("database", out var database) || string.IsNullOrWhiteSpace(database))
                {
                    result.AddError("database", "Database name is required");
                    _logger?.LogError("[Operation:{OperationId}] Validation failed: Database name is required", operationId);
                }
                
                // If using user/password authentication (not integrated security), 
                // both user and password must be provided
                if (!connectionParams.TryGetValue("username", out var username) || string.IsNullOrWhiteSpace(username))
                {
                    result.AddError("username", "Username is required");
                    _logger?.LogError("[Operation:{OperationId}] Validation failed: Username is required", operationId);
                }
                
                if (!connectionParams.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password))
                {
                    result.AddError("password", "Password is required");
                    _logger?.LogError("[Operation:{OperationId}] Validation failed: Password is required", operationId);
                }
                
                // Validate port if specified
                if (connectionParams.TryGetValue("port", out var portStr) && !string.IsNullOrWhiteSpace(portStr))
                {
                    if (!int.TryParse(portStr, out var port) || port < 1 || port > 65535)
                    {
                        result.AddError("port", "Port must be a valid number between 1 and 65535");
                        _logger?.LogError("[Operation:{OperationId}] Validation failed: Port '{Port}' is invalid", operationId, portStr);
                    }
                }
                
                // Validate connection timeout if specified
                if (connectionParams.TryGetValue("connectionTimeout", out var connectionTimeoutStr) && 
                    !string.IsNullOrWhiteSpace(connectionTimeoutStr))
                {
                    if (!int.TryParse(connectionTimeoutStr, out var connectionTimeout) || connectionTimeout < 0)
                    {
                        result.AddError("connectionTimeout", "Connection timeout must be a positive number");
                        _logger?.LogError("[Operation:{OperationId}] Validation failed: Connection timeout '{Timeout}' is invalid", operationId, connectionTimeoutStr);
                    }
                }
                
                // Validate command timeout if specified
                if (connectionParams.TryGetValue("commandTimeout", out var commandTimeoutStr) && 
                    !string.IsNullOrWhiteSpace(commandTimeoutStr))
                {
                    if (!int.TryParse(commandTimeoutStr, out var commandTimeout) || commandTimeout < 0)
                    {
                        result.AddError("commandTimeout", "Command timeout must be a positive number");
                        _logger?.LogError("[Operation:{OperationId}] Validation failed: Command timeout '{Timeout}' is invalid", operationId, commandTimeoutStr);
                    }
                }
                
                // Validate SSL mode if specified
                if (connectionParams.TryGetValue("sslMode", out var sslMode) && !string.IsNullOrWhiteSpace(sslMode))
                {
                    var validSslModes = new[] { "none", "preferred", "required", "verifyca", "verifyfull" };
                    if (!validSslModes.Contains(sslMode.ToLowerInvariant()))
                    {
                        result.AddError("sslMode", $"SSL mode must be one of: {string.Join(", ", validSslModes)}");
                        _logger?.LogError("[Operation:{OperationId}] Validation failed: SSL mode '{Mode}' is invalid", operationId, sslMode);
                    }
                }
                
                if (result.IsValid)
                {
                    _logger?.LogDebug("[Operation:{OperationId}] Connection parameters validation successful", operationId);
                }
                else
                {
                    var errorCount = result.Errors.Count();
                    _logger?.LogWarning("[Operation:{OperationId}] Connection parameters validation failed with {Count} errors", 
                        operationId, errorCount);
                }
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[Operation:{OperationId}] Exception during connection validation: {Message}", 
                    operationId, ex.Message);
                result.AddError("_general", $"Validation error: {ex.Message}");
                return Task.FromResult(result);
            }
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
        
        /// <summary>
        /// Builds a MySQL connection string from the provided parameters
        /// </summary>
        /// <param name="connectionParams">Connection parameters</param>
        /// <returns>MySQL connection string</returns>
        private string BuildConnectionString(IDictionary<string, string> connectionParams)
        {
            var builder = new MySqlConnectionStringBuilder();
            
            // Server name/address (required)
            if (connectionParams.TryGetValue("server", out var server))
            {
                builder.Server = server;
            }
            
            // Port (default 3306)
            if (connectionParams.TryGetValue("port", out var portStr) && 
                int.TryParse(portStr, out var port))
            {
                builder.Port = (uint)port;
            }
            
            // Database (required)
            if (connectionParams.TryGetValue("database", out var database))
            {
                builder.Database = database;
            }
            
            // Username (required)
            if (connectionParams.TryGetValue("username", out var username))
            {
                builder.UserID = username;
            }
            
            // Password (required)
            if (connectionParams.TryGetValue("password", out var password))
            {
                builder.Password = password;
            }
            
            // SSL Mode
            if (connectionParams.TryGetValue("sslMode", out var sslMode))
            {
                switch (sslMode.ToLowerInvariant())
                {
                    case "none":
                        builder.SslMode = MySqlSslMode.None;
                        break;
                    case "preferred":
                        builder.SslMode = MySqlSslMode.Preferred;
                        break;
                    case "required":
                        builder.SslMode = MySqlSslMode.Required;
                        break;
                    case "verifyca":
                        builder.SslMode = MySqlSslMode.VerifyCA;
                        break;
                    case "verifyfull":
                        builder.SslMode = MySqlSslMode.VerifyFull;
                        break;
                    default:
                        builder.SslMode = MySqlSslMode.Preferred; // Default
                        break;
                }
            }
            
            // Connection timeout (default 30s)
            if (connectionParams.TryGetValue("connectionTimeout", out var connectionTimeoutStr) && 
                int.TryParse(connectionTimeoutStr, out var connectionTimeout))
            {
                builder.ConnectionTimeout = (uint)connectionTimeout;
            }
            
            // Command timeout (default 30s)
            if (connectionParams.TryGetValue("commandTimeout", out var commandTimeoutStr) && 
                int.TryParse(commandTimeoutStr, out var commandTimeout))
            {
                builder.DefaultCommandTimeout = (uint)commandTimeout;
            }
            
            // Additional MySQL specific properties
            if (connectionParams.TryGetValue("allowUserVariables", out var allowUserVariablesStr) &&
                bool.TryParse(allowUserVariablesStr, out var allowUserVariables))
            {
                builder.AllowUserVariables = allowUserVariables;
            }
            
            if (connectionParams.TryGetValue("useCompression", out var useCompressionStr) &&
                bool.TryParse(useCompressionStr, out var useCompression))
            {
                builder.UseCompression = useCompression;
            }
            
            if (connectionParams.TryGetValue("connectionLifetime", out var connectionLifetimeStr) &&
                int.TryParse(connectionLifetimeStr, out var connectionLifetime))
            {
                builder.ConnectionLifeTime = (uint)connectionLifetime;
            }
            
            if (connectionParams.TryGetValue("keepAlive", out var keepAliveStr) &&
                int.TryParse(keepAliveStr, out var keepAlive))
            {
                builder.Keepalive = (uint)keepAlive;
            }
            
            if (connectionParams.TryGetValue("maxPoolSize", out var maxPoolSizeStr) &&
                int.TryParse(maxPoolSizeStr, out var maxPoolSize))
            {
                builder.MaximumPoolSize = (uint)maxPoolSize;
            }
            
            if (connectionParams.TryGetValue("minPoolSize", out var minPoolSizeStr) &&
                int.TryParse(minPoolSizeStr, out var minPoolSize))
            {
                builder.MinimumPoolSize = (uint)minPoolSize;
            }
            
            if (connectionParams.TryGetValue("pooling", out var poolingStr) &&
                bool.TryParse(poolingStr, out var pooling))
            {
                builder.Pooling = pooling;
            }
            
            if (connectionParams.TryGetValue("characterSet", out var characterSet))
            {
                builder.CharacterSet = characterSet;
            }
            
            return builder.ConnectionString;
        }
        
        private void OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState oldState, SmartInsight.Core.Interfaces.ConnectionState newState)
        {
            StateChanged?.Invoke(this, new ConnectorStateChangedEventArgs(Id, oldState, newState));
        }
        
        private void OnErrorOccurred(string operation, string message, Exception? exception = null)
        {
            ErrorOccurred?.Invoke(this, new ConnectorErrorEventArgs(Id, operation, message, exception?.ToString(), exception));
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
                supportsAdvancedFiltering: true,
                supportsResume: true,
                supportsSchemaDiscovery: true,
                supportsPreview: true,
                supportsTransformation: true,
                supportsProgressReporting: true,
                supportedSourceTypes: new[] { "MySQL", "MariaDB" }
            );
        }
        
        /// <inheritdoc/>
        public IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["name"] = Name,
                ["description"] = Description,
                ["sourceType"] = SourceType,
                ["version"] = Version
            };
        }
        
        /// <inheritdoc/>
        public async Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
        {
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogDebug("[Operation:{OperationId}] Starting connection to MySQL server", operationId);
            
            if (_disposed)
            {
                _logger?.LogError("[Operation:{OperationId}] Cannot connect: connector is disposed", operationId);
                OnErrorOccurred("Connect", "Cannot connect: connector is disposed");
                return ConnectionResult.Failure("Cannot connect: connector is disposed");
            }
                
            // Check if already connected
            if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
            {
                _logger?.LogDebug("[Operation:{OperationId}] Already connected to MySQL server", operationId);
                return ConnectionResult.Success(
                    connectionId: _connectionId,
                    serverVersion: null,
                    connectionInfo: new Dictionary<string, object>
                    {
                        ["status"] = "already_connected",
                        ["server"] = connectionParams["server"]
                    });
            }
            
            try
            {
                // Check if we have a configuration
                if (_configuration == null)
                {
                    string errorMessage = "Configuration not initialized, call Initialize first";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("Connect", errorMessage);
                    return ConnectionResult.Failure(errorMessage);
                }
                
                // Lock to prevent multiple simultaneous connection attempts
                // Since connections can cause race conditions
                await _connectionLock.WaitAsync(cancellationToken);
                try
                {
                    // Build connection string
                    var connectionString = BuildConnectionString(_configuration.ConnectionParameters);
                    
                    _logger?.LogDebug("[Operation:{OperationId}] Building MySQL connection with masked connection string", operationId);
                    
                    // Create connection and open it
                    _connection = new MySqlConnection(connectionString);
                    
                    // Try to open the connection with a timeout
                    try
                    {
                        // Set connection timeout from parameters or use default
                        int connectionTimeout = 30; // Default 30 seconds
                        if (_configuration.ConnectionParameters.TryGetValue("connectionTimeout", out var connectionTimeoutStr) && 
                            int.TryParse(connectionTimeoutStr, out var parsedTimeout))
                        {
                            connectionTimeout = parsedTimeout;
                        }
                        
                        _logger?.LogDebug("[Operation:{OperationId}] Connecting to MySQL with timeout of {Timeout} seconds", operationId, connectionTimeout);
                        
                        // Create a cancellation token source with timeout
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        combinedCts.CancelAfter(TimeSpan.FromSeconds(connectionTimeout));
                        
                        await _connection.OpenAsync(combinedCts.Token);
                        
                        // Update connection state
                        ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Connected;
                        
                        // Log successful connection
                        _logger?.LogInformation("[Operation:{OperationId}] Successfully connected to MySQL server", operationId);
                        
                        // Trigger state changed event
                        OnStateChanged(SmartInsight.Core.Interfaces.ConnectionState.Connecting, 
                                      SmartInsight.Core.Interfaces.ConnectionState.Connected);
                        
                        return ConnectionResult.Success(
                            connectionId: _connectionId,
                            serverVersion: null,
                            connectionInfo: new Dictionary<string, object>
                            {
                                ["status"] = "connected",
                                ["server"] = _configuration.ConnectionParameters["server"]
                            });
                    }
                    catch (OperationCanceledException ex)
                    {
                        // Was it a timeout or user cancellation?
                        if (cancellationToken.IsCancellationRequested)
                        {
                            string errorMessage = "Connection attempt canceled by user";
                            _logger?.LogWarning("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                            OnErrorOccurred("Connect", errorMessage, ex);
                        }
                        else
                        {
                            string errorMessage = "Connection attempt timed out";
                            _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                            OnErrorOccurred("Connect", errorMessage, ex);
                        }
                        
                        // Dispose the failed connection
                        _connection?.Dispose();
                        _connection = null;
                        
                        return ConnectionResult.Failure("Connection attempt timed out");
                    }
                    catch (MySqlException ex)
                    {
                        // Handle MySQL-specific errors
                        string errorMessage = $"MySQL connection error: {ex.Message}";
                        _logger?.LogError(ex, "[Operation:{OperationId}] {Message}, Error Code: {ErrorCode}", 
                            operationId, errorMessage, ex.Number);
                        
                        // Provide more specific error messages for common error codes
                        string detailedMessage = ex.Number switch
                        {
                            1042 => "Unable to connect to MySQL server: host not found or inaccessible",
                            1045 => "Access denied: incorrect username or password",
                            1049 => "Unknown database: the specified database does not exist",
                            1130 => "Host not allowed to connect: check host permissions",
                            1152 => "Connection aborted: connection was aborted during handshake",
                            1203 => "User limit reached: too many connections to the server",
                            1226 => "User has exceeded the allowed resource limits",
                            _ => ex.Message
                        };
                        
                        OnErrorOccurred("Connect", $"MySQL Error ({ex.Number}): {detailedMessage}", ex);
                        
                        // Dispose the failed connection
                        _connection?.Dispose();
                        _connection = null;
                        
                        return ConnectionResult.Failure($"MySQL Error ({ex.Number}): {detailedMessage}");
                    }
                }
                finally
                {
                    // Always release the connection lock
                    _connectionLock.Release();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error connecting to MySQL: {ex.Message}";
                _logger?.LogError(ex, "[Operation:{OperationId}] {Message}", operationId, errorMessage);
                OnErrorOccurred("Connect", errorMessage, ex);
                
                // Make sure connection state is updated on error
                ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Disconnected;
                
                // Dispose any partially created connection
                _connection?.Dispose();
                _connection = null;
                
                return ConnectionResult.Failure($"Unexpected error connecting to MySQL: {ex.Message}");
            }
        }
        
        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
        {
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogDebug("[Operation:{OperationId}] Testing MySQL connection", operationId);
            
            if (_disposed)
            {
                _logger?.LogError("[Operation:{OperationId}] Cannot test connection: connector is disposed", operationId);
                OnErrorOccurred("TestConnection", "Cannot test connection: connector is disposed");
                return false;
            }
            
            try
            {
                // Validate connection parameters first
                var validationResult = await ValidateConnectionAsync(connectionParams);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e => $"{e.FieldName}: {e.ErrorMessage}"));
                    _logger?.LogError("[Operation:{OperationId}] Connection validation failed: {Errors}", operationId, errorMessages);
                    OnErrorOccurred("TestConnection", $"Connection validation failed: {errorMessages}");
                    return false;
                }
                
                // Build connection string from parameters
                var connectionString = BuildConnectionString(connectionParams);
                
                _logger?.LogDebug("[Operation:{OperationId}] Testing MySQL connection with masked connection string", operationId);
                
                // Create temporary connection for testing
                using var tempConnection = new MySqlConnection(connectionString);
                try
                {
                    // Set connection timeout from parameters or use default
                    int connectionTimeout = 30; // Default 30 seconds
                    if (connectionParams.TryGetValue("connectionTimeout", out var connectionTimeoutStr) && 
                        int.TryParse(connectionTimeoutStr, out var parsedTimeout))
                    {
                        connectionTimeout = parsedTimeout;
                    }
                    
                    _logger?.LogDebug("[Operation:{OperationId}] Testing MySQL connection with timeout of {Timeout} seconds", 
                        operationId, connectionTimeout);
                        
                    // Create a cancellation token source with timeout
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(connectionTimeout));
                    
                    // Try to open connection with timeout
                    await tempConnection.OpenAsync(cts.Token);
                    
                    // Test a simple query to ensure the connection works and database is accessible
                    if (connectionParams.TryGetValue("database", out var database) && !string.IsNullOrWhiteSpace(database))
                    {
                        _logger?.LogDebug("[Operation:{OperationId}] Testing query execution on database '{Database}'", 
                            operationId, database);
                            
                        // Execute a simple query to verify database access
                        using var cmd = new MySqlCommand("SELECT 1", tempConnection);
                        await cmd.ExecuteScalarAsync(cts.Token);
                    }
                    
                    _logger?.LogInformation("[Operation:{OperationId}] MySQL connection test successful", operationId);
                    return true;
                }
                catch (OperationCanceledException ex)
                {
                    string errorMessage = "Connection test timed out";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("TestConnection", errorMessage, ex);
                    return false;
                }
                catch (MySqlException ex)
                {
                    // Handle MySQL-specific errors
                    string errorMessage = $"MySQL connection test error: {ex.Message}";
                    _logger?.LogError(ex, "[Operation:{OperationId}] {Message}, Error Code: {ErrorCode}", 
                        operationId, errorMessage, ex.Number);
                    
                    // Provide more specific error messages for common error codes
                    string detailedMessage = ex.Number switch
                    {
                        1042 => "Unable to connect to MySQL server: host not found or inaccessible",
                        1045 => "Access denied: incorrect username or password",
                        1049 => "Unknown database: the specified database does not exist",
                        1130 => "Host not allowed to connect: check host permissions",
                        1152 => "Connection aborted: connection was aborted during handshake",
                        1203 => "User limit reached: too many connections to the server",
                        1226 => "User has exceeded the allowed resource limits",
                        _ => ex.Message
                    };
                    
                    OnErrorOccurred("TestConnection", $"MySQL Error ({ex.Number}): {detailedMessage}", ex);
                    return false;
                }
                finally
                {
                    // Ensure connection is closed
                    if (tempConnection.State != System.Data.ConnectionState.Closed)
                    {
                        tempConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error testing MySQL connection: {ex.Message}";
                _logger?.LogError(ex, "[Operation:{OperationId}] {Message}", operationId, errorMessage);
                OnErrorOccurred("TestConnection", errorMessage, ex);
                return false;
            }
        }
        
        /// <inheritdoc/>
        public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (ConnectionState == SmartInsight.Core.Interfaces.ConnectionState.Disconnected)
                {
                    _logger?.LogWarning("Already disconnected from MySQL server");
                    return true;
                }
                
                if (_connection == null)
                {
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Disconnected;
                    return true;
                }
                
                try
                {
                    _logger?.LogDebug("Disconnecting from MySQL server...");
                    
                    // Update state
                    var previousState = ConnectionState;
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Disconnecting;
                    OnStateChanged(previousState, ConnectionState);
                    
                    // Close connection
                    await _connection.CloseAsync();
                    _connection.Dispose();
                    _connection = null;
                    
                    // Update state
                    previousState = ConnectionState;
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Disconnected;
                    OnStateChanged(previousState, ConnectionState);
                    
                    _logger?.LogInformation("Successfully disconnected from MySQL server");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disconnecting from MySQL server: {Message}", ex.Message);
                    OnErrorOccurred("Disconnect", $"Error disconnecting: {ex.Message}", ex);
                    
                    ConnectionState = SmartInsight.Core.Interfaces.ConnectionState.Error;
                    return false;
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        
        /// <inheritdoc/>
        public Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(IDictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
        {
            // To be implemented in a later subtask
            throw new NotImplementedException("DiscoverDataStructures method not yet implemented");
        }
        
        /// <summary>
        /// Extracts incremental changes from a MySQL table since the last extraction
        /// </summary>
        /// <param name="schema">Database schema name</param>
        /// <param name="tableName">Table name</param>
        /// <param name="trackingColumn">Column used for tracking changes (usually a timestamp or version column)</param>
        /// <param name="lastTrackingValue">Last value of tracking column from previous extraction</param>
        /// <param name="filters">Additional filters to apply</param>
        /// <param name="includedFields">Fields to include in the result</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple containing changed data and change tracking information</returns>
        private async Task<(IEnumerable<IDictionary<string, object>> Data, IEnumerable<FieldInfo> Fields, object LastTrackingValue)> ExtractIncrementalChangesAsync(
            string schema,
            string tableName,
            string trackingColumn,
            object? lastTrackingValue,
            IDictionary<string, object>? filters = null,
            IEnumerable<string>? includedFields = null,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Extracting incremental changes from {Schema}.{Table} using tracking column {Column}", 
                schema, tableName, trackingColumn);
            
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("Must be connected to extract incremental changes");
            }
            
            // Validate tracking column exists
            bool trackingColumnExists = await ValidateColumnExistsAsync(schema, tableName, trackingColumn, cancellationToken);
            if (!trackingColumnExists)
            {
                throw new ArgumentException($"Tracking column '{trackingColumn}' does not exist in table '{schema}.{tableName}'");
            }
            
            // Set database context if needed
            var currentDatabase = _connection.Database;
            if (schema != currentDatabase)
            {
                using (var cmd = new MySqlCommand($"USE `{schema}`", _connection))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            
            try
            {
                // Create merged filters - combine user filters with tracking column filter
                var mergedFilters = filters != null ? new Dictionary<string, object>(filters) : new Dictionary<string, object>();
                
                // Add tracking column filter if a previous value exists
                if (lastTrackingValue != null)
                {
                    // Depending on the type of tracking column, we might need different comparison operators
                    // For this implementation, we assume it's a timestamp, auto-increment ID, or version number
                    // that can be compared with > operator
                    mergedFilters[$"{trackingColumn}_gt"] = lastTrackingValue;
                }
                
                // Build the SELECT clause
                var sql = new StringBuilder();
                
                if (includedFields != null && includedFields.Any())
                {
                    // Select only specified fields, but ensure tracking column is included
                    var fields = new HashSet<string>(includedFields);
                    fields.Add(trackingColumn); // Ensure tracking column is always included
                    
                    sql.Append("SELECT ");
                    sql.Append(string.Join(", ", fields.Select(f => $"`{f}`")));
                    sql.Append($" FROM `{tableName}`");
                }
                else
                {
                    // Select all fields
                    sql.Append($"SELECT * FROM `{tableName}`");
                }
                
                // Apply filters as WHERE clauses
                if (mergedFilters != null && mergedFilters.Count > 0)
                {
                    sql.Append(" WHERE ");
                    var filterClauses = new List<string>();
                    
                    foreach (var filter in mergedFilters)
                    {
                        string key = filter.Key;
                        
                        // Check for operation suffix (_gt, _lt, _ge, _le, _ne)
                        string op = "=";
                        string fieldName = key;
                        
                        if (key.EndsWith("_gt"))
                        {
                            op = ">";
                            fieldName = key.Substring(0, key.Length - 3);
                        }
                        else if (key.EndsWith("_lt"))
                        {
                            op = "<";
                            fieldName = key.Substring(0, key.Length - 3);
                        }
                        else if (key.EndsWith("_ge"))
                        {
                            op = ">=";
                            fieldName = key.Substring(0, key.Length - 3);
                        }
                        else if (key.EndsWith("_le"))
                        {
                            op = "<=";
                            fieldName = key.Substring(0, key.Length - 3);
                        }
                        else if (key.EndsWith("_ne"))
                        {
                            op = "<>";
                            fieldName = key.Substring(0, key.Length - 3);
                        }
                        
                        // Handle null values
                        if (filter.Value == null)
                        {
                            if (op == "=")
                                filterClauses.Add($"`{fieldName}` IS NULL");
                            else if (op == "<>")
                                filterClauses.Add($"`{fieldName}` IS NOT NULL");
                            else
                                _logger?.LogWarning("Ignoring null comparison with operator {Op} for field {Field}", op, fieldName);
                        }
                        else
                        {
                            filterClauses.Add($"`{fieldName}` {op} @{key}");
                        }
                    }
                    
                    sql.Append(string.Join(" AND ", filterClauses));
                }
                
                // Order by tracking column to ensure consistent results
                sql.Append($" ORDER BY `{trackingColumn}` ASC");
                
                // Add LIMIT if specified
                if (limit.HasValue)
                {
                    sql.Append($" LIMIT {limit.Value}");
                }
                
                // Create command with SQL and parameters
                using (var cmd = new MySqlCommand(sql.ToString(), _connection))
                {
                    // Add parameters for filters
                    if (mergedFilters != null)
                    {
                        foreach (var filter in mergedFilters)
                        {
                            if (filter.Value != null)
                            {
                                cmd.Parameters.AddWithValue($"@{filter.Key}", filter.Value);
                            }
                        }
                    }
                    
                    // Initialize our return collections
                    var data = new List<IDictionary<string, object>>();
                    List<FieldInfo> fields = new List<FieldInfo>();
                    object newTrackingValue = lastTrackingValue ?? DBNull.Value;
                    
                    // Execute reader and process results
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        // Extract field information from reader schema
                        fields = await GetFieldInfoFromReaderAsync(reader, cancellationToken);
                        
                        // Get index of tracking column
                        int trackingColumnIndex = -1;
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(trackingColumn, StringComparison.OrdinalIgnoreCase))
                            {
                                trackingColumnIndex = i;
                                break;
                            }
                        }
                        
                        if (trackingColumnIndex == -1)
                        {
                            throw new InvalidOperationException($"Tracking column '{trackingColumn}' not found in query results");
                        }
                        
                        // Read and process result rows
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var row = new Dictionary<string, object>();
                            
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string fieldName = reader.GetName(i);
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                
                                // Handle special types (like byte arrays, date/time, etc.)
                                if (value is byte[] bytes)
                                {
                                    // Convert binary data to base64 string for easier transport
                                    value = Convert.ToBase64String(bytes);
                                }
                                else if (value is DateTime dateTime)
                                {
                                    // Ensure consistent DateTime format
                                    value = dateTime.ToString("o");
                                }
                                
                                row[fieldName] = value;
                            }
                            
                            data.Add(row);
                            
                            // Update the last tracking value seen
                            if (!reader.IsDBNull(trackingColumnIndex))
                            {
                                var currentTrackingValue = reader.GetValue(trackingColumnIndex);
                                
                                // Compare with the current max tracking value
                                // This handles different data types appropriately
                                if (newTrackingValue == DBNull.Value || CompareTrackingValues(currentTrackingValue, newTrackingValue) > 0)
                                {
                                    newTrackingValue = currentTrackingValue;
                                }
                            }
                            
                            // Report progress every 1000 rows
                            if (data.Count % 1000 == 0)
                            {
                                OnProgressChanged(
                                    "ExtractIncrementalChanges",
                                    data.Count,
                                    limit ?? -1,
                                    $"Extracted {data.Count} incremental changes from {schema}.{tableName}"
                                );
                            }
                        }
                    }
                    
                    _logger?.LogInformation("Successfully extracted {Count} incremental changes from table {Schema}.{Table}", 
                        data.Count, schema, tableName);
                    
                    return (data, fields, newTrackingValue == DBNull.Value ? null : newTrackingValue);
                }
            }
            finally
            {
                // Restore original database context if changed
                if (schema != currentDatabase)
                {
                    using (var cmd = new MySqlCommand($"USE `{currentDatabase}`", _connection))
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
        }
        
        /// <summary>
        /// Validates if a column exists in a table
        /// </summary>
        private async Task<bool> ValidateColumnExistsAsync(string schema, string tableName, string columnName, CancellationToken cancellationToken)
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("Must be connected to validate column existence");
            }
            
            // Set database context if needed
            var currentDatabase = _connection.Database;
            if (schema != currentDatabase)
            {
                using (var cmd = new MySqlCommand($"USE `{schema}`", _connection))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            
            try
            {
                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @column",
                    _connection))
                {
                    cmd.Parameters.AddWithValue("@schema", schema);
                    cmd.Parameters.AddWithValue("@table", tableName);
                    cmd.Parameters.AddWithValue("@column", columnName);
                    
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    return Convert.ToInt32(result) > 0;
                }
            }
            finally
            {
                // Restore original database context if changed
                if (schema != currentDatabase)
                {
                    using (var cmd = new MySqlCommand($"USE `{currentDatabase}`", _connection))
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
        }
        
        /// <summary>
        /// Compares two tracking values to determine which is greater
        /// </summary>
        /// <returns>Positive if a > b, negative if a < b, 0 if equal</returns>
        private int CompareTrackingValues(object a, object b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            
            // Handle same types
            if (a.GetType() == b.GetType())
            {
                if (a is IComparable comparable)
                {
                    return comparable.CompareTo(b);
                }
            }
            
            // Handle numeric comparisons across different types
            if ((a is IConvertible) && (b is IConvertible))
            {
                try
                {
                    double aVal = Convert.ToDouble(a);
                    double bVal = Convert.ToDouble(b);
                    return aVal.CompareTo(bVal);
                }
                catch
                {
                    // Fall back to string comparison if numeric conversion fails
                }
            }
            
            // Fall back to string comparison
            return a.ToString().CompareTo(b.ToString());
        }
        
        /// <inheritdoc/>
        public async Task<ExtractionResult> ExtractDataAsync(ExtractionParameters extractionParams, CancellationToken cancellationToken = default)
        {
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogInformation("[Operation:{OperationId}] Starting data extraction from MySQL database", operationId);
            
            if (_disposed)
            {
                string errorMessage = "Cannot extract data: connector is disposed";
                _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                OnErrorOccurred("ExtractData", errorMessage);
                return ExtractionResult.Failure(errorMessage, "Connector disposed");
            }
            
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                string errorMessage = "Must be connected to extract data";
                _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                OnErrorOccurred("ExtractData", errorMessage);
                return ExtractionResult.Failure(errorMessage, "Connection not established or not open");
            }
            
            try
            {
                // Validate extraction parameters
                if (extractionParams == null)
                {
                    string errorMessage = "Extraction parameters cannot be null";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("ExtractData", errorMessage);
                    return ExtractionResult.Failure(errorMessage, "Invalid parameters");
                }
                
                if (extractionParams.TargetStructures == null || !extractionParams.TargetStructures.Any())
                {
                    string errorMessage = "Target structures cannot be empty";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("ExtractData", errorMessage);
                    return ExtractionResult.Failure(errorMessage, "Invalid parameters");
                }

                // For MySQL connector, we expect the target structure to be in format "schema.table"
                string fullTableName = extractionParams.TargetStructures.First();
                string[] parts = fullTableName.Split('.');
                
                if (parts.Length != 2)
                {
                    string errorMessage = $"Target structure '{fullTableName}' must be in format 'schema.table'";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("ExtractData", errorMessage);
                    return ExtractionResult.Failure(errorMessage, "Invalid target structure format");
                }
                
                string schema = parts[0];
                string tableName = parts[1];
                var filters = extractionParams.FilterCriteria ?? new Dictionary<string, object>();
                int? limit = extractionParams.MaxRecords > 0 ? extractionParams.MaxRecords : null;
                int? offset = null;
                
                _logger?.LogDebug("[Operation:{OperationId}] Extracting data from MySQL table {Schema}.{Table}", 
                    operationId, schema, tableName);
                
                // Check if we have a batch size in the options
                if (extractionParams.BatchSize > 0)
                {
                    limit = extractionParams.BatchSize;
                    _logger?.LogDebug("[Operation:{OperationId}] Using batch size: {BatchSize}", operationId, limit);
                }
                
                // Check if we have a pagination offset in the options
                if (extractionParams.Options != null && 
                    extractionParams.Options.TryGetValue("offset", out var offsetObj) && 
                    offsetObj is int offsetValue)
                {
                    offset = offsetValue;
                    _logger?.LogDebug("[Operation:{OperationId}] Using offset: {Offset}", operationId, offset);
                }
                
                bool includeTotalCount = extractionParams.Options != null && 
                                       extractionParams.Options.TryGetValue("includeTotalCount", out var includeTotalCountObj) && 
                                       includeTotalCountObj is bool includeTotalCountValue && 
                                       includeTotalCountValue;
                
                _logger?.LogDebug("[Operation:{OperationId}] Include total count: {IncludeTotalCount}", 
                    operationId, includeTotalCount);
                
                var startTime = DateTime.UtcNow;
                
                // Check if this is an incremental extraction
                bool isIncremental = extractionParams.Options != null && 
                                    extractionParams.Options.TryGetValue("incremental", out var incrementalObj) && 
                                    incrementalObj is bool incrementalValue && 
                                    incrementalValue;
                
                _logger?.LogDebug("[Operation:{OperationId}] Incremental extraction: {IsIncremental}", 
                    operationId, isIncremental);
                
                // For incremental extraction, we need a tracking column
                string? trackingColumn = null;
                object? lastTrackingValue = null;
                
                if (isIncremental)
                {
                    // Get tracking column from options
                    if (extractionParams.Options?.TryGetValue("trackingColumn", out var trackingColumnObj) == true && 
                        trackingColumnObj is string trackingColumnValue)
                    {
                        trackingColumn = trackingColumnValue;
                    }
                    else
                    {
                        string errorMessage = "Incremental extraction requires 'trackingColumn' in options";
                        _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                        OnErrorOccurred("ExtractData", errorMessage);
                        return ExtractionResult.Failure(errorMessage, "Missing required parameter");
                    }
                    
                    // Get last tracking value from options
                    if (extractionParams.Options?.TryGetValue("lastTrackingValue", out var lastTrackingValueObj) == true)
                    {
                        lastTrackingValue = lastTrackingValueObj;
                    }
                    
                    _logger?.LogDebug("[Operation:{OperationId}] Using tracking column: {Column}, Last value: {LastValue}", 
                        operationId, trackingColumn, lastTrackingValue ?? "null");
                    
                    try
                    {
                        // Extract incremental changes
                        var (data, fields, newTrackingValue) = await ExtractIncrementalChangesAsync(
                            schema,
                            tableName,
                            trackingColumn,
                            lastTrackingValue,
                            filters,
                            extractionParams.IncludeFields,
                            limit,
                            cancellationToken);
                        
                        // Calculate execution time
                        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        
                        // Create record count
                        int recordCount = data.Count();
                        
                        // Create continuation token with new tracking value for subsequent incremental extractions
                        string? continuationToken = null;
                        if (recordCount > 0)
                        {
                            continuationToken = $"{fullTableName}|{trackingColumn}|{newTrackingValue}";
                        }
                        
                        _logger?.LogInformation("[Operation:{OperationId}] Successfully extracted {Count} incremental records from MySQL table {Schema}.{Table} in {Time}ms", 
                            operationId, recordCount, schema, tableName, executionTime);
                        
                        // Create structure info for the result
                        var structureInfo = new DataStructureInfo(
                            tableName,
                            "TABLE",
                            fields,
                            schema,
                            string.Empty,
                            new Dictionary<string, object>
                            {
                                ["source"] = "mysql",
                                ["full_name"] = fullTableName,
                                ["tracking_column"] = trackingColumn,
                                ["last_tracking_value"] = newTrackingValue?.ToString() ?? "null",
                                ["operation_id"] = operationId
                            }
                        );
                        
                        bool hasMoreRecords = recordCount > 0 && recordCount == limit;
                        
                        return ExtractionResult.Success(
                            data,
                            recordCount,
                            executionTime,
                            structureInfo,
                            hasMoreRecords,
                            continuationToken
                        );
                    }
                    catch (MySqlException ex)
                    {
                        string errorMessage = $"MySQL error during incremental extraction: {ex.Message}";
                        _logger?.LogError(ex, "[Operation:{OperationId}] {Message}, ErrorCode: {ErrorCode}", 
                            operationId, errorMessage, ex.Number);
                        OnErrorOccurred("ExtractIncrementalData", errorMessage, ex);
                        
                        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        return ExtractionResult.Failure(errorMessage, $"MySQL Error ({ex.Number}): {ex.Message}", executionTime);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Error during incremental extraction: {ex.Message}";
                        _logger?.LogError(ex, "[Operation:{OperationId}] {Message}", operationId, errorMessage);
                        OnErrorOccurred("ExtractIncrementalData", errorMessage, ex);
                        
                        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        return ExtractionResult.Failure(errorMessage, ex.ToString(), executionTime);
                    }
                }
                else
                {
                    // Standard extraction (non-incremental)
                    try
                    {
                        // Set database context if needed
                        var currentDatabase = _connection.Database;
                        if (schema != currentDatabase)
                        {
                            _logger?.LogDebug("[Operation:{OperationId}] Switching database context from {Current} to {Target}",
                                operationId, currentDatabase, schema);
                            
                            using (var cmd = new MySqlCommand($"USE `{schema}`", _connection))
                            {
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }
                        }
                        
                        // Get total count if requested
                        long? totalCount = null;
                        if (includeTotalCount)
                        {
                            try
                            {
                                totalCount = await GetRowCountAsync(schema, tableName, filters, cancellationToken);
                                _logger?.LogDebug("[Operation:{OperationId}] Total count for {Schema}.{Table}: {Count}",
                                    operationId, schema, tableName, totalCount);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "[Operation:{OperationId}] Failed to get total count, continuing without it: {Message}",
                                    operationId, ex.Message);
                            }
                        }
                        
                        // Build and execute query with filters, pagination
                        _logger?.LogDebug("[Operation:{OperationId}] Executing query for {Schema}.{Table} with {FilterCount} filters",
                            operationId, schema, tableName, filters?.Count ?? 0);
                            
                        var (data, fields) = await QueryDataAsync(schema, tableName, filters, 
                            extractionParams.IncludeFields, limit, offset, cancellationToken);
                        
                        // Restore original database context if changed
                        if (schema != currentDatabase)
                        {
                            _logger?.LogDebug("[Operation:{OperationId}] Restoring database context to {Original}",
                                operationId, currentDatabase);
                                
                            using (var cmd = new MySqlCommand($"USE `{currentDatabase}`", _connection))
                            {
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }
                        }
                        
                        // Calculate execution time
                        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        
                        // Create record count
                        int recordCount = data.Count();
                        
                        bool hasMoreRecords = totalCount.HasValue && offset.HasValue && limit.HasValue && 
                                              offset.Value + recordCount < totalCount.Value;
                        
                        // Create continuation token if there are more records
                        string? continuationToken = null;
                        if (hasMoreRecords && offset.HasValue && limit.HasValue)
                        {
                            int nextOffset = offset.Value + recordCount;
                            continuationToken = $"{fullTableName}|{nextOffset}";
                            
                            _logger?.LogDebug("[Operation:{OperationId}] Created continuation token for next page: {Token}",
                                operationId, continuationToken);
                        }
                        
                        _logger?.LogInformation("[Operation:{OperationId}] Successfully extracted {Count} records from MySQL table {Schema}.{Table} in {Time}ms", 
                            operationId, recordCount, schema, tableName, executionTime);
                        
                        // Create structure info for the result
                        var structureInfo = new DataStructureInfo(
                            tableName,
                            "TABLE",
                            fields,
                            schema,
                            string.Empty,
                            new Dictionary<string, object>
                            {
                                ["source"] = "mysql",
                                ["full_name"] = fullTableName,
                                ["operation_id"] = operationId,
                                ["total_count"] = totalCount
                            }
                        );
                        
                        return ExtractionResult.Success(
                            data,
                            recordCount,
                            executionTime,
                            structureInfo,
                            hasMoreRecords,
                            continuationToken
                        );
                    }
                    catch (MySqlException ex)
                    {
                        string errorMessage = $"MySQL error during data extraction: {ex.Message}";
                        _logger?.LogError(ex, "[Operation:{OperationId}] {Message}, ErrorCode: {ErrorCode}", 
                            operationId, errorMessage, ex.Number);
                        OnErrorOccurred("ExtractData", errorMessage, ex);
                        
                        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        return ExtractionResult.Failure(errorMessage, $"MySQL Error ({ex.Number}): {ex.Message}", executionTime);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Error extracting data from MySQL: {ex.Message}";
                        _logger?.LogError(ex, "[Operation:{OperationId}] {Message}", operationId, errorMessage);
                        OnErrorOccurred("ExtractData", errorMessage, ex);
                        
                        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                        return ExtractionResult.Failure(errorMessage, ex.ToString(), executionTime);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error during data extraction: {ex.Message}";
                _logger?.LogError(ex, "[Operation:{OperationId}] {Message}", operationId, errorMessage);
                OnErrorOccurred("ExtractData", errorMessage, ex);
                
                var executionTime = 0L; // We can't measure execution time in the error case
                return ExtractionResult.Failure(errorMessage, ex.ToString(), executionTime);
            }
        }
        
        /// <summary>
        /// Gets the total row count for a table with applied filters
        /// </summary>
        private async Task<long> GetRowCountAsync(
            string schema, 
            string tableName,
            IDictionary<string, object> filters,
            CancellationToken cancellationToken)
        {
            var sql = new StringBuilder();
            sql.Append($"SELECT COUNT(*) FROM `{tableName}`");
            
            // Apply filters as WHERE clauses
            if (filters != null && filters.Count > 0)
            {
                sql.Append(" WHERE ");
                var filterClauses = new List<string>();
                
                foreach (var filter in filters)
                {
                    // Handle null values
                    if (filter.Value == null)
                    {
                        filterClauses.Add($"`{filter.Key}` IS NULL");
                    }
                    else
                    {
                        filterClauses.Add($"`{filter.Key}` = @{filter.Key}");
                    }
                }
                
                sql.Append(string.Join(" AND ", filterClauses));
            }
            
            using (var cmd = new MySqlCommand(sql.ToString(), _connection))
            {
                // Add parameters for filters
                if (filters != null)
                {
                    foreach (var filter in filters)
                    {
                        if (filter.Value != null)
                        {
                            cmd.Parameters.AddWithValue($"@{filter.Key}", filter.Value);
                        }
                    }
                }
                
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt64(result);
            }
        }
        
        /// <summary>
        /// Queries data from a table with optional filtering and pagination
        /// </summary>
        private async Task<(IEnumerable<IDictionary<string, object>> Data, IEnumerable<FieldInfo> Fields)> QueryDataAsync(
            string schema,
            string tableName,
            IDictionary<string, object> filters,
            IEnumerable<string>? selectedFields,
            int? limit,
            int? offset,
            CancellationToken cancellationToken)
        {
            // Build the SELECT clause
            var sql = new StringBuilder();
            
            if (selectedFields != null && selectedFields.Any())
            {
                // Select only specified fields
                sql.Append("SELECT ");
                sql.Append(string.Join(", ", selectedFields.Select(f => $"`{f}`")));
                sql.Append($" FROM `{tableName}`");
            }
            else
            {
                // Select all fields
                sql.Append($"SELECT * FROM `{tableName}`");
            }
            
            // Apply filters as WHERE clauses
            if (filters != null && filters.Count > 0)
            {
                sql.Append(" WHERE ");
                var filterClauses = new List<string>();
                
                foreach (var filter in filters)
                {
                    // Handle null values
                    if (filter.Value == null)
                    {
                        filterClauses.Add($"`{filter.Key}` IS NULL");
                    }
                    else
                    {
                        filterClauses.Add($"`{filter.Key}` = @{filter.Key}");
                    }
                }
                
                sql.Append(string.Join(" AND ", filterClauses));
            }
            
            // Add LIMIT and OFFSET for pagination
            if (limit.HasValue)
            {
                sql.Append($" LIMIT {limit.Value}");
                
                if (offset.HasValue)
                {
                    sql.Append($" OFFSET {offset.Value}");
                }
            }
            
            // Create command with SQL and parameters
            using (var cmd = new MySqlCommand(sql.ToString(), _connection))
            {
                // Add parameters for filters
                if (filters != null)
                {
                    foreach (var filter in filters)
                    {
                        if (filter.Value != null)
                        {
                            cmd.Parameters.AddWithValue($"@{filter.Key}", filter.Value);
                        }
                    }
                }
                
                // Initialize our return collections
                var data = new List<IDictionary<string, object>>();
                List<FieldInfo> fields = new List<FieldInfo>();
                
                // Execute reader and process results
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    // Extract field information from reader schema
                    fields = await GetFieldInfoFromReaderAsync(reader, cancellationToken);
                    
                    // Read and process result rows
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var row = new Dictionary<string, object>();
                        
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string fieldName = reader.GetName(i);
                            object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            
                            // Handle special types (like byte arrays, date/time, etc.)
                            if (value is byte[] bytes)
                            {
                                // Convert binary data to base64 string for easier transport
                                value = Convert.ToBase64String(bytes);
                            }
                            else if (value is DateTime dateTime)
                            {
                                // Ensure consistent DateTime format
                                value = dateTime.ToString("o");
                            }
                            
                            row[fieldName] = value;
                        }
                        
                        data.Add(row);
                        
                        // Report progress every 1000 rows
                        if (data.Count % 1000 == 0)
                        {
                            OnProgressChanged(
                                "ExtractData",
                                data.Count,
                                limit ?? -1,
                                $"Extracted {data.Count} rows from {schema}.{tableName}"
                            );
                        }
                    }
                }
                
                return (data, fields);
            }
        }
        
        /// <summary>
        /// Extract field information from a database reader
        /// </summary>
        private async Task<List<FieldInfo>> GetFieldInfoFromReaderAsync(
            System.Data.Common.DbDataReader reader,
            CancellationToken cancellationToken)
        {
            var fields = new List<FieldInfo>();
            
            // Get schema table with field metadata
            var schemaTable = await reader.GetSchemaTableAsync(cancellationToken);
            
            if (schemaTable != null)
            {
                foreach (DataRow row in schemaTable.Rows)
                {
                    string columnName = row["ColumnName"].ToString();
                    Type columnType = (Type)row["DataType"];
                    bool isNullable = (bool)row["AllowDBNull"];
                    int? maxLength = row["ColumnSize"] == DBNull.Value ? null : Convert.ToInt32(row["ColumnSize"]);
                    int? precision = null;
                    int? scale = null;
                    
                    if (row["NumericPrecision"] != DBNull.Value)
                    {
                        precision = Convert.ToInt32(row["NumericPrecision"]);
                    }
                    
                    if (row["NumericScale"] != DBNull.Value)
                    {
                        scale = Convert.ToInt32(row["NumericScale"]);
                    }
                    
                    bool isPrimaryKey = false;
                    if (row["IsKey"] != DBNull.Value)
                    {
                        isPrimaryKey = (bool)row["IsKey"];
                    }
                    
                    // Map .NET type to standard type string
                    string dataType = MapDotNetTypeToStandard(columnType);
                    
                    // Create metadata
                    var metadata = new Dictionary<string, object>
                    {
                        ["clr_type"] = columnType.FullName,
                        ["ordinal"] = Convert.ToInt32(row["ColumnOrdinal"])
                    };
                    
                    // Add field info to list
                    fields.Add(new FieldInfo(
                        columnName,
                        dataType,
                        isNullable,
                        string.Empty, // No description available from reader schema
                        isPrimaryKey,
                        maxLength,
                        precision,
                        scale,
                        null, // No default value from reader schema
                        metadata
                    ));
                }
            }
            
            return fields;
        }
        
        /// <summary>
        /// Maps a .NET type to a standardized data type string
        /// </summary>
        private string MapDotNetTypeToStandard(Type type)
        {
            if (type == typeof(string) || type == typeof(char) || type == typeof(Guid))
            {
                return "string";
            }
            else if (type == typeof(bool))
            {
                return "boolean";
            }
            else if (type == typeof(byte) || type == typeof(sbyte) || 
                     type == typeof(short) || type == typeof(ushort) ||
                     type == typeof(int) || type == typeof(uint) ||
                     type == typeof(long) || type == typeof(ulong))
            {
                return "integer";
            }
            else if (type == typeof(float) || type == typeof(double))
            {
                return "float";
            }
            else if (type == typeof(decimal))
            {
                return "decimal";
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return "datetime";
            }
            else if (type == typeof(TimeSpan))
            {
                return "time";
            }
            else if (type == typeof(byte[]))
            {
                return "binary";
            }
            else
            {
                return "object";
            }
        }
        
        /// <inheritdoc/>
        public async Task<TransformationResult> TransformDataAsync(IEnumerable<IDictionary<string, object>> data, TransformationParameters transformationParams, CancellationToken cancellationToken = default)
        {
            string operationId = Guid.NewGuid().ToString("N");
            _logger?.LogDebug("[Operation:{OperationId}] Starting data transformation with {RuleCount} rules", 
                operationId, transformationParams?.Rules?.Count ?? 0);
            
            if (_disposed)
            {
                string errorMessage = "Cannot transform data: connector is disposed";
                _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                OnErrorOccurred("TransformData", errorMessage);
                return TransformationResult.Failure(errorMessage, "Connector disposed");
            }
            
            try
            {
                // Validate parameters
                if (data == null)
                {
                    string errorMessage = "Data to transform cannot be null";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("TransformData", errorMessage);
                    return TransformationResult.Failure(errorMessage, "Invalid parameters");
                }
                
                if (transformationParams == null)
                {
                    string errorMessage = "Transformation parameters cannot be null";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("TransformData", errorMessage);
                    return TransformationResult.Failure(errorMessage, "Invalid parameters");
                }
                
                if (transformationParams.Rules == null || !transformationParams.Rules.Any())
                {
                    string errorMessage = "At least one transformation rule must be specified";
                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                    OnErrorOccurred("TransformData", errorMessage);
                    return TransformationResult.Failure(errorMessage, "Invalid parameters");
                }
                
                // Start tracking execution time
                var startTime = DateTime.UtcNow;
                
                // Convert to list for multiple iterations
                var dataList = data.ToList();
                if (!dataList.Any())
                {
                    _logger?.LogWarning("[Operation:{OperationId}] No data provided for transformation", operationId);
                    return TransformationResult.Success(
                        transformedData: new List<IDictionary<string, object>>(),
                        executionTimeMs: 0,
                        successCount: 0,
                        failureCount: 0);
                }
                
                _logger?.LogDebug("[Operation:{OperationId}] Transforming {RecordCount} records with {RuleCount} rules", 
                    operationId, dataList.Count, transformationParams.Rules.Count);
                
                // Create working copy of data for transformation
                List<IDictionary<string, object>> transformedData;
                if (transformationParams.PreserveOriginalData)
                {
                    // Deep clone the data
                    _logger?.LogDebug("[Operation:{OperationId}] Creating deep copy of data for transformation", operationId);
                    transformedData = dataList.Select(row => (IDictionary<string, object>)new Dictionary<string, object>(row)).ToList();
                }
                else
                {
                    // Use original data
                    _logger?.LogDebug("[Operation:{OperationId}] Using original data for in-place transformation", operationId);
                    transformedData = dataList;
                }
                
                int successCount = 0;
                int failureCount = 0;
                List<RuleExecutionResult> ruleResults = new List<RuleExecutionResult>();
                
                // Order rules by the Order property
                var orderedRules = transformationParams.Rules
                    .OrderBy(r => r.Order)
                    .ToList();
                
                _logger?.LogDebug("[Operation:{OperationId}] Rules ordered by priority: {OrderedRules}", 
                    operationId, string.Join(", ", orderedRules.Select(r => r.Id)));
                
                // Apply each rule
                foreach (var rule in orderedRules)
                {
                    _logger?.LogDebug("[Operation:{OperationId}] Applying transformation rule {RuleId} ({RuleType})", 
                        operationId, rule.Id, rule.Type);
                    
                    var ruleStartTime = DateTime.UtcNow;
                    int ruleSuccessCount = 0;
                    int ruleFailureCount = 0;
                    
                    try
                    {
                        // Apply rule based on its type
                        switch (rule.Type.ToLowerInvariant())
                        {
                            case "map":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying map rule {RuleId} with {SourceCount} source fields and {TargetCount} target fields", 
                                    operationId, rule.Id, rule.SourceFields.Count, rule.TargetFields.Count);
                                (ruleSuccessCount, ruleFailureCount) = ApplyMapRule(transformedData, rule);
                                break;
                                
                            case "filter":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying filter rule {RuleId} with condition: '{Condition}'", 
                                    operationId, rule.Id, rule.Condition ?? "none");
                                (transformedData, ruleSuccessCount, ruleFailureCount) = ApplyFilterRule(transformedData, rule);
                                break;
                                
                            case "aggregate":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying aggregate rule {RuleId}", operationId, rule.Id);
                                (transformedData, ruleSuccessCount, ruleFailureCount) = ApplyAggregateRule(transformedData, rule);
                                break;
                                
                            case "join":
                                // Join rule requires external data, retrieve from parameters if available
                                if (rule.Parameters != null && rule.Parameters.TryGetValue("externalData", out var externalDataObj) &&
                                    externalDataObj is IEnumerable<IDictionary<string, object>> externalData)
                                {
                                    _logger?.LogDebug("[Operation:{OperationId}] Applying join rule {RuleId} with {ExternalCount} external records", 
                                        operationId, rule.Id, externalData.Count());
                                    (transformedData, ruleSuccessCount, ruleFailureCount) = ApplyJoinRule(transformedData, externalData, rule);
                                }
                                else
                                {
                                    string errorMessage = $"Join rule {rule.Id} requires 'externalData' parameter";
                                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                                    throw new ArgumentException(errorMessage);
                                }
                                break;
                                
                            case "format":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying format rule {RuleId}", operationId, rule.Id);
                                (ruleSuccessCount, ruleFailureCount) = ApplyFormatRule(transformedData, rule);
                                break;
                                
                            case "add":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying add rule {RuleId} for target field: {TargetField}", 
                                    operationId, rule.Id, rule.TargetFields.FirstOrDefault() ?? "unknown");
                                (ruleSuccessCount, ruleFailureCount) = ApplyAddRule(transformedData, rule);
                                break;
                                
                            case "remove":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying remove rule {RuleId} for {FieldCount} fields", 
                                    operationId, rule.Id, rule.SourceFields.Count);
                                (ruleSuccessCount, ruleFailureCount) = ApplyRemoveRule(transformedData, rule);
                                break;
                                
                            case "rename":
                                _logger?.LogDebug("[Operation:{OperationId}] Applying rename rule {RuleId} for {FieldCount} fields", 
                                    operationId, rule.Id, rule.SourceFields.Count);
                                (ruleSuccessCount, ruleFailureCount) = ApplyRenameRule(transformedData, rule);
                                break;
                                
                            case "custom":
                                if (rule.Expression == null)
                                {
                                    string errorMessage = $"Custom rule {rule.Id} requires an expression";
                                    _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, errorMessage);
                                    throw new ArgumentException(errorMessage);
                                }
                                _logger?.LogDebug("[Operation:{OperationId}] Applying custom rule {RuleId} with expression: {Expression}", 
                                    operationId, rule.Id, rule.Expression);
                                (ruleSuccessCount, ruleFailureCount) = ApplyCustomRule(transformedData, rule);
                                break;
                                
                            default:
                                string unsupportedMessage = $"Transformation rule type '{rule.Type}' is not supported";
                                _logger?.LogError("[Operation:{OperationId}] {Message}", operationId, unsupportedMessage);
                                throw new NotSupportedException(unsupportedMessage);
                        }
                        
                        var ruleExecutionTime = (long)(DateTime.UtcNow - ruleStartTime).TotalMilliseconds;
                        
                        // Record rule result
                        ruleResults.Add(new RuleExecutionResult(
                            rule.Id,
                            true,
                            ruleExecutionTime,
                            transformedData.Count,
                            ruleSuccessCount,
                            ruleFailureCount,
                            null));
                        
                        // Update counts
                        successCount += ruleSuccessCount;
                        failureCount += ruleFailureCount;
                        
                        _logger?.LogDebug("[Operation:{OperationId}] Rule {RuleId} applied successfully in {Time}ms. Success: {Success}, Failure: {Failure}, Records remaining: {Records}",
                            operationId, rule.Id, ruleExecutionTime, ruleSuccessCount, ruleFailureCount, transformedData.Count);
                    }
                    catch (Exception ex)
                    {
                        var ruleExecutionTime = (long)(DateTime.UtcNow - ruleStartTime).TotalMilliseconds;
                        
                        // Record rule failure
                        ruleResults.Add(new RuleExecutionResult(
                            rule.Id,
                            false,
                            ruleExecutionTime,
                            transformedData.Count,
                            ruleSuccessCount,
                            ruleFailureCount, 
                            ex.Message));
                        
                        // Update failure count
                        failureCount += (transformedData.Count - ruleSuccessCount);
                        
                        _logger?.LogError(ex, "[Operation:{OperationId}] Error applying rule {RuleId}: {Message}", 
                            operationId, rule.Id, ex.Message);
                        OnErrorOccurred("TransformData", $"Error applying rule {rule.Id}: {ex.Message}", ex);
                        
                        // If configured to fail on error, exit early
                        if (transformationParams.FailOnError)
                        {
                            var totalExecutionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                            
                            _logger?.LogError("[Operation:{OperationId}] Transformation aborted due to rule failure and FailOnError=true", operationId);
                            
                            return TransformationResult.Failure(
                                errorMessage: $"Transformation rule {rule.Id} failed: {ex.Message}",
                                errorDetails: ex.ToString(),
                                executionTimeMs: totalExecutionTime,
                                successCount: successCount,
                                failureCount: failureCount,
                                ruleResults: ruleResults);
                        }
                        else
                        {
                            _logger?.LogWarning("[Operation:{OperationId}] Continuing transformation despite rule {RuleId} failure (FailOnError=false)", 
                                operationId, rule.Id);
                        }
                    }
                }
                
                // Calculate total execution time
                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger?.LogInformation("[Operation:{OperationId}] Data transformation completed in {Time}ms. Records: {Total}, Success: {Success}, Failure: {Failure}",
                    operationId, executionTime, transformedData.Count, successCount, failureCount);
                
                // Return the transformation result with operation ID in the log context
                return TransformationResult.Success(
                    transformedData,
                    executionTime,
                    successCount,
                    failureCount,
                    ruleResults);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[Operation:{OperationId}] Error during data transformation: {Message}", operationId, ex.Message);
                OnErrorOccurred("TransformData", $"Error during transformation: {ex.Message}", ex);
                
                return TransformationResult.Failure(ex.Message, ex.ToString());
            }
        }
        
        #region Transformation Rule Implementations
        
        private (int SuccessCount, int FailureCount) ApplyMapRule(List<IDictionary<string, object>> data, TransformationRule rule)
        {
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var row in data)
            {
                try
                {
                    // Skip rows that don't match the condition
                    if (!EvaluateCondition(row, rule.Condition))
                    {
                        continue;
                    }
                    
                    // Get source values
                    var sourceValues = rule.SourceFields.Select(field => 
                        row.TryGetValue(field, out var value) ? value : null).ToList();
                    
                    // Apply mapping
                    if (rule.Expression != null)
                    {
                        // Parse expression and apply transformation
                        var result = EvaluateExpression(rule.Expression, sourceValues, rule.Parameters);
                        
                        // Set target field value
                        if (rule.TargetFields.Count > 0)
                        {
                            row[rule.TargetFields[0]] = result;
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    else if (rule.SourceFields.Count == 1 && rule.TargetFields.Count == 1)
                    {
                        // Simple field copy
                        if (row.TryGetValue(rule.SourceFields[0], out var value))
                        {
                            row[rule.TargetFields[0]] = value;
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (successCount, failureCount);
        }
        
        private (List<IDictionary<string, object>> FilteredData, int SuccessCount, int FailureCount) ApplyFilterRule(
            List<IDictionary<string, object>> data, TransformationRule rule)
        {
            var filteredData = new List<IDictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var row in data)
            {
                try
                {
                    bool includeRow = EvaluateCondition(row, rule.Condition);
                    
                    if (includeRow)
                    {
                        filteredData.Add(row);
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (filteredData, successCount, failureCount);
        }
        
        private (List<IDictionary<string, object>> AggregatedData, int SuccessCount, int FailureCount) ApplyAggregateRule(
            List<IDictionary<string, object>> data, TransformationRule rule)
        {
            if (rule.Parameters == null || !rule.Parameters.TryGetValue("groupByFields", out var groupByFieldsObj) ||
                !(groupByFieldsObj is IEnumerable<string> groupByFields))
            {
                throw new ArgumentException("Aggregate rule requires 'groupByFields' parameter");
            }
            
            string aggregateFunction = rule.Parameters.TryGetValue("function", out var funcObj) && funcObj is string func
                ? func.ToLowerInvariant()
                : "sum";
            
            var groupedData = new Dictionary<string, List<IDictionary<string, object>>>();
            
            // Group data by specified fields
            foreach (var row in data)
            {
                var key = string.Join("|", groupByFields.Select(field => 
                    row.TryGetValue(field, out var value) ? value?.ToString() ?? "null" : "null"));
                
                if (!groupedData.ContainsKey(key))
                {
                    groupedData[key] = new List<IDictionary<string, object>>();
                }
                
                groupedData[key].Add(row);
            }
            
            List<IDictionary<string, object>> result = new List<IDictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            // Apply aggregation to each group
            foreach (var group in groupedData)
            {
                try
                {
                    IDictionary<string, object> aggregatedRow = new Dictionary<string, object>();
                    
                    // Preserve group by fields
                    var firstRow = group.Value.First();
                    foreach (var field in groupByFields)
                    {
                        if (firstRow.TryGetValue(field, out var value))
                        {
                            aggregatedRow[field] = value;
                        }
                    }
                    
                    // Apply aggregation to each source field
                    foreach (var field in rule.SourceFields)
                    {
                        var targetField = rule.TargetFields.FirstOrDefault() ?? field;
                        
                        switch (aggregateFunction)
                        {
                            case "sum":
                                aggregatedRow[targetField] = group.Value.Sum(row => 
                                    row.TryGetValue(field, out var value) && value is IConvertible convertible ?
                                    Convert.ToDouble(convertible) : 0);
                                break;
                                
                            case "avg":
                                aggregatedRow[targetField] = group.Value.Average(row => 
                                    row.TryGetValue(field, out var value) && value is IConvertible convertible ?
                                    Convert.ToDouble(convertible) : 0);
                                break;
                                
                            case "min":
                                aggregatedRow[targetField] = group.Value.Min(row => 
                                    row.TryGetValue(field, out var value) && value is IConvertible convertible ?
                                    Convert.ToDouble(convertible) : 0);
                                break;
                                
                            case "max":
                                aggregatedRow[targetField] = group.Value.Max(row => 
                                    row.TryGetValue(field, out var value) && value is IConvertible convertible ?
                                    Convert.ToDouble(convertible) : 0);
                                break;
                                
                            case "count":
                                aggregatedRow[targetField] = group.Value.Count;
                                break;
                                
                            default:
                                throw new NotSupportedException($"Aggregate function '{aggregateFunction}' is not supported");
                        }
                    }
                    
                    result.Add(aggregatedRow);
                    successCount++;
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (result, successCount, failureCount);
        }
        
        private (List<IDictionary<string, object>> JoinedData, int SuccessCount, int FailureCount) ApplyJoinRule(
            List<IDictionary<string, object>> leftData, 
            IEnumerable<IDictionary<string, object>> rightData,
            TransformationRule rule)
        {
            // Simple implementation for time constraints
            var joinType = rule.Parameters?.TryGetValue("joinType", out var joinTypeObj) == true && joinTypeObj is string jType
                ? jType.ToLowerInvariant()
                : "inner";
                
            var result = new List<IDictionary<string, object>>();
            int successCount = 0;
            int failureCount = 0;
            
            // For simplicity, we're just implementing inner join based on the source fields
            foreach (var leftRow in leftData)
            {
                try
                {
                    var leftKey = string.Join("|", rule.SourceFields.Select(field => 
                        leftRow.TryGetValue(field, out var value) ? value?.ToString() ?? "null" : "null"));
                        
                    var matches = rightData.Where(rightRow => 
                    {
                        var rightKey = string.Join("|", rule.SourceFields.Select(field => 
                            rightRow.TryGetValue(field, out var value) ? value?.ToString() ?? "null" : "null"));
                        return leftKey == rightKey;
                    }).ToList();
                    
                    if (matches.Any())
                    {
                        foreach (var rightRow in matches)
                        {
                            var joinedRow = new Dictionary<string, object>(leftRow);
                            
                            // Add right data with target field prefixes if specified
                            foreach (var pair in rightRow)
                            {
                                var targetField = pair.Key;
                                if (rule.TargetFields.Count > 0)
                                {
                                    targetField = $"{rule.TargetFields[0]}_{pair.Key}";
                                }
                                
                                joinedRow[targetField] = pair.Value;
                            }
                            
                            result.Add(joinedRow);
                            successCount++;
                        }
                    }
                    else if (joinType == "left")
                    {
                        // Add left row for left join
                        result.Add(new Dictionary<string, object>(leftRow));
                        successCount++;
                    }
                    else
                    {
                        // No match for inner join
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (result, successCount, failureCount);
        }
        
        private (int SuccessCount, int FailureCount) ApplyFormatRule(List<IDictionary<string, object>> data, TransformationRule rule)
        {
            string format = rule.Parameters?.TryGetValue("format", out var formatObj) == true && formatObj is string fmt
                ? fmt
                : "{0}";
                
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var row in data)
            {
                try
                {
                    // Skip rows that don't match the condition
                    if (!EvaluateCondition(row, rule.Condition))
                    {
                        continue;
                    }
                    
                    // Get source values for formatting
                    var sourceValues = rule.SourceFields.Select(field => 
                        row.TryGetValue(field, out var value) ? value : null).ToArray();
                    
                    // Apply formatting
                    if (rule.TargetFields.Count > 0)
                    {
                        row[rule.TargetFields[0]] = string.Format(format, sourceValues);
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (successCount, failureCount);
        }
        
        private (int SuccessCount, int FailureCount) ApplyAddRule(List<IDictionary<string, object>> data, TransformationRule rule)
        {
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var row in data)
            {
                try
                {
                    // Skip rows that don't match the condition
                    if (!EvaluateCondition(row, rule.Condition))
                    {
                        continue;
                    }
                    
                    // Add new field with constant or expression value
                    if (rule.TargetFields.Count > 0)
                    {
                        object value;
                        
                        if (rule.Expression != null)
                        {
                            // Evaluate expression for the value
                            value = EvaluateExpression(rule.Expression, new List<object>(), rule.Parameters);
                        }
                        else if (rule.Parameters != null && rule.Parameters.TryGetValue("value", out var valueObj))
                        {
                            // Use constant value
                            value = valueObj;
                        }
                        else
                        {
                            // Default
                            value = null;
                        }
                        
                        row[rule.TargetFields[0]] = value;
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (successCount, failureCount);
        }
        
        private (int SuccessCount, int FailureCount) ApplyRemoveRule(List<IDictionary<string, object>> data, TransformationRule rule)
        {
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var row in data)
            {
                try
                {
                    // Skip rows that don't match the condition
                    if (!EvaluateCondition(row, rule.Condition))
                    {
                        continue;
                    }
                    
                    // Remove source fields
                    bool anyRemoved = false;
                    foreach (var field in rule.SourceFields)
                    {
                        if (row.Remove(field))
                        {
                            anyRemoved = true;
                        }
                    }
                    
                    if (anyRemoved)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (successCount, failureCount);
        }
        
        private (int SuccessCount, int FailureCount) ApplyRenameRule(List<IDictionary<string, object>> data, TransformationRule rule)
        {
            int successCount = 0;
            int failureCount = 0;
            
            // Check if source and target fields have matching counts
            if (rule.SourceFields.Count != rule.TargetFields.Count)
            {
                throw new ArgumentException("Rename rule requires matching counts of source and target fields");
            }
            
            foreach (var row in data)
            {
                try
                {
                    // Skip rows that don't match the condition
                    if (!EvaluateCondition(row, rule.Condition))
                    {
                        continue;
                    }
                    
                    bool anyRenamed = false;
                    
                    // Rename fields
                    for (int i = 0; i < rule.SourceFields.Count; i++)
                    {
                        string sourceField = rule.SourceFields[i];
                        string targetField = rule.TargetFields[i];
                        
                        if (row.TryGetValue(sourceField, out var value))
                        {
                            row[targetField] = value;
                            row.Remove(sourceField);
                            anyRenamed = true;
                        }
                    }
                    
                    if (anyRenamed)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (successCount, failureCount);
        }
        
        private (int SuccessCount, int FailureCount) ApplyCustomRule(List<IDictionary<string, object>> data, TransformationRule rule)
        {
            // This is a simplified placeholder for custom rule execution
            // In a real implementation, this might use a scripting engine
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var row in data)
            {
                try
                {
                    // Skip rows that don't match the condition
                    if (!EvaluateCondition(row, rule.Condition))
                    {
                        continue;
                    }
                    
                    // For demonstration, we'll handle very simple expressions
                    if (rule.Expression.StartsWith("concat"))
                    {
                        var values = rule.SourceFields.Select(field => 
                            row.TryGetValue(field, out var value) ? value?.ToString() : "").ToList();
                        
                        string result = string.Join("", values);
                        
                        if (rule.TargetFields.Count > 0)
                        {
                            row[rule.TargetFields[0]] = result;
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    else
                    {
                        // Unsupported expression
                        failureCount++;
                    }
                }
                catch
                {
                    failureCount++;
                }
            }
            
            return (successCount, failureCount);
        }
        
        private bool EvaluateCondition(IDictionary<string, object> row, string? condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true; // No condition means always apply
            }
            
            // Simplified condition evaluation - in a real implementation, this would use a more robust expression evaluator
            try
            {
                // Handle simple field existence check
                if (condition.StartsWith("exists:"))
                {
                    string fieldName = condition.Substring(7).Trim();
                    return row.ContainsKey(fieldName) && row[fieldName] != null;
                }
                
                // Handle simple field equality check
                if (condition.Contains("=="))
                {
                    var parts = condition.Split(new[] { "==" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string fieldName = parts[0].Trim();
                        string expectedValue = parts[1].Trim('"', '\'', ' ');
                        
                        return row.TryGetValue(fieldName, out var value) &&
                               value?.ToString() == expectedValue;
                    }
                }
                
                // Fallback for unsupported conditions
                return true;
            }
            catch
            {
                // If condition evaluation fails, default to false
                return false;
            }
        }
        
        private object EvaluateExpression(string expression, IEnumerable<object?> sourceValues, IDictionary<string, object>? parameters)
        {
            // Simplified expression evaluation - in a real implementation, this would use a more robust expression evaluator
            try
            {
                var values = sourceValues.ToList();
                
                // Handle simple arithmetic operations
                if (expression == "sum" && values.Count > 0)
                {
                    double sum = 0;
                    foreach (var value in values)
                    {
                        if (value is IConvertible convertible)
                        {
                            sum += Convert.ToDouble(convertible);
                        }
                    }
                    return sum;
                }
                
                if (expression == "multiply" && values.Count > 0)
                {
                    double product = 1;
                    foreach (var value in values)
                    {
                        if (value is IConvertible convertible)
                        {
                            product *= Convert.ToDouble(convertible);
                        }
                    }
                    return product;
                }
                
                // Handle string operations
                if (expression == "uppercase" && values.Count > 0)
                {
                    return values.First()?.ToString()?.ToUpperInvariant() ?? string.Empty;
                }
                
                if (expression == "lowercase" && values.Count > 0)
                {
                    return values.First()?.ToString()?.ToLowerInvariant() ?? string.Empty;
                }
                
                // Return first value as fallback
                return values.FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                // If expression evaluation fails, return empty string
                return string.Empty;
            }
        }
        
        #endregion
        
        /// <inheritdoc/>
        public async Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Disposing MySQL connector resources");
            
            if (_disposed)
            {
                _logger?.LogDebug("MySQL connector already disposed");
                return;
            }
            
            try
            {
                // Ensure we're disconnected
                if (ConnectionState != SmartInsight.Core.Interfaces.ConnectionState.Disconnected)
                {
                    await DisconnectAsync(cancellationToken);
                }
                
                // Dispose any other resources as needed
                _connectionLock?.Dispose();
                
                _disposed = true;
                _logger?.LogInformation("MySQL connector resources disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing MySQL connector resources: {Message}", ex.Message);
                OnErrorOccurred("Dispose", $"Error disposing resources: {ex.Message}", ex);
                throw;
            }
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                try
                {
                    // Release managed resources
                    _connection?.Dispose();
                    _connection = null;
                    _connectionLock?.Dispose();
                    
                    _logger?.LogDebug("MySQL connector disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during MySQL connector disposal: {Message}", ex.Message);
                    // We can't use OnErrorOccurred here as the object might be partially disposed
                }
            }
            
            _disposed = true;
        }
    }
} 