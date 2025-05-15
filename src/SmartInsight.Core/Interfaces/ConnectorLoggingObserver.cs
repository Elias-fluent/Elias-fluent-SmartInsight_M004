using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Observer that logs connector lifecycle events
/// </summary>
public class ConnectorLoggingObserver : IConnectorLifecycleObserver
{
    private readonly ILogger<ConnectorLoggingObserver> _logger;
    
    /// <summary>
    /// Creates a new connector logging observer
    /// </summary>
    /// <param name="logger">Logger to use</param>
    public ConnectorLoggingObserver(ILogger<ConnectorLoggingObserver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Called when a connector is registered
    /// </summary>
    public Task OnConnectorRegisteredAsync(IDataSourceConnector connector, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connector registered: {ConnectorId} ({Name}, {Type})", 
            connector.Id, connector.Name, connector.SourceType);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Called when a connector is unregistered
    /// </summary>
    public Task OnConnectorUnregisteredAsync(string connectorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connector unregistered: {ConnectorId}", connectorId);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Called when a connector's state changes
    /// </summary>
    public Task OnConnectorStateChangedAsync(ConnectorStateChangedEventArgs args, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connector state changed: {ConnectorId} - {PreviousState} -> {CurrentState}", 
            args.ConnectorId, args.PreviousState, args.CurrentState);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Called when a connector encounters an error
    /// </summary>
    public Task OnConnectorErrorAsync(ConnectorErrorEventArgs args, CancellationToken cancellationToken = default)
    {
        _logger.LogError(args.Exception, "Connector error: {ConnectorId} - {Operation} - {Message}", 
            args.ConnectorId, args.Operation, args.Message);
            
        if (!string.IsNullOrWhiteSpace(args.Details))
        {
            _logger.LogDebug("Error details: {Details}", args.Details);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Called when a connector reports progress
    /// </summary>
    public Task OnConnectorProgressAsync(ConnectorProgressEventArgs args, CancellationToken cancellationToken = default)
    {
        double progressPercentage = args.ProgressPercentage;
        
        if (progressPercentage % 10 == 0 || progressPercentage >= 100) // Only log every 10% to reduce noise
        {
            _logger.LogInformation("Operation progress: {OperationId} - {ProgressPercentage}% ({Current}/{Total}) {Message}", 
                args.OperationId, progressPercentage, args.Current, args.Total, args.Message);
        }
        else
        {
            _logger.LogDebug("Operation progress: {OperationId} - {ProgressPercentage}% ({Current}/{Total}) {Message}", 
                args.OperationId, progressPercentage, args.Current, args.Total, args.Message);
        }
        
        return Task.CompletedTask;
    }
} 