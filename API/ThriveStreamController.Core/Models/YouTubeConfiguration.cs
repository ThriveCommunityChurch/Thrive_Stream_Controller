namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Configuration settings for YouTube Live Streaming API integration.
    /// </summary>
    public class YouTubeConfiguration
    {
        /// <summary>
        /// Gets or sets the OAuth 2.0 Client ID from Google Cloud Console.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OAuth 2.0 Client Secret from Google Cloud Console.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OAuth 2.0 Refresh Token for accessing the YouTube API.
        /// This is obtained during the initial OAuth authorization flow.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the persistent broadcast ID that will be reused for all streams.
        /// If not set, a new broadcast will be created on first use.
        /// </summary>
        public string? PersistentBroadcastId { get; set; }

        /// <summary>
        /// Gets or sets the persistent stream ID that will be reused for all streams.
        /// If not set, a new stream will be created on first use.
        /// </summary>
        public string? PersistentStreamId { get; set; }

        /// <summary>
        /// Gets or sets the YouTube Channel ID.
        /// </summary>
        public string? ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the redirect URI for OAuth callback.
        /// Default: http://localhost:5080/api/auth/youtube/callback
        /// </summary>
        public string RedirectUri { get; set; } = "http://localhost:5080/api/auth/youtube/callback";

        /// <summary>
        /// Gets or sets the OAuth scopes required for YouTube Live Streaming.
        /// </summary>
        public string[] Scopes { get; set; } = new[]
        {
            "https://www.googleapis.com/auth/youtube",
            "https://www.googleapis.com/auth/youtube.force-ssl"
        };

        /// <summary>
        /// Validates that the configuration has the minimum required settings.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ClientId) &&
                   !string.IsNullOrWhiteSpace(ClientSecret);
        }

        /// <summary>
        /// Checks if OAuth is configured (has refresh token).
        /// </summary>
        /// <returns>True if OAuth is configured, false otherwise.</returns>
        public bool IsOAuthConfigured()
        {
            return IsValid() && !string.IsNullOrWhiteSpace(RefreshToken);
        }
    }
}

