namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents the connection status of the OBS WebSocket connection.
    /// </summary>
    public class OBSConnectionStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the connection to OBS is established.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the OBS WebSocket server address.
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last error message if connection failed.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last connection attempt.
        /// </summary>
        public DateTime? LastConnectionAttempt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the connection was established.
        /// </summary>
        public DateTime? ConnectedAt { get; set; }
    }
}

