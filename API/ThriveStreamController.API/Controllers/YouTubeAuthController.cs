using Microsoft.AspNetCore.Mvc;
using ThriveStreamController.Core.Interfaces;

namespace ThriveStreamController.API.Controllers
{
    /// <summary>
    /// Controller for handling YouTube OAuth authentication flow.
    /// </summary>
    [ApiController]
    [Route("api/auth/youtube")]
    public class YouTubeAuthController : ControllerBase
    {
        private readonly IYouTubeAuthService _authService;
        private readonly ILogger<YouTubeAuthController> _logger;

        public YouTubeAuthController(
            IYouTubeAuthService authService,
            ILogger<YouTubeAuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates the OAuth authorization flow by redirecting to Google's consent screen.
        /// </summary>
        /// <returns>Redirect to Google OAuth consent screen.</returns>
        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            try
            {
                var state = Guid.NewGuid().ToString();
                var authUrl = _authService.GetAuthorizationUrl(state);
                
                _logger.LogDebug("Redirecting to YouTube authorization URL");
                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating YouTube authorization");
                return BadRequest(new { error = "Failed to initiate authorization", message = ex.Message });
            }
        }

        /// <summary>
        /// Handles the OAuth callback from Google after user authorization.
        /// </summary>
        /// <param name="code">The authorization code from Google.</param>
        /// <param name="state">The state parameter for CSRF protection.</param>
        /// <param name="error">Error message if authorization failed.</param>
        /// <returns>Redirect to the UI with success or error message.</returns>
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error)
        {
            try
            {
                // Check if user denied authorization
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("YouTube authorization denied: {Error}", error);
                    return Redirect($"http://localhost:5173/settings?youtube_auth=error&message={Uri.EscapeDataString(error)}");
                }

                // Validate we have an authorization code
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogError("No authorization code received");
                    return Redirect("http://localhost:5173/settings?youtube_auth=error&message=No+authorization+code+received");
                }

                // Exchange the authorization code for tokens
                var success = await _authService.ExchangeAuthorizationCodeAsync(code);

                if (success)
                {
                    _logger.LogInformation("YouTube authorization successful");
                    return Redirect("http://localhost:5173/settings?youtube_auth=success");
                }
                else
                {
                    _logger.LogError("Failed to exchange authorization code");
                    return Redirect("http://localhost:5173/settings?youtube_auth=error&message=Failed+to+exchange+authorization+code");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling YouTube OAuth callback");
                return Redirect($"http://localhost:5173/settings?youtube_auth=error&message={Uri.EscapeDataString(ex.Message)}");
            }
        }

        /// <summary>
        /// Checks if YouTube OAuth is configured.
        /// </summary>
        /// <returns>Status of YouTube OAuth configuration.</returns>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var isConfigured = await _authService.IsConfiguredAsync();
                var connectionTimestamp = isConfigured ? await _authService.GetConnectionTimestampAsync() : null;

                return Ok(new {
                    IsConfigured = isConfigured,
                    Platform = "YouTube",
                    ConnectedAt = connectionTimestamp?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking YouTube OAuth status");
                return StatusCode(500, new { error = "Failed to check OAuth status", message = ex.Message });
            }
        }

        /// <summary>
        /// Gets information about the authenticated YouTube channel.
        /// </summary>
        /// <returns>YouTube channel information.</returns>
        [HttpGet("channel")]
        public async Task<IActionResult> GetChannelInfo()
        {
            try
            {
                var isConfigured = await _authService.IsConfiguredAsync();
                if (!isConfigured)
                {
                    return BadRequest(new { error = "YouTube is not configured. Please authorize first." });
                }

                var channelInfo = await _authService.GetChannelInfoAsync();

                if (channelInfo == null)
                {
                    return NotFound(new { error = "No YouTube channel found for authenticated user" });
                }

                return Ok(channelInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching YouTube channel information");
                return StatusCode(500, new { error = "Failed to fetch channel information", message = ex.Message });
            }
        }

        /// <summary>
        /// Revokes YouTube OAuth tokens and disconnects the integration.
        /// </summary>
        /// <returns>Success or error response.</returns>
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            try
            {
                await _authService.RevokeTokensAsync();
                _logger.LogInformation("YouTube OAuth tokens revoked");
                return Ok(new { message = "YouTube integration disconnected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking YouTube OAuth tokens");
                return StatusCode(500, new { error = "Failed to revoke tokens", message = ex.Message });
            }
        }
    }
}

