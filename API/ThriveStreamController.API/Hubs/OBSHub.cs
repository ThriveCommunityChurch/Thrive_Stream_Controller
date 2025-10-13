using Microsoft.AspNetCore.SignalR;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;
using ThriveStreamController.Core.Services;

namespace ThriveStreamController.API.Hubs;

/// <summary>
/// SignalR hub for real-time OBS communication
/// </summary>
public class OBSHub : Hub
{
    private readonly ILogger<OBSHub> _logger;
    private readonly IOBSService _obsService;
    private readonly MediaStatusTracker _mediaStatusTracker;

    /// <summary>
    /// Constructor for OBSHub
    /// </summary>
    public OBSHub(ILogger<OBSHub> logger, IOBSService obsService, MediaStatusTracker mediaStatusTracker)
    {
        _logger = logger;
        _obsService = obsService;
        _mediaStatusTracker = mediaStatusTracker;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to OBS Hub: {ConnectionId}", Context.ConnectionId);
        
        // Send current OBS status to the newly connected client
        var status = _obsService.GetConnectionStatus();
        await Clients.Caller.SendAsync("ConnectionStatusChanged", status);
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from OBS Hub: {ConnectionId}", Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets all current media status for all scenes
    /// </summary>
    public async Task<Dictionary<string, SceneMediaStatus>> GetAllMediaStatus()
    {
        _logger.LogDebug("Client {ConnectionId} requested all media status", Context.ConnectionId);
        return await Task.FromResult(_mediaStatusTracker.GetAllMediaStatus());
    }
}

