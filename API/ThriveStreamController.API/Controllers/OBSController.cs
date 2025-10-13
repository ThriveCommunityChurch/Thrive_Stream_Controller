using Microsoft.AspNetCore.Mvc;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;
using ThriveStreamController.Core.Models.Requests;
using ThriveStreamController.Core.System;

namespace ThriveStreamController.API.Controllers
{
    /// <summary>
    /// Controller for OBS WebSocket operations.
    /// Provides endpoints for connecting, disconnecting, and controlling OBS Studio.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OBSController : ControllerBase
    {
        private readonly IOBSService _obsService;
        private readonly ILogger<OBSController> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="OBSController"/> class.
        /// </summary>
        /// <param name="obsService">The OBS service instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configuration">The configuration instance.</param>
        public OBSController(
            IOBSService obsService,
            ILogger<OBSController> logger,
            IConfiguration configuration)
        {
            _obsService = obsService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the current OBS connection status.
        /// </summary>
        /// <returns>The current connection status.</returns>
        [HttpGet("status")]
        public ActionResult<OBSConnectionStatus> GetStatus()
        {
            return Ok(_obsService.ConnectionStatus);
        }

        /// <summary>
        /// Tests a connection to the OBS WebSocket server with provided credentials.
        /// This does not persist the connection - it's only for validation.
        /// </summary>
        /// <param name="request">The connection test request containing URL and optional password.</param>
        /// <returns>The connection test result.</returns>
        [HttpPost("test-connection")]
        public async Task<ActionResult> TestConnection([FromBody] TestConnectionRequest request)
        {
            // Validate the request
            var validationResponse = TestConnectionRequest.ValidateRequest(request);
            if (validationResponse.HasErrors)
            {
                _logger.LogWarning("Test connection validation failed: {Error}", validationResponse.ErrorMessage);
                return BadRequest(new
                {
                    message = validationResponse.ErrorMessage,
                    success = false,
                    hasErrors = true
                });
            }

            try
            {
                _logger.LogInformation("Testing connection to OBS at {Url}", request.Url);

                // Create a temporary OBS service instance for testing
                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var testLogger = loggerFactory.CreateLogger<ThriveStreamController.Core.Services.OBSService>();
                var clientLogger = loggerFactory.CreateLogger<ThriveStreamController.Core.Services.OBSWebSocketClient>();
                var testService = new ThriveStreamController.Core.Services.OBSService(testLogger, clientLogger);

                var success = await testService.ConnectAsync(request.Url, request.Password);

                if (success)
                {
                    // Disconnect immediately after successful test
                    await testService.DisconnectAsync();

                    _logger.LogInformation("Test connection successful to {Url}", request.Url);
                    return Ok(new
                    {
                        message = SystemMessages.OBSConnectionSuccess,
                        success = true,
                        hasErrors = false,
                        url = request.Url
                    });
                }

                _logger.LogWarning("Test connection failed to {Url}", request.Url);
                return BadRequest(new
                {
                    message = SystemMessages.OBSConnectionFailed,
                    success = false,
                    hasErrors = true,
                    url = request.Url
                });
            }
            catch (InvalidOperationException ex)
            {
                // These are our custom exceptions with user-friendly messages
                _logger.LogError(ex, "OBS connection error: {Message}", ex.Message);
                return BadRequest(new
                {
                    message = ex.Message,
                    success = false,
                    hasErrors = true,
                    url = request.Url
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error testing OBS connection");
                return StatusCode(500, new
                {
                    message = SystemMessages.OBSConnectionFailed,
                    error = ex.Message,
                    success = false,
                    hasErrors = true
                });
            }
        }

        /// <summary>
        /// Connects to the OBS WebSocket server using configuration settings.
        /// </summary>
        /// <returns>The connection result.</returns>
        [HttpPost("connect")]
        public async Task<ActionResult<OBSConnectionStatus>> Connect()
        {
            try
            {
                var url = _configuration["OBS:WebSocketUrl"] ?? "ws://localhost:4455";
                var password = _configuration["OBS:Password"];

                var success = await _obsService.ConnectAsync(url, password);
                if (success)
                {
                    return Ok(_obsService.ConnectionStatus);
                }

                return BadRequest(new { message = "Failed to connect to OBS", status = _obsService.ConnectionStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Disconnects from the OBS WebSocket server.
        /// </summary>
        /// <returns>The disconnection result.</returns>
        [HttpPost("disconnect")]
        public async Task<ActionResult> Disconnect()
        {
            try
            {
                await _obsService.DisconnectAsync();
                return Ok(new { message = "Disconnected from OBS" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a list of all available scenes from OBS.
        /// </summary>
        /// <returns>A list of OBS scenes with the current active scene.</returns>
        [HttpGet("scenes")]
        public async Task<ActionResult<ScenesResponse>> GetScenes()
        {
            try
            {
                var scenes = await _obsService.GetScenesAsync();
                var currentScene = scenes.FirstOrDefault(s => s.IsActive)?.Name ?? string.Empty;

                var response = new ScenesResponse
                {
                    Scenes = scenes,
                    CurrentScene = currentScene
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scenes from OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Switches to the specified scene in OBS.
        /// </summary>
        /// <param name="request">The scene switch request containing the scene name.</param>
        /// <returns>The scene switch result.</returns>
        [HttpPost("scenes/switch")]
        public async Task<ActionResult> SwitchScene([FromBody] SwitchSceneRequest request)
        {
            // Validate the request
            var validationResponse = SwitchSceneRequest.ValidateRequest(request);
            if (validationResponse.HasErrors)
            {
                _logger.LogWarning("Switch scene validation failed: {Error}", validationResponse.ErrorMessage);
                return BadRequest(new
                {
                    message = validationResponse.ErrorMessage,
                    hasErrors = true
                });
            }

            try
            {
                var success = await _obsService.SwitchSceneAsync(request.SceneName);

                if (success)
                {
                    return Ok(new
                    {
                        message = string.Format(SystemMessages.OBSSceneSwitchSuccess, request.SceneName),
                        hasErrors = false
                    });
                }

                return BadRequest(new
                {
                    message = string.Format(SystemMessages.OBSSceneSwitchFailed, request.SceneName),
                    hasErrors = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching scene in OBS");
                return StatusCode(500, new
                {
                    message = string.Format(SystemMessages.OBSSceneSwitchFailed, request.SceneName),
                    error = ex.Message,
                    hasErrors = true
                });
            }
        }

        /// <summary>
        /// Gets the current streaming status from OBS.
        /// </summary>
        /// <returns>The current streaming status.</returns>
        [HttpGet("streaming/status")]
        public async Task<ActionResult<StreamingStatus>> GetStreamingStatus()
        {
            try
            {
                var status = await _obsService.GetStreamingStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streaming status from OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Starts streaming in OBS.
        /// </summary>
        /// <returns>The start streaming result.</returns>
        [HttpPost("streaming/start")]
        public async Task<ActionResult> StartStreaming()
        {
            try
            {
                var success = await _obsService.StartStreamingAsync();

                if (success)
                {
                    return Ok(new { message = "Streaming started" });
                }

                return BadRequest(new { message = "Failed to start streaming" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting streaming in OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Stops streaming in OBS.
        /// </summary>
        /// <returns>The stop streaming result.</returns>
        [HttpPost("streaming/stop")]
        public async Task<ActionResult> StopStreaming()
        {
            try
            {
                var success = await _obsService.StopStreamingAsync();

                if (success)
                {
                    return Ok(new { message = "Streaming stopped" });
                }

                return BadRequest(new { message = "Failed to stop streaming" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping streaming in OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the scene items for a specific scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <returns>A list of scene items.</returns>
        [HttpGet("scenes/{sceneName}/items")]
        public async Task<ActionResult<List<SceneItem>>> GetSceneItems(string sceneName)
        {
            try
            {
                var sceneItems = await _obsService.GetSceneItemsAsync(sceneName);
                return Ok(sceneItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scene items from OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the media input status for a specific input.
        /// </summary>
        /// <param name="inputName">The name of the media input.</param>
        /// <returns>The media input status.</returns>
        [HttpGet("media/{inputName}/status")]
        public async Task<ActionResult<MediaInputStatus>> GetMediaInputStatus(string inputName)
        {
            try
            {
                var status = await _obsService.GetMediaInputStatusAsync(inputName);

                if (status == null)
                {
                    return NotFound(new { message = $"Media input '{inputName}' not found or not a media source" });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media input status from OBS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

}

