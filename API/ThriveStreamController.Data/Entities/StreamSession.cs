using System;

namespace ThriveStreamController.Data.Entities
{
    /// <summary>
    /// Represents a streaming session with start/end times and status information.
    /// </summary>
    public class StreamSession
    {
        /// <summary>
        /// Gets or sets the unique identifier for the stream session.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the stream session started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the stream session ended.
        /// Can be null if the stream is still active.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the current status of the stream session.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the scene that was active when the stream started.
        /// </summary>
        public string? SceneName { get; set; }

        /// <summary>
        /// Gets or sets additional notes or metadata about the stream session.
        /// </summary>
        public string? Notes { get; set; }

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

