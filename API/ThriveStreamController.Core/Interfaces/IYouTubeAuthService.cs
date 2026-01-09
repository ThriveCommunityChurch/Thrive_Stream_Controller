using ThriveStreamController.Core.Models;

namespace ThriveStreamController.Core.Interfaces
{
    /// <summary>
    /// Service for handling YouTube OAuth 2.0 authentication flow.
    /// </summary>
    public interface IYouTubeAuthService
    {
        /// <summary>
        /// Generates the OAuth authorization URL for the user to visit.
        /// </summary>
        /// <param name="state">Optional state parameter for CSRF protection.</param>
        /// <returns>The authorization URL.</returns>
        string GetAuthorizationUrl(string? state = null);

        /// <summary>
        /// Exchanges an authorization code for access and refresh tokens.
        /// </summary>
        /// <param name="authorizationCode">The authorization code from the OAuth callback.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> ExchangeAuthorizationCodeAsync(string authorizationCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a valid access token, refreshing if necessary.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A valid access token.</returns>
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if OAuth is configured (has refresh token).
        /// </summary>
        /// <returns>True if OAuth is configured, false otherwise.</returns>
        Task<bool> IsConfiguredAsync();

        /// <summary>
        /// Revokes the current OAuth tokens and clears stored credentials.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RevokeTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about the authenticated YouTube channel.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>YouTube channel information.</returns>
        Task<YouTubeChannelInfo?> GetChannelInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the date and time when YouTube was first connected.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The connection timestamp, or null if not connected.</returns>
        Task<DateTime?> GetConnectionTimestampAsync(CancellationToken cancellationToken = default);
    }
}

