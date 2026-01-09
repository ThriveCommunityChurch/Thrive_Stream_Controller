using ThriveStreamController.Core.Models;

namespace ThriveStreamController.Core.Interfaces
{
    /// <summary>
    /// Service for managing YouTube Live broadcasts.
    /// Handles the full broadcast lifecycle: create, bind stream, go live, end.
    /// </summary>
    public interface IYouTubeLiveService
    {
        /// <summary>
        /// Creates a new YouTube Live broadcast with the specified settings.
        /// Uses a persistent stream so OBS/Castr configuration never changes.
        /// </summary>
        /// <param name="title">The broadcast title.</param>
        /// <param name="description">The broadcast description.</param>
        /// <param name="scheduledStartTime">Optional scheduled start time. Defaults to now.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Information about the created broadcast.</returns>
        Task<YouTubeBroadcastInfo> CreateBroadcastAsync(
            string title,
            string? description = null,
            DateTime? scheduledStartTime = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets or creates a persistent stream that can be reused across broadcasts.
        /// This ensures the stream key and RTMP URL never change.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The persistent stream information including stream key and RTMP URL.</returns>
        Task<YouTubeStreamInfo> GetOrCreatePersistentStreamAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Binds a broadcast to the persistent stream.
        /// Must be called before transitioning to live.
        /// </summary>
        /// <param name="broadcastId">The broadcast ID to bind.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if binding was successful.</returns>
        Task<bool> BindBroadcastToStreamAsync(string broadcastId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the broadcast to live status.
        /// Should only be called after OBS has started streaming.
        /// </summary>
        /// <param name="broadcastId">The broadcast ID to transition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated broadcast information.</returns>
        Task<YouTubeBroadcastInfo> TransitionToLiveAsync(string broadcastId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the broadcast to testing status.
        /// Used when the stream is receiving video but not yet public.
        /// </summary>
        /// <param name="broadcastId">The broadcast ID to transition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated broadcast information.</returns>
        Task<YouTubeBroadcastInfo> TransitionToTestingAsync(string broadcastId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ends the broadcast and marks it as complete.
        /// </summary>
        /// <param name="broadcastId">The broadcast ID to end.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated broadcast information.</returns>
        Task<YouTubeBroadcastInfo> EndBroadcastAsync(string broadcastId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of a broadcast.
        /// </summary>
        /// <param name="broadcastId">The broadcast ID to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Current broadcast information.</returns>
        Task<YouTubeBroadcastInfo> GetBroadcastStatusAsync(string broadcastId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the thumbnail for a broadcast.
        /// </summary>
        /// <param name="broadcastId">The broadcast ID.</param>
        /// <param name="thumbnailPath">Path to the thumbnail image file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if successful.</returns>
        Task<bool> SetThumbnailAsync(string broadcastId, string thumbnailPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates broadcast metadata (title and/or description).
        /// </summary>
        /// <param name="broadcastId">The broadcast ID to update.</param>
        /// <param name="title">New title (optional).</param>
        /// <param name="description">New description (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated broadcast information.</returns>
        Task<YouTubeBroadcastInfo> UpdateBroadcastAsync(
            string broadcastId,
            string? title = null,
            string? description = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the currently active broadcast, if any.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Active broadcast info or null.</returns>
        Task<YouTubeBroadcastInfo?> GetActiveBroadcastAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets broadcast defaults/template from the most recent broadcast.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Default settings from the most recent broadcast.</returns>
        Task<YouTubeBroadcastDefaults?> GetBroadcastDefaultsAsync(CancellationToken cancellationToken = default);
    }
}

