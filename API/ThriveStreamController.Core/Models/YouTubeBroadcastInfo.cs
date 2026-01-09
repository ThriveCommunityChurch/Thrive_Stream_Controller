namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents information about a YouTube Live broadcast.
    /// </summary>
    public class YouTubeBroadcastInfo
    {
        /// <summary>
        /// Gets or sets the broadcast ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the broadcast title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the broadcast description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the scheduled start time.
        /// </summary>
        public DateTime? ScheduledStartTime { get; set; }

        /// <summary>
        /// Gets or sets the actual start time when the broadcast went live.
        /// </summary>
        public DateTime? ActualStartTime { get; set; }

        /// <summary>
        /// Gets or sets the actual end time when the broadcast ended.
        /// </summary>
        public DateTime? ActualEndTime { get; set; }

        /// <summary>
        /// Gets or sets the broadcast lifecycle status.
        /// Values: created, ready, testing, live, complete, revoked
        /// </summary>
        public string LifecycleStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the privacy status.
        /// Values: public, private, unlisted
        /// </summary>
        public string PrivacyStatus { get; set; } = "public";

        /// <summary>
        /// Gets or sets the stream ID this broadcast is bound to.
        /// </summary>
        public string? BoundStreamId { get; set; }

        /// <summary>
        /// Gets or sets the URL to watch the broadcast.
        /// </summary>
        public string? WatchUrl { get; set; }

        /// <summary>
        /// Gets or sets the embed HTML for the broadcast.
        /// </summary>
        public string? EmbedHtml { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail URL.
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Gets or sets whether this broadcast is currently live.
        /// </summary>
        public bool IsLive => LifecycleStatus == "live";

        /// <summary>
        /// Gets or sets whether this broadcast has ended.
        /// </summary>
        public bool IsComplete => LifecycleStatus == "complete";

        /// <summary>
        /// Gets or sets any error message associated with the broadcast.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents information about a YouTube Live stream (ingestion point).
    /// </summary>
    public class YouTubeStreamInfo
    {
        /// <summary>
        /// Gets or sets the stream ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stream title/name.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stream key for RTMP ingestion.
        /// </summary>
        public string StreamKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the RTMP ingestion URL.
        /// </summary>
        public string RtmpUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full ingestion address (RTMP URL + stream key).
        /// </summary>
        public string IngestionAddress => $"{RtmpUrl}/{StreamKey}";

        /// <summary>
        /// Gets or sets the stream health status.
        /// Values: good, ok, bad, noData
        /// </summary>
        public string? HealthStatus { get; set; }

        /// <summary>
        /// Gets or sets whether the stream is receiving video.
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents default settings from a previous broadcast (template).
    /// </summary>
    public class YouTubeBroadcastDefaults
    {
        /// <summary>
        /// Gets or sets the default title template.
        /// </summary>
        public string? TitleTemplate { get; set; }

        /// <summary>
        /// Gets or sets the default description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default privacy status.
        /// </summary>
        public string PrivacyStatus { get; set; } = "public";

        /// <summary>
        /// Gets or sets the default thumbnail path.
        /// </summary>
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// Gets or sets the ID of the broadcast these defaults came from.
        /// </summary>
        public string? SourceBroadcastId { get; set; }
    }
}

