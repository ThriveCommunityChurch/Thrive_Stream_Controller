namespace ThriveStreamController.Data.Entities
{
    /// <summary>
    /// Represents persistent stream configuration for a platform (broadcast IDs, stream keys, RTMP URLs).
    /// These are reused across multiple streaming sessions.
    /// </summary>
    public class PersistentStreamConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier for the stream configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the platform name (YouTube, Facebook).
        /// </summary>
        public string Platform { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the broadcast/video ID from the platform.
        /// For YouTube: liveBroadcast ID
        /// For Facebook: live_video ID
        /// </summary>
        public string? BroadcastId { get; set; }

        /// <summary>
        /// Gets or sets the stream ID (YouTube only).
        /// For YouTube: liveStream ID
        /// </summary>
        public string? StreamId { get; set; }

        /// <summary>
        /// Gets or sets the persistent stream key.
        /// </summary>
        public string? StreamKey { get; set; }

        /// <summary>
        /// Gets or sets the RTMP URL for streaming.
        /// </summary>
        public string? RtmpUrl { get; set; }

        /// <summary>
        /// Gets or sets whether this configuration is currently active.
        /// Only one configuration per platform should be active at a time.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets additional configuration data as JSON.
        /// </summary>
        public string? ConfigurationJson { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}

