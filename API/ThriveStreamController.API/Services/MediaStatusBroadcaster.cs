using Microsoft.AspNetCore.SignalR;
using ThriveStreamController.API.Hubs;
using ThriveStreamController.Core.Models;
using ThriveStreamController.Core.Services;

namespace ThriveStreamController.API.Services
{
    /// <summary>
    /// Service that broadcasts media status updates to SignalR clients.
    /// </summary>
    public class MediaStatusBroadcaster
    {
        private readonly ILogger<MediaStatusBroadcaster> _logger;
        private readonly IHubContext<OBSHub> _hubContext;
        private readonly MediaStatusTracker _mediaStatusTracker;

        public MediaStatusBroadcaster(
            ILogger<MediaStatusBroadcaster> logger,
            IHubContext<OBSHub> hubContext,
            MediaStatusTracker mediaStatusTracker)
        {
            _logger = logger;
            _hubContext = hubContext;
            _mediaStatusTracker = mediaStatusTracker;

            // Subscribe to media status changes
            _mediaStatusTracker.MediaStatusChanged += OnMediaStatusChanged;
        }

        private async void OnMediaStatusChanged(object? sender, SceneMediaStatus sceneMediaStatus)
        {
            try
            {
                _logger.LogDebug("Broadcasting media status update for scene: {SceneName}", sceneMediaStatus.SceneName);
                
                // Broadcast to all connected clients
                await _hubContext.Clients.All.SendAsync("MediaStatusChanged", sceneMediaStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting media status change");
            }
        }
    }
}

