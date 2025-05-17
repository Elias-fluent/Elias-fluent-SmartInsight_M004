using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using SmartInsight.Core.Interfaces;
using System.Linq;
using System.Text;

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
            _logger?.LogDebug("Initializing MySQL connector");
            
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
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e => $"{e.FieldName}: {e.ErrorMessage}"));
                    _logger?.LogError("Configuration validation failed: {Errors}", errorMessages);
                    OnErrorOccurred("Initialize", $"Configuration validation failed: {errorMessages}");
                    return false;
                }
                
                _logger?.LogInformation("MySQL connector initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing MySQL connector");
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
            
            // If using user/password authentication (not integrated security), 
            // both user and password must be provided
            if (!connectionParams.TryGetValue("username", out var username) || string.IsNullOrWhiteSpace(username))
            {
                result.AddError("username", "Username is required");
            }
            
            if (!connectionParams.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password))
            {
                result.AddError("password", "Password is required");
            }
            
            // Validate port if specified
            if (connectionParams.TryGetValue("port", out var portStr) && !string.IsNullOrWhiteSpace(portStr))
            {
                if (!int.TryParse(portStr, out var port) || port < 1 || port > 65535)
                {
                    result.AddError("port", "Port must be a valid number between 1 and 65535");
                }
            }
            
            // Validate connection timeout if specified
            if (connectionParams.TryGetValue("connectionTimeout", out var connectionTimeoutStr) && 
                !string.IsNullOrWhiteSpace(connectionTimeoutStr))
            {
                if (!int.TryParse(connectionTimeoutStr, out var connectionTimeout) || connectionTimeout < 0)
                {
                    result.AddError("connectionTimeout", "Connection timeout must be a positive number");
                }
            }
            
            // Validate command timeout if specified
            if (connectionParams.TryGetValue("commandTimeout", out var commandTimeoutStr) && 
                !string.IsNullOrWhiteSpace(commandTimeoutStr))
            {
                if (!int.TryParse(commandTimeoutStr, out var commandTimeout) || commandTimeout < 0)
                {
                    result.AddError("commandTimeout", "Command timeout must be a positive number");
                }
            }
            
            // Validate SSL mode if specified
            if (connectionParams.TryGetValue("sslMode", out var sslMode) && !string.IsNullOrWhiteSpace(sslMode))
            {
                var validSslModes = new[] { "none", "preferred", "required", "verifyca", "verifyfull" };
                if (!validSslModes.Contains(sslMode.ToLowerInvariant()))
                {
                    result.AddError("sslMode", $"SSL mode must be one of: {string.Join(", ", validSslModes)}");
                }
            }
            
            return Task.FromResult(result);
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
        public Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
        {
            // To be implemented in the next subtask
            throw new NotImplementedException("Connect method not yet implemented");
        }
        
        /// <inheritdoc/>
        public Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
        {
            // To be implemented in the next subtask
            throw new NotImplementedException("TestConnection method not yet implemented");
        }
        
        /// <inheritdoc/>
        public Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(IDictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
        {
            // To be implemented in a later subtask
            throw new NotImplementedException("DiscoverDataStructures method not yet implemented");
        }
        
        /// <inheritdoc/>
        public Task<ExtractionResult> ExtractDataAsync(ExtractionParameters extractionParams, CancellationToken cancellationToken = default)
        {
            // To be implemented in a later subtask
            throw new NotImplementedException("ExtractData method not yet implemented");
        }
        
        /// <inheritdoc/>
        public Task<TransformationResult> TransformDataAsync(IEnumerable<IDictionary<string, object>> data, TransformationParameters transformationParams, CancellationToken cancellationToken = default)
        {
            // To be implemented in a later subtask
            throw new NotImplementedException("TransformData method not yet implemented");
        }
        
        /// <inheritdoc/>
        public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            // To be implemented in a later subtask
            throw new NotImplementedException("Disconnect method not yet implemented");
        }
        
        /// <inheritdoc/>
        public Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            // To be implemented in a later subtask
            throw new NotImplementedException("DisposeAsync method not yet implemented");
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
                // Release managed resources
                _connection?.Dispose();
                _connection = null;
                _connectionLock.Dispose();
            }
            
            _disposed = true;
        }
    }
} 