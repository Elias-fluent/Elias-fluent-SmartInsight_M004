# Serilog Logging Implementation Guide

## Overview

This document provides a comprehensive overview of the Serilog logging implementation in the SmartInsight application. Serilog is a structured logging framework that enables rich, contextual logging with configurable sinks and enrichers. 

## Features

- **Structured Logging**: Log events are treated as structured data, not just strings, enabling better filtering, searching, and analysis.
- **Configurable Outputs**: Logs can be sent to multiple destinations (Console, File, Seq) concurrently.
- **Environment-Aware**: Different logging configurations for Development, Staging, and Production environments.
- **Contextual Information**: Logs automatically include machine name, thread ID, and other contextual data.
- **Request Logging**: HTTP requests are automatically logged with performance metrics.
- **Centralized Configuration**: Logging settings are defined in `appsettings.json`.

## Architecture

The implementation consists of the following components:

1. **SerilogOptions**: Configuration options class
2. **SerilogConfiguration**: Core service for creating and configuring Serilog
3. **SerilogWebHostExtensions**: Extension methods for configuring Web Host Builders
4. **SerilogServiceExtensions**: Extensions for integrating Serilog with ASP.NET Core services
5. **TelemetryServiceRegistration**: Registration of all telemetry services including Serilog

## Configuration

### appsettings.json

Serilog is configured via the `Serilog` section in `appsettings.json`:

```json
"Serilog": {
  "UseSerilog": true,
  "MinimumLevel": "Information",
  "OverrideMinimumLevel": {
    "Microsoft": "Warning",
    "Microsoft.AspNetCore": "Warning",
    "System": "Warning"
  },
  "WriteToConsole": true,
  "UseConsoleInDevelopment": true,
  "WriteToFile": true,
  "FilePath": "logs/smartinsight-.log",
  "RollingInterval": 1,
  "FileSizeLimitMB": 10,
  "RetainedFileCount": 31,
  "WriteToSeq": false,
  "SeqServerUrl": "http://localhost:5341",
  "EnrichWithMachineName": true,
  "EnrichWithEnvironment": true,
  "EnrichWithThreadId": true,
  "EnrichWithContext": true
}
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| UseSerilog | Whether to use Serilog for logging | `true` |
| MinimumLevel | Base minimum log level | `"Information"` |
| OverrideMinimumLevel | Override log levels for specific namespaces | See example above |
| WriteToConsole | Whether to log to the console | `true` |
| UseConsoleInDevelopment | Always log to console in Development | `true` |
| WriteToFile | Whether to log to a file | `true` |
| FilePath | Path to the log file | `"logs/smartinsight-.log"` |
| RollingInterval | When to roll log files (0=Infinite, 1=Day, 2=Hour, 3=Minute) | `1` (Day) |
| FileSizeLimitMB | Maximum log file size | `10` MB |
| RetainedFileCount | Number of log files to keep | `31` |
| WriteToSeq | Whether to log to a Seq server | `false` |
| SeqServerUrl | URL of the Seq server | `"http://localhost:5341"` |
| EnrichWithMachineName | Add machine name to logs | `true` |
| EnrichWithEnvironment | Add environment info to logs | `true` |
| EnrichWithThreadId | Add thread ID to logs | `true` |
| EnrichWithContext | Add context info to logs | `true` |

## Usage

### Application Startup

Serilog is integrated in the `Program.cs` files of web applications:

```csharp
// 1. Configure Serilog for the host builder
builder.Host.UseSerilogConfig();

// 2. Add telemetry services (includes Serilog)
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment.EnvironmentName);

var app = builder.Build();

// 3. Add request logging middleware
app.UseSerilogRequestLogging();

// 4. Configure Serilog with application services
app.UseSerilogConfiguration(app.Configuration, app.Environment);
```

### Using the Logger

Inject `ILogger<T>` into your services:

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoSomething(string userId)
    {
        _logger.LogInformation("Operation started for user {UserId}", userId);
        
        try
        {
            // Do work
            _logger.LogDebug("Operation details: {Details}", new { /* properties */ });
            _logger.LogInformation("Operation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during operation for user {UserId}", userId);
            throw;
        }
    }
}
```

### Structured Logging Best Practices

1. **Use named parameters instead of string interpolation**:
   ```csharp
   // Good
   _logger.LogInformation("Processing order {OrderId}", orderId);
   
   // Bad
   _logger.LogInformation($"Processing order {orderId}");
   ```

2. **Include contextual information**:
   ```csharp
   _logger.LogInformation("User {UserId} made payment of {Amount} using {PaymentMethod}", 
       userId, amount, paymentMethod);
   ```

3. **Use appropriate log levels**:
   - `Verbose`: Detailed debugging information
   - `Debug`: Information useful for debugging
   - `Information`: General information about application flow
   - `Warning`: Non-critical issues that don't stop the application
   - `Error`: Errors that prevent a function from working
   - `Fatal`: Critical errors that cause the application to crash

4. **Add context to logs**:
   ```csharp
   using (LogContext.PushProperty("OrderId", orderId))
   {
       // All logs within this scope will include OrderId
       _logger.LogInformation("Starting order processing");
       // ...
   }
   ```

## Monitoring and Management

### Log Files

Log files are stored in the `logs` directory by default, with a naming pattern specified by `FilePath` in configuration.

### Seq Integration

If enabled, logs can be viewed in a Seq server for better visualization and querying:

1. Set `WriteToSeq` to `true` in configuration
2. Ensure the Seq server is running at the URL specified in `SeqServerUrl`
3. View logs in the Seq web interface

## Extending the Logging System

### Adding Custom Enrichers

To add custom properties to all logs:

```csharp
loggerConfig = loggerConfig.Enrich.WithProperty("Application", "SmartInsight");
```

### Adding Custom Sinks

To add a custom log destination:

```csharp
// Add to SerilogConfiguration.CreateLogger
if (options.WriteToCustomSink)
{
    loggerConfig = loggerConfig.WriteTo.CustomSink(options.CustomSinkSettings);
}
```

## References

- [Serilog Documentation](https://serilog.net/)
- [Serilog.AspNetCore Documentation](https://github.com/serilog/serilog-aspnetcore)
- [Structured Logging Concepts](https://nblumhardt.com/2016/06/structured-logging-concepts-in-net-series-1/) 