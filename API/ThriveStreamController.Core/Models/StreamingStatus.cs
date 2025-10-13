namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents the current streaming status from OBS.
    /// </summary>
    public class StreamingStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether OBS is currently streaming.
        /// </summary>
        public bool IsStreaming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether OBS is currently recording.
        /// </summary>
        public bool IsRecording { get; set; }

        /// <summary>
        /// Gets or sets the name of the currently active scene.
        /// </summary>
        public string? CurrentScene { get; set; }

        /// <summary>
        /// Gets or sets the duration of the current stream in seconds.
        /// </summary>
        public int StreamDurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the stream started.
        /// </summary>
        public DateTime? StreamStartTime { get; set; }
    }
}

