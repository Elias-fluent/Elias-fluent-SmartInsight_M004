using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using iTextSharp.text.pdf;
using System.Linq;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// File Repository connector implementation
    /// </summary>
    [ConnectorMetadata(
        id: "file-repository-connector", 
        name: "File Repository Connector", 
        sourceType: "FileRepository",
        Description = "Connector for file repositories and document stores.",
        Version = "1.0.0",
        Author = "SmartInsight Team",
        Capabilities = new[] { "read", "extract", "transform" },
        DocumentationUrl = "https://docs.example.com/connectors/file-repository")]
    [ConnectorCategory("FileSystem")]
    [ConnectorCategory("Documents")]
    [ConnectionParameter(
        name: "rootPath",
        displayName: "Root Path",
        description: "Root directory path for the file repository",
        type: "string",
        IsRequired = true)]
    [ConnectionParameter(
        name: "includeSubDirectories",
        displayName: "Include Subdirectories",
        description: "Whether to include files in subdirectories",
        type: "boolean",
        IsRequired = false,
        DefaultValue = "true")]
    [ConnectionParameter(
        name: "fileExtensions",
        displayName: "File Extensions",
        description: "Comma-separated list of file extensions to include (e.g., pdf,docx,txt)",
        type: "string",
        IsRequired = false)]
    [ConnectionParameter(
        name: "maxFileSizeMB",
        displayName: "Max File Size (MB)",
        description: "Maximum file size in megabytes",
        type: "integer",
        IsRequired = false,
        DefaultValue = "10")]
    public class FileRepositoryConnector : IDataSourceConnector, IDisposable
    {
        private readonly ILogger<FileRepositoryConnector>? _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private IConnectorConfiguration? _configuration;
        private bool _disposed;
        private string? _connectionId;
        private string? _rootPath;
        private bool _includeSubDirectories;
        private string[]? _fileExtensions;
        private int _maxFileSizeMB;
        
        /// <summary>
        /// Creates a new instance of the file repository connector
        /// </summary>
        public FileRepositoryConnector() 
        {
            // Default constructor for use when logger is not available
        }
        
        /// <summary>
        /// Creates a new instance of the file repository connector with logging
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public FileRepositoryConnector(ILogger<FileRepositoryConnector> logger)
        {
            _logger = logger;
        }
        
        /// <inheritdoc/>
        public string Id => "file-repository-connector";
        
        /// <inheritdoc/>
        public string Name => "File Repository Connector";
        
        /// <inheritdoc/>
        public string SourceType => "FileRepository";
        
        /// <inheritdoc/>
        public string Description => "Connector for file repositories and document stores.";
        
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
            _logger?.LogDebug("Initializing file repository connector");
            
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
                
                // Extract and store configuration parameters
                if (configuration.ConnectionParameters.TryGetValue("rootPath", out var rootPath))
                {
                    _rootPath = rootPath;
                }
                
                if (configuration.ConnectionParameters.TryGetValue("includeSubDirectories", out var includeSubDirs) &&
                    bool.TryParse(includeSubDirs, out var parsedIncludeSubDirs))
                {
                    _includeSubDirectories = parsedIncludeSubDirs;
                }
                else
                {
                    _includeSubDirectories = true; // Default value
                }
                
                if (configuration.ConnectionParameters.TryGetValue("fileExtensions", out var fileExtensions) &&
                    !string.IsNullOrWhiteSpace(fileExtensions))
                {
                    _fileExtensions = fileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                
                if (configuration.ConnectionParameters.TryGetValue("maxFileSizeMB", out var maxFileSizeMB) &&
                    int.TryParse(maxFileSizeMB, out var parsedMaxFileSizeMB))
                {
                    _maxFileSizeMB = parsedMaxFileSizeMB;
                }
                else
                {
                    _maxFileSizeMB = 10; // Default value 10MB
                }
                
                _logger?.LogInformation("File repository connector initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing file repository connector");
                OnErrorOccurred("Initialize", $"Error initializing connector: {ex.Message}", ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams)
        {
            _logger?.LogDebug("Validating connection parameters for file repository connector");
            
            List<ValidationError> errors = new List<ValidationError>();
            List<string> warnings = new List<string>();
            
            // Validate required parameters
            if (!connectionParams.TryGetValue("rootPath", out var rootPath) || string.IsNullOrWhiteSpace(rootPath))
            {
                errors.Add(new ValidationError("rootPath", "Root path is required"));
            }
            else
            {
                // Validate that root path exists and is accessible
                if (!Directory.Exists(rootPath))
                {
                    errors.Add(new ValidationError("rootPath", $"Root path '{rootPath}' does not exist or is not accessible"));
                }
                else
                {
                    try
                    {
                        // Test directory access
                        Directory.GetFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        errors.Add(new ValidationError("rootPath", $"Access to directory '{rootPath}' is denied"));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ValidationError("rootPath", $"Error accessing directory '{rootPath}': {ex.Message}"));
                    }
                }
            }
            
            // Validate file extensions (optional)
            if (connectionParams.TryGetValue("fileExtensions", out var fileExtensions) &&
                !string.IsNullOrWhiteSpace(fileExtensions))
            {
                var extensions = fileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                foreach (var extension in extensions)
                {
                    if (!extension.StartsWith("."))
                    {
                        warnings.Add($"File extension '{extension}' should start with a dot (.)");
                    }
                }
            }
            
            // Validate maxFileSizeMB (optional)
            if (connectionParams.TryGetValue("maxFileSizeMB", out var maxFileSizeMB))
            {
                if (!int.TryParse(maxFileSizeMB, out var parsedMaxFileSizeMB))
                {
                    errors.Add(new ValidationError("maxFileSizeMB", "Max file size must be a valid integer"));
                }
                else if (parsedMaxFileSizeMB <= 0)
                {
                    errors.Add(new ValidationError("maxFileSizeMB", "Max file size must be greater than 0"));
                }
                else if (parsedMaxFileSizeMB > 100)
                {
                    warnings.Add("Max file size is very large (> 100MB), which may cause performance issues");
                }
            }
            
            if (errors.Count > 0)
            {
                _logger?.LogWarning("Connection validation failed with {ErrorCount} errors", errors.Count);
                return Task.FromResult(ValidationResult.Failure(errors, warnings));
            }
            
            return Task.FromResult(ValidationResult.Success(warnings));
        }

        /// <inheritdoc/>
        public async Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default)
        {
            try
            {
                await _connectionLock.WaitAsync(cancellationToken);
                
                try
                {
                    _logger?.LogInformation("Connecting to file repository: {Path}", 
                        connectionParams.ContainsKey("rootPath") ? connectionParams["rootPath"] : "unknown");
                    
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
                    
                    // Store connection parameters
                    _rootPath = connectionParams["rootPath"];
                    
                    if (connectionParams.TryGetValue("includeSubDirectories", out var includeSubDirs) &&
                        bool.TryParse(includeSubDirs, out var parsedIncludeSubDirs))
                    {
                        _includeSubDirectories = parsedIncludeSubDirs;
                    }
                    else
                    {
                        _includeSubDirectories = true; // Default value
                    }
                    
                    if (connectionParams.TryGetValue("fileExtensions", out var fileExtensions) &&
                        !string.IsNullOrWhiteSpace(fileExtensions))
                    {
                        _fileExtensions = fileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    }
                    
                    if (connectionParams.TryGetValue("maxFileSizeMB", out var maxFileSizeMB) &&
                        int.TryParse(maxFileSizeMB, out var parsedMaxFileSizeMB))
                    {
                        _maxFileSizeMB = parsedMaxFileSizeMB;
                    }
                    else
                    {
                        _maxFileSizeMB = 10; // Default value 10MB
                    }
                    
                    // Generate a unique connection ID
                    _connectionId = $"file-repo-{Guid.NewGuid():N}";
                    
                    // Update state
                    ConnectionState = ConnectionState.Connected;
                    OnStateChanged(ConnectionState.Connecting, ConnectionState);
                    
                    _logger?.LogInformation("Successfully connected to file repository: {ConnectionId}, Path: {Path}", 
                        _connectionId, _rootPath);
                    
                    // Return connection result with additional info
                    return ConnectionResult.Success(_connectionId!);
                }
                finally
                {
                    _connectionLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error connecting to file repository");
                ConnectionState = ConnectionState.Error;
                OnStateChanged(ConnectionState.Connecting, ConnectionState);
                OnErrorOccurred("Connect", ex.Message, ex);
                
                return ConnectionResult.Failure(ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams)
        {
            _logger?.LogDebug("Testing connection to file repository");
            
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

        /// <summary>
        /// Validates the configuration
        /// </summary>
        private Task<ValidationResult> ValidateConfigurationAsync(IConnectorConfiguration configuration)
        {
            return ValidateConnectionAsync(configuration.ConnectionParameters);
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
        public Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(IDictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Discovering data structures in file repository");
            
            if (ConnectionState != ConnectionState.Connected)
            {
                _logger?.LogError("Cannot discover data structures: Not connected to file repository");
                throw new InvalidOperationException("Not connected to file repository");
            }
            
            if (string.IsNullOrEmpty(_rootPath) || !Directory.Exists(_rootPath))
            {
                _logger?.LogError("Root path is invalid or not accessible");
                throw new InvalidOperationException("Root path is invalid or not accessible");
            }
            
            try
            {
                List<DataStructureInfo> structures = new List<DataStructureInfo>();
                
                // Get file extension filter
                string? extensionFilter = null;
                if (filter != null && filter.TryGetValue("extension", out var extValue) && extValue != null)
                {
                    extensionFilter = extValue.ToString();
                }
                
                // Define search option based on includeSubDirectories flag
                var searchOption = _includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                // Get files
                string[] files;
                if (_fileExtensions != null && _fileExtensions.Length > 0)
                {
                    // Get files with specified extensions
                    var allFiles = new List<string>();
                    foreach (var ext in _fileExtensions)
                    {
                        var pattern = $"*{(ext.StartsWith(".") ? ext : $".{ext}")}";
                        allFiles.AddRange(Directory.GetFiles(_rootPath, pattern, searchOption));
                    }
                    files = allFiles.ToArray();
                }
                else
                {
                    // Get all files
                    files = Directory.GetFiles(_rootPath, "*.*", searchOption);
                }
                
                // Filter files by extension if specified in filter
                if (!string.IsNullOrEmpty(extensionFilter))
                {
                    files = files.Where(f => Path.GetExtension(f).Equals(extensionFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                
                // Filter by max file size
                if (_maxFileSizeMB > 0)
                {
                    var maxBytes = _maxFileSizeMB * 1024 * 1024;
                    files = files.Where(f => new FileInfo(f).Length <= maxBytes).ToArray();
                }
                
                // Group files by type and create data structures
                var fileGroups = files.GroupBy(f => Path.GetExtension(f).ToLowerInvariant());
                
                foreach (var group in fileGroups)
                {
                    var extension = group.Key;
                    var fileType = GetFileType(extension);
                    var fileCategory = GetFileCategory(extension);
                    
                    // Create fields for this file type
                    List<FieldInfo> fields = new List<FieldInfo>
                    {
                        new FieldInfo("path", "string", false, "Full file path", true),
                        new FieldInfo("filename", "string", false, "File name", false),
                        new FieldInfo("size", "long", true, "File size in bytes", false),
                        new FieldInfo("created", "datetime", true, "Creation date", false),
                        new FieldInfo("modified", "datetime", true, "Last modified date", false),
                        new FieldInfo("content", "string", true, "File content", false),
                        new FieldInfo("extension", "string", true, "File extension", false)
                    };
                    
                    // Add specific fields based on file type
                    if (fileType == "Document")
                    {
                        fields.Add(new FieldInfo("title", "string", true, "Document title", false));
                        fields.Add(new FieldInfo("author", "string", true, "Document author", false));
                        fields.Add(new FieldInfo("pages", "integer", true, "Number of pages", false));
                    }
                    
                    // Create data structure for this file type
                    var structureName = $"{fileType.ToLowerInvariant()}_files";
                    var structure = new DataStructureInfo(
                        name: structureName,
                        type: "file",
                        fields: fields,
                        description: $"{fileType} files with {extension} extension");
                    
                    structures.Add(structure);
                }
                
                _logger?.LogInformation("Discovered {Count} data structures in file repository", structures.Count);
                return Task.FromResult<IEnumerable<DataStructureInfo>>(structures);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error discovering data structures in file repository");
                throw;
            }
        }

        /// <summary>
        /// Determines the type of file based on its extension
        /// </summary>
        private string GetFileType(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                    return "PDF";
                case ".docx":
                case ".doc":
                case ".rtf":
                    return "Document";
                case ".txt":
                case ".md":
                case ".csv":
                    return "Text";
                case ".html":
                case ".htm":
                    return "HTML";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return "Image";
                case ".xlsx":
                case ".xls":
                    return "Spreadsheet";
                case ".pptx":
                case ".ppt":
                    return "Presentation";
                case ".xml":
                case ".json":
                    return "Data";
                default:
                    return "Other";
            }
        }

        /// <summary>
        /// Determines the category of file based on its extension
        /// </summary>
        private string GetFileCategory(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                case ".docx":
                case ".doc":
                case ".rtf":
                case ".txt":
                case ".md":
                    return "Document";
                case ".html":
                case ".htm":
                case ".xml":
                case ".json":
                    return "Markup";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return "Image";
                case ".xlsx":
                case ".xls":
                case ".csv":
                    return "Data";
                case ".pptx":
                case ".ppt":
                    return "Presentation";
                default:
                    return "Other";
            }
        }

        /// <inheritdoc/>
        public async Task<ExtractionResult> ExtractDataAsync(ExtractionParameters extractionParams, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Extracting data from file repository");
            
            if (ConnectionState != ConnectionState.Connected)
            {
                _logger?.LogError("Cannot extract data: Not connected to file repository");
                throw new InvalidOperationException("Not connected to file repository");
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
                
                // Find target file extension from structure name
                string? extension = null;
                string fileType = "file";
                
                // Extract file type from structure name (e.g., "document_files", "pdf_files")
                var parts = structureName.Split('_');
                if (parts.Length > 0)
                {
                    fileType = parts[0];
                    
                    // Determine extension from file type
                    switch (fileType.ToLowerInvariant())
                    {
                        case "pdf":
                            extension = ".pdf";
                            break;
                        case "document":
                            extension = ".docx";
                            break;
                        case "text":
                            extension = ".txt";
                            break;
                        case "html":
                            extension = ".html";
                            break;
                        case "image":
                            extension = ".jpg";
                            break;
                        case "spreadsheet":
                            extension = ".xlsx";
                            break;
                        case "presentation":
                            extension = ".pptx";
                            break;
                        case "data":
                            extension = ".json";
                            break;
                    }
                }
                
                // If extension is specified in filter criteria, use that instead
                if (extractionParams.FilterCriteria != null && 
                    extractionParams.FilterCriteria.TryGetValue("extension", out var extValue) && 
                    extValue != null)
                {
                    extension = extValue.ToString();
                    if (!extension.StartsWith("."))
                    {
                        extension = "." + extension;
                    }
                }
                
                // Define search option based on includeSubDirectories flag
                var searchOption = _includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                // Get files
                string[] files;
                if (!string.IsNullOrEmpty(extension))
                {
                    // Get files with specified extension
                    var pattern = $"*{extension}";
                    files = Directory.GetFiles(_rootPath, pattern, searchOption);
                }
                else if (_fileExtensions != null && _fileExtensions.Length > 0)
                {
                    // Get files with specified extensions
                    var allFiles = new List<string>();
                    foreach (var ext in _fileExtensions)
                    {
                        var pattern = $"*{(ext.StartsWith(".") ? ext : $".{ext}")}";
                        allFiles.AddRange(Directory.GetFiles(_rootPath, pattern, searchOption));
                    }
                    files = allFiles.ToArray();
                }
                else
                {
                    // Get all files
                    files = Directory.GetFiles(_rootPath, "*.*", searchOption);
                }
                
                // Apply max file size filter
                if (_maxFileSizeMB > 0)
                {
                    var maxBytes = _maxFileSizeMB * 1024 * 1024;
                    files = files.Where(f => new FileInfo(f).Length <= maxBytes).ToArray();
                }
                
                // Apply custom filters if specified
                if (extractionParams.FilterCriteria != null)
                {
                    // Filter by path or filename
                    if (extractionParams.FilterCriteria.TryGetValue("path", out var pathValue) && pathValue != null)
                    {
                        string pathFilter = pathValue.ToString();
                        files = files.Where(f => f.Contains(pathFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }
                    
                    if (extractionParams.FilterCriteria.TryGetValue("filename", out var filenameValue) && filenameValue != null)
                    {
                        string filenameFilter = filenameValue.ToString();
                        files = files.Where(f => Path.GetFileName(f).Contains(filenameFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
                    }
                    
                    // Filter by date
                    if (extractionParams.FilterCriteria.TryGetValue("modifiedAfter", out var modifiedAfterValue) && 
                        modifiedAfterValue != null && DateTime.TryParse(modifiedAfterValue.ToString(), out var modifiedAfter))
                    {
                        files = files.Where(f => File.GetLastWriteTime(f) >= modifiedAfter).ToArray();
                    }
                    
                    if (extractionParams.FilterCriteria.TryGetValue("modifiedBefore", out var modifiedBeforeValue) && 
                        modifiedBeforeValue != null && DateTime.TryParse(modifiedBeforeValue.ToString(), out var modifiedBefore))
                    {
                        files = files.Where(f => File.GetLastWriteTime(f) <= modifiedBefore).ToArray();
                    }
                    
                    // Filter by size
                    if (extractionParams.FilterCriteria.TryGetValue("minSize", out var minSizeValue) && 
                        minSizeValue != null && long.TryParse(minSizeValue.ToString(), out var minSize))
                    {
                        files = files.Where(f => new FileInfo(f).Length >= minSize).ToArray();
                    }
                    
                    if (extractionParams.FilterCriteria.TryGetValue("maxSize", out var maxSizeValue) && 
                        maxSizeValue != null && long.TryParse(maxSizeValue.ToString(), out var maxSize))
                    {
                        files = files.Where(f => new FileInfo(f).Length <= maxSize).ToArray();
                    }
                }
                
                // Apply pagination
                int totalCount = files.Length;
                if (extractionParams.MaxRecords > 0)
                {
                    files = files.Take(extractionParams.MaxRecords).ToArray();
                }
                
                // Process files and extract data
                int processedCount = 0;
                foreach (var filePath in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var fileName = Path.GetFileName(filePath);
                        var fileExt = Path.GetExtension(filePath).ToLowerInvariant();
                        
                        // Create a dictionary with basic file properties
                        var fileData = new Dictionary<string, object>
                        {
                            ["path"] = filePath,
                            ["filename"] = fileName,
                            ["size"] = fileInfo.Length,
                            ["created"] = fileInfo.CreationTime,
                            ["modified"] = fileInfo.LastWriteTime,
                            ["extension"] = fileExt
                        };
                        
                        // Extract content based on file type
                        if (extractionParams.Options != null && 
                            extractionParams.Options.TryGetValue("includeContent", out var includeContentValue) && 
                            includeContentValue is bool includeContent && includeContent)
                        {
                            try
                            {
                                string content = ExtractFileContent(filePath, fileExt);
                                fileData["content"] = content;
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Error extracting content from file {FilePath}", filePath);
                                fileData["content"] = $"Error extracting content: {ex.Message}";
                            }
                        }
                        
                        // Extract document-specific metadata for document types
                        if (fileExt == ".pdf" || fileExt == ".docx" || fileExt == ".doc")
                        {
                            try
                            {
                                var metadata = ExtractDocumentMetadata(filePath, fileExt);
                                foreach (var kvp in metadata)
                                {
                                    fileData[kvp.Key] = kvp.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Error extracting metadata from file {FilePath}", filePath);
                            }
                        }
                        
                        data.Add(fileData);
                        
                        // Report progress
                        processedCount++;
                        if (processedCount % 10 == 0 || processedCount == files.Length)
                        {
                            OnProgressChanged("extract", processedCount, totalCount, $"Processed {processedCount} of {totalCount} files");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error processing file {FilePath}", filePath);
                    }
                }
                
                var endTime = DateTime.UtcNow;
                var executionTime = (long)(endTime - startTime).TotalMilliseconds;
                
                _logger?.LogInformation("Extracted data from {Count} files in {Time}ms", data.Count, executionTime);
                
                // Create a data structure info for the extraction
                var structureInfo = new DataStructureInfo(
                    name: structureName,
                    type: "file",
                    fields: new List<FieldInfo>
                    {
                        new FieldInfo("path", "string", false, "Full file path", true),
                        new FieldInfo("filename", "string", false, "File name", false),
                        new FieldInfo("size", "long", true, "File size in bytes", false),
                        new FieldInfo("created", "datetime", true, "Creation date", false),
                        new FieldInfo("modified", "datetime", true, "Last modified date", false),
                        new FieldInfo("content", "string", true, "File content", false),
                        new FieldInfo("extension", "string", true, "File extension", false)
                    },
                    description: $"Files with {extension} extension");
                
                return ExtractionResult.Success(data, data.Count, executionTime, structureInfo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error extracting data from file repository");
                return ExtractionResult.Failure($"Error extracting data: {ex.Message}", ex.ToString());
            }
        }

        /// <summary>
        /// Extracts content from a file based on its type
        /// </summary>
        private string ExtractFileContent(string filePath, string fileExtension)
        {
            switch (fileExtension.ToLowerInvariant())
            {
                case ".txt":
                case ".md":
                case ".csv":
                case ".json":
                case ".xml":
                case ".html":
                case ".htm":
                    // Text files - read directly
                    return File.ReadAllText(filePath);
                    
                case ".pdf":
                    // PDF files - extract text using iTextSharp
                    return ExtractPdfText(filePath);
                    
                case ".docx":
                case ".doc":
                    // Word documents - extract text using OpenXML
                    return ExtractDocxText(filePath);
                    
                default:
                    // Other file types - return file info as content
                    var info = new FileInfo(filePath);
                    return $"[{fileExtension.TrimStart('.')} file: {info.Name}, Size: {info.Length} bytes, Last Modified: {info.LastWriteTime}]";
            }
        }

        /// <summary>
        /// Extracts text from a PDF file
        /// </summary>
        private string ExtractPdfText(string filePath)
        {
            try
            {
                using var reader = new PdfReader(filePath);
                var text = new System.Text.StringBuilder();
                
                // Simple metadata extraction
                text.AppendLine($"PDF Document: {Path.GetFileName(filePath)}");
                text.AppendLine($"Pages: {reader.NumberOfPages}");
                
                // Basic extraction of PDF information
                if (reader.Info != null && reader.Info.Count > 0)
                {
                    text.AppendLine("Document Information:");
                    foreach (var key in reader.Info.Keys)
                    {
                        text.AppendLine($"  {key}: {reader.Info[key]}");
                    }
                }
                
                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error extracting text from PDF {FilePath}", filePath);
                return $"[Error extracting PDF text: {ex.Message}]";
            }
        }

        /// <summary>
        /// Extracts text from a DOCX file
        /// </summary>
        private string ExtractDocxText(string filePath)
        {
            try
            {
                if (string.Compare(Path.GetExtension(filePath), ".docx", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return $"[Unsupported document format: {Path.GetExtension(filePath)}]";
                }
                
                using var document = WordprocessingDocument.Open(filePath, false);
                if (document.MainDocumentPart?.Document.Body == null)
                {
                    return "[Document has no content]";
                }
                
                return document.MainDocumentPart.Document.Body.InnerText;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error extracting text from DOCX {FilePath}", filePath);
                return $"[Error extracting DOCX text: {ex.Message}]";
            }
        }

        /// <summary>
        /// Extracts metadata from a document file
        /// </summary>
        private Dictionary<string, object> ExtractDocumentMetadata(string filePath, string fileExtension)
        {
            var metadata = new Dictionary<string, object>();
            
            switch (fileExtension.ToLowerInvariant())
            {
                case ".pdf":
                    try
                    {
                        using var reader = new PdfReader(filePath);
                        var info = reader.Info;
                        
                        if (info.ContainsKey("Title"))
                            metadata["title"] = info["Title"];
                            
                        if (info.ContainsKey("Author"))
                            metadata["author"] = info["Author"];
                            
                        metadata["pages"] = reader.NumberOfPages;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error extracting PDF metadata from {FilePath}", filePath);
                    }
                    break;
                    
                case ".docx":
                    try
                    {
                        using var document = WordprocessingDocument.Open(filePath, false);
                        var props = document.PackageProperties;
                        
                        if (!string.IsNullOrEmpty(props.Title))
                            metadata["title"] = props.Title;
                            
                        if (!string.IsNullOrEmpty(props.Creator))
                            metadata["author"] = props.Creator;
                            
                        var body = document.MainDocumentPart?.Document.Body;
                        if (body != null)
                        {
                            int paragraphCount = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Count();
                            metadata["paragraphs"] = paragraphCount;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error extracting DOCX metadata from {FilePath}", filePath);
                    }
                    break;
            }
            
            return metadata;
        }

        /// <inheritdoc/>
        public Task<TransformationResult> TransformDataAsync(IEnumerable<IDictionary<string, object>> data, TransformationParameters transformationParams, CancellationToken cancellationToken = default)
        {
            try
            {
                // Basic implementation - return data as is
                var transformedData = data.ToList();
                
                // Return successful result
                return Task.FromResult(TransformationResult.Success(transformedData, 0));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error transforming file data");
                return Task.FromResult(TransformationResult.Failure($"Error transforming data: {ex.Message}"));
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
                    // Cleanup any resources
                    // Nothing to do for file repository
                    
                    ConnectionState = ConnectionState.Disconnected;
                    OnStateChanged(ConnectionState.Disconnecting, ConnectionState);
                    
                    _logger?.LogInformation("Disconnected from file repository");
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disconnecting from file repository");
                    OnErrorOccurred("Disconnect", ex.Message, ex);
                    
                    ConnectionState = ConnectionState.Error;
                    OnStateChanged(ConnectionState.Disconnecting, ConnectionState);
                    
                    return false;
                }
            }
            
            return false;
        }

        /// <inheritdoc/>
        public Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            // Ensure disconnected
            if (ConnectionState != ConnectionState.Disconnected)
            {
                DisconnectAsync(cancellationToken).Wait(cancellationToken);
            }
            
            // Cleanup any resources
            _configuration = null;
            _connectionId = null;
            _rootPath = null;
            _fileExtensions = null;
            
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public ConnectorCapabilities GetCapabilities()
        {
            return new ConnectorCapabilities(
                supportsIncremental: false,
                supportsSchemaDiscovery: true,
                supportsAdvancedFiltering: true,
                supportsPreview: true,
                maxConcurrentExtractions: 2,
                supportedAuthentications: new[] { "none" },
                supportedSourceTypes: new[] { "file", "document" });
        }

        /// <inheritdoc/>
        public IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Description"] = Description,
                ["Version"] = Version,
                ["Author"] = "SmartInsight Team",
                ["Documentation"] = "https://docs.example.com/connectors/file-repository",
                ["SupportedFileExtensions"] = new[] { ".pdf", ".docx", ".doc", ".txt", ".md", ".csv", ".json", ".xml", ".html", ".htm" },
                ["SupportedFileTypes"] = new[] { "Document", "PDF", "Text", "HTML", "Data" }
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
            }
            
            _disposed = true;
        }
    }
} 