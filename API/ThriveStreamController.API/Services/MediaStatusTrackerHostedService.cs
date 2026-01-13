using ThriveStreamController.Core.Services;

namespace ThriveStreamController.API.Services
{
    /// <summary>
    /// Hosted service wrapper for MediaStatusTracker.
    /// </summary>
    public class MediaStatusTrackerHostedService : IHostedService
    {
        private readonly MediaStatusTracker _mediaStatusTracker;
        private readonly MediaStatusBroadcaster _mediaStatusBroadcaster;

        public MediaStatusTrackerHostedService(
            MediaStatusTracker mediaStatusTracker,
            MediaStatusBroadcaster mediaStatusBroadcaster)
        {
            _mediaStatusTracker = mediaStatusTracker;
            _mediaStatusBroadcaster = mediaStatusBroadcaster;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mediaStatusTracker.Start();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mediaStatusTracker.StopAsync();
        }
    }
}

