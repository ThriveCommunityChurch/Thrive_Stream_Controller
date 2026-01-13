namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Information about a YouTube channel.
    /// </summary>
    public class YouTubeChannelInfo
    {
        /// <summary>
        /// Gets or sets the channel ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the channel title/name.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the channel description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the custom URL for the channel.
        /// </summary>
        public string? CustomUrl { get; set; }

        /// <summary>
        /// Gets or sets the channel thumbnail URL.
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Gets or sets the subscriber count.
        /// </summary>
        public long? SubscriberCount { get; set; }

        /// <summary>
        /// Gets or sets the video count.
        /// </summary>
        public long? VideoCount { get; set; }

        /// <summary>
        /// Gets or sets the view count.
        /// </summary>
        public long? ViewCount { get; set; }

        /// <summary>
        /// Gets or sets whether subscriber count is hidden.
        /// </summary>
        public bool HiddenSubscriberCount { get; set; }

        /// <summary>
        /// Gets or sets the date the channel was published.
        /// </summary>
        public DateTime? PublishedAt { get; set; }
    }
}

