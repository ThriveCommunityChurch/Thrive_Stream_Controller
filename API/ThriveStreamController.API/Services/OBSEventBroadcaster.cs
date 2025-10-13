using Microsoft.AspNetCore.SignalR;
using ThriveStreamController.API.Hubs;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;

namespace ThriveStreamController.API.Services;

/// <summary>
/// Background service that broadcasts OBS events to SignalR clients
/// </summary>
public class OBSEventBroadcaster : IHostedService
{
    private readonly ILogger<OBSEventBroadcaster> _logger;
    private readonly IOBSService _obsService;
    private readonly IHubContext<OBSHub> _hubContext;

    /// <summary>
    /// Constructor for OBSEventBroadcaster
    /// </summary>
    public OBSEventBroadcaster(
        ILogger<OBSEventBroadcaster> logger,
        IOBSService obsService,
        IHubContext<OBSHub> hubContext)
    {
        _logger = logger;
        _obsService = obsService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Start the service and subscribe to OBS events
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OBS Event Broadcaster starting");

        // Subscribe to OBS events
        _obsService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _obsService.SceneChanged += OnSceneChanged;
        _obsService.StreamingStatusChanged += OnStreamingStatusChanged;

        _logger.LogInformation("OBS Event Broadcaster started");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the service and unsubscribe from OBS events
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OBS Event Broadcaster stopping");

        // Unsubscribe from OBS events
        _obsService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _obsService.SceneChanged -= OnSceneChanged;
        _obsService.StreamingStatusChanged -= OnStreamingStatusChanged;

        _logger.LogInformation("OBS Event Broadcaster stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle OBS connection status changes
    /// </summary>
    private async void OnConnectionStatusChanged(object? sender, OBSConnectionStatus status)
    {
        try
        {
            _logger.LogInformation("Broadcasting connection status change: {IsConnected}", status.IsConnected);
            await _hubContext.Clients.All.SendAsync("ConnectionStatusChanged", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting connection status change");
        }
    }

    /// <summary>
    /// Handle OBS scene changes
    /// </summary>
    private async void OnSceneChanged(object? sender, string sceneName)
    {
        try
        {
            _logger.LogInformation("Broadcasting scene change: {SceneName}", sceneName);
            await _hubContext.Clients.All.SendAsync("SceneChanged", sceneName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting scene change");
        }
    }

    /// <summary>
    /// Handle OBS streaming status changes
    /// </summary>
    private async void OnStreamingStatusChanged(object? sender, StreamingStatus status)
    {
        try
        {
            _logger.LogInformation("Broadcasting streaming status change: {IsStreaming}", status.IsStreaming);
            await _hubContext.Clients.All.SendAsync("StreamingStatusChanged", status.IsStreaming);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting streaming status change");
        }
    }
}

