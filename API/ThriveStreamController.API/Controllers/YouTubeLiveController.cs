using Microsoft.AspNetCore.Mvc;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;

namespace ThriveStreamController.API.Controllers
{
    /// <summary>
    /// Controller for YouTube Live broadcast operations.
    /// </summary>
    [ApiController]
    [Route("api/youtube/live")]
    public class YouTubeLiveController : ControllerBase
    {
        private readonly IYouTubeLiveService _liveService;
        private readonly IYouTubeAuthService _authService;
        private readonly ILogger<YouTubeLiveController> _logger;

        public YouTubeLiveController(
            IYouTubeLiveService liveService,
            IYouTubeAuthService authService,
            ILogger<YouTubeLiveController> logger)
        {
            _liveService = liveService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new YouTube Live broadcast.
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> CreateBroadcast([FromBody] CreateBroadcastRequest request)
        {
            try
            {
                if (!await _authService.IsConfiguredAsync())
                {
                    return BadRequest(new { error = "YouTube is not configured. Please authorize first." });
                }

                var broadcast = await _liveService.CreateBroadcastAsync(
                    request.Title,
                    request.Description,
                    request.ScheduledStartTime);

                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating broadcast");
                return StatusCode(500, new { error = "Failed to create broadcast", message = ex.Message });
            }
        }

        /// <summary>
        /// Gets or creates the persistent stream configuration.
        /// </summary>
        [HttpGet("stream")]
        public async Task<ActionResult<YouTubeStreamInfo>> GetPersistentStream()
        {
            try
            {
                if (!await _authService.IsConfiguredAsync())
                {
                    return BadRequest(new { error = "YouTube is not configured. Please authorize first." });
                }

                var stream = await _liveService.GetOrCreatePersistentStreamAsync();
                return Ok(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting persistent stream");
                return StatusCode(500, new { error = "Failed to get stream", message = ex.Message });
            }
        }

        /// <summary>
        /// Binds a broadcast to the persistent stream.
        /// </summary>
        [HttpPost("broadcast/{broadcastId}/bind")]
        public async Task<ActionResult> BindBroadcast(string broadcastId)
        {
            try
            {
                var success = await _liveService.BindBroadcastToStreamAsync(broadcastId);
                if (success)
                {
                    return Ok(new { message = "Broadcast bound to stream successfully" });
                }
                return BadRequest(new { error = "Failed to bind broadcast to stream" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error binding broadcast {BroadcastId}", broadcastId);
                return StatusCode(500, new { error = "Failed to bind broadcast", message = ex.Message });
            }
        }

        /// <summary>
        /// Transitions a broadcast to testing status.
        /// </summary>
        [HttpPost("broadcast/{broadcastId}/testing")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> TransitionToTesting(string broadcastId)
        {
            try
            {
                var broadcast = await _liveService.TransitionToTestingAsync(broadcastId);
                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning broadcast {BroadcastId} to testing", broadcastId);
                return StatusCode(500, new { error = "Failed to transition to testing", message = ex.Message });
            }
        }

        /// <summary>
        /// Transitions a broadcast to live status.
        /// </summary>
        [HttpPost("broadcast/{broadcastId}/live")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> TransitionToLive(string broadcastId)
        {
            try
            {
                var broadcast = await _liveService.TransitionToLiveAsync(broadcastId);
                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning broadcast {BroadcastId} to live", broadcastId);
                return StatusCode(500, new { error = "Failed to go live", message = ex.Message });
            }
        }

        /// <summary>
        /// Ends a broadcast.
        /// </summary>
        [HttpPost("broadcast/{broadcastId}/end")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> EndBroadcast(string broadcastId)
        {
            try
            {
                var broadcast = await _liveService.EndBroadcastAsync(broadcastId);
                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending broadcast {BroadcastId}", broadcastId);
                return StatusCode(500, new { error = "Failed to end broadcast", message = ex.Message });
            }
        }

        /// <summary>
        /// Gets the status of a broadcast.
        /// </summary>
        [HttpGet("broadcast/{broadcastId}")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> GetBroadcastStatus(string broadcastId)
        {
            try
            {
                var broadcast = await _liveService.GetBroadcastStatusAsync(broadcastId);
                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting broadcast status {BroadcastId}", broadcastId);
                return StatusCode(500, new { error = "Failed to get broadcast status", message = ex.Message });
            }
        }

        /// <summary>
        /// Updates a broadcast's metadata.
        /// </summary>
        [HttpPatch("broadcast/{broadcastId}")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> UpdateBroadcast(
            string broadcastId,
            [FromBody] UpdateBroadcastRequest request)
        {
            try
            {
                var broadcast = await _liveService.UpdateBroadcastAsync(
                    broadcastId,
                    request.Title,
                    request.Description);
                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating broadcast {BroadcastId}", broadcastId);
                return StatusCode(500, new { error = "Failed to update broadcast", message = ex.Message });
            }
        }

        /// <summary>
        /// Gets the currently active broadcast, if any.
        /// </summary>
        [HttpGet("broadcast/active")]
        public async Task<ActionResult<YouTubeBroadcastInfo>> GetActiveBroadcast()
        {
            try
            {
                var broadcast = await _liveService.GetActiveBroadcastAsync();
                if (broadcast == null)
                {
                    return NotFound(new { message = "No active broadcast" });
                }
                return Ok(broadcast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active broadcast");
                return StatusCode(500, new { error = "Failed to get active broadcast", message = ex.Message });
            }
        }

        /// <summary>
        /// Gets broadcast defaults from the most recent broadcast.
        /// </summary>
        [HttpGet("defaults")]
        public async Task<ActionResult<YouTubeBroadcastDefaults>> GetDefaults()
        {
            try
            {
                var defaults = await _liveService.GetBroadcastDefaultsAsync();
                if (defaults == null)
                {
                    return Ok(new YouTubeBroadcastDefaults());
                }
                return Ok(defaults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting broadcast defaults");
                return StatusCode(500, new { error = "Failed to get defaults", message = ex.Message });
            }
        }

        /// <summary>
        /// Sets the thumbnail for a broadcast.
        /// </summary>
        [HttpPost("broadcast/{broadcastId}/thumbnail")]
        public async Task<ActionResult> SetThumbnail(string broadcastId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                // Save to temp file
                var tempPath = Path.GetTempFileName();
                var extension = Path.GetExtension(file.FileName);
                var filePath = Path.ChangeExtension(tempPath, extension);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                try
                {
                    var success = await _liveService.SetThumbnailAsync(broadcastId, filePath);
                    if (success)
                    {
                        return Ok(new { message = "Thumbnail set successfully" });
                    }
                    return BadRequest(new { error = "Failed to set thumbnail" });
                }
                finally
                {
                    // Clean up temp file
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting thumbnail for broadcast {BroadcastId}", broadcastId);
                return StatusCode(500, new { error = "Failed to set thumbnail", message = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for creating a broadcast.
    /// </summary>
    public class CreateBroadcastRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
    }

    /// <summary>
    /// Request model for updating a broadcast.
    /// </summary>
    public class UpdateBroadcastRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
