namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents the status of a media input in OBS.
    /// </summary>
    public class MediaInputStatus
    {
        /// <summary>
        /// Gets or sets the state of the media input.
        /// </summary>
        public string MediaState { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total duration of the media in milliseconds.
        /// Null if not playing.
        /// </summary>
        public long? MediaDuration { get; set; }

        /// <summary>
        /// Gets or sets the current cursor position in milliseconds.
        /// Null if not playing.
        /// </summary>
        public long? MediaCursor { get; set; }

        /// <summary>
        /// Gets the remaining time in milliseconds.
        /// Null if not playing or duration is unknown.
        /// </summary>
        public long? RemainingTime
        {
            get
            {
                if (MediaDuration.HasValue && MediaCursor.HasValue)
                {
                    return MediaDuration.Value - MediaCursor.Value;
                }
                return null;
            }
        }
    }
}

