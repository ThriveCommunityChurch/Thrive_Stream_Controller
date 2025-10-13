using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;

namespace ThriveStreamController.Core.Services
{
    /// <summary>
    /// Service for managing OBS WebSocket connections and operations.
    /// Implements the IOBSService interface to provide OBS control functionality.
    /// Uses custom WebSocket client designed for ASP.NET Core.
    /// </summary>
    public class OBSService : IOBSService, IDisposable
    {
        private readonly ILogger<OBSService> _logger;
        private readonly OBSWebSocketClient _client;
        private OBSConnectionStatus _connectionStatus;
        private readonly object _statusLock = new object();
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _isConnecting = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OBSService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="clientLogger">The logger instance for the WebSocket client.</param>
        public OBSService(ILogger<OBSService> logger, ILogger<OBSWebSocketClient> clientLogger)
        {
            _logger = logger;
            _client = new OBSWebSocketClient(clientLogger);
            _connectionStatus = new OBSConnectionStatus
            {
                IsConnected = false,
                ServerUrl = string.Empty
            };

            // Subscribe to client events
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.EventReceived += OnEventReceived;
        }

        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        public OBSConnectionStatus ConnectionStatus => _connectionStatus;

        /// <summary>
        /// Event raised when the connection status changes.
        /// </summary>
        public event EventHandler<OBSConnectionStatus>? ConnectionStatusChanged;

        /// <summary>
        /// Event raised when the active scene changes in OBS.
        /// </summary>
        public event EventHandler<string>? SceneChanged;

        /// <summary>
        /// Event raised when the streaming status changes in OBS.
        /// </summary>
        public event EventHandler<StreamingStatus>? StreamingStatusChanged;

        /// <summary>
        /// Connects to the OBS WebSocket server.
        /// </summary>
        /// <param name="url">The WebSocket server URL (e.g., "ws://localhost:4455").</param>
        /// <param name="password">The WebSocket server password (optional).</param>
        /// <returns>A task that represents the asynchronous connect operation. Returns true if connection was successful.</returns>
        public async Task<bool> ConnectAsync(string url, string? password = null)
        {
            // Check if already connected
            if (_client.IsConnected)
            {
                _logger.LogInformation("Already connected to OBS");
                return true;
            }

            // Check if already connecting
            if (_isConnecting)
            {
                _logger.LogWarning("Connection attempt already in progress");
                return false;
            }

            // Acquire lock to prevent multiple simultaneous connection attempts
            await _connectionLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_client.IsConnected)
                {
                    _logger.LogInformation("Already connected to OBS (after lock)");
                    return true;
                }

                if (_isConnecting)
                {
                    _logger.LogWarning("Connection attempt already in progress (after lock)");
                    return false;
                }

                _isConnecting = true;
                _logger.LogInformation("Attempting to connect to OBS at {Url}", url);

                lock (_statusLock)
                {
                    _connectionStatus.ServerUrl = url;
                    _connectionStatus.LastError = null;
                }

                try
                {
                    // Attempt to connect using our custom WebSocket client
                    var success = await _client.ConnectAsync(url, password);

                    if (success)
                    {
                        lock (_statusLock)
                        {
                            _connectionStatus.IsConnected = true;
                            _connectionStatus.ConnectedAt = DateTime.UtcNow;
                            _connectionStatus.LastError = null;
                        }
                        _logger.LogInformation("Successfully connected to OBS");
                        ConnectionStatusChanged?.Invoke(this, _connectionStatus);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Connection attempt failed");
                        lock (_statusLock)
                        {
                            _connectionStatus.LastError = "Connection attempt failed";
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to OBS: {Message}", ex.Message);
                    lock (_statusLock)
                    {
                        _connectionStatus.LastError = $"Error: {ex.Message}";
                    }
                    throw;
                }
            }
            finally
            {
                _isConnecting = false;
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Disconnects from the OBS WebSocket server.
        /// </summary>
        /// <returns>A task that represents the asynchronous disconnect operation.</returns>
        public async Task DisconnectAsync()
        {
            try
            {
                _logger.LogInformation("Disconnecting from OBS...");
                await _client.DisconnectAsync();

                lock (_statusLock)
                {
                    _connectionStatus.IsConnected = false;
                    _connectionStatus.LastError = null;
                }

                ConnectionStatusChanged?.Invoke(this, _connectionStatus);
                _logger.LogInformation("Disconnected from OBS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from OBS");
                throw;
            }
        }

        /// <summary>
        /// Gets a list of all scenes from OBS.
        /// </summary>
        /// <returns>A list of OBS scenes.</returns>
        public async Task<List<OBSScene>> GetScenesAsync()
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot get scenes: Not connected to OBS");
                    return [];
                }

                _logger.LogInformation("Fetching scenes from OBS...");

                // Send GetSceneList request
                var response = await _client.SendRequestAsync("GetSceneList");

                if (response == null)
                {
                    _logger.LogWarning("GetSceneList returned null");
                    return [];
                }

                // Check if request was successful
                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (!result)
                {
                    var code = requestStatus?["code"]?.Value<int>() ?? 0;
                    var comment = requestStatus?["comment"]?.Value<string>();
                    _logger.LogWarning("GetSceneList failed: Code={Code}, Comment={Comment}", code, comment);
                    return [];
                }

                // Parse response data
                var responseData = response["responseData"] as JObject;
                if (responseData == null)
                {
                    _logger.LogWarning("GetSceneList response has no data");
                    return [];
                }

                var currentProgramSceneName = responseData["currentProgramSceneName"]?.Value<string>();
                var scenesArray = responseData["scenes"] as JArray;

                if (scenesArray == null || scenesArray.Count == 0)
                {
                    _logger.LogWarning("GetSceneList returned no scenes");
                    return [];
                }

                var scenes = new List<OBSScene>();
                foreach (var sceneToken in scenesArray)
                {
                    var sceneObj = sceneToken as JObject;
                    if (sceneObj == null) continue;

                    var sceneName = sceneObj["sceneName"]?.Value<string>();
                    var sceneIndex = sceneObj["sceneIndex"]?.Value<int>() ?? 0;

                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        scenes.Add(new OBSScene
                        {
                            Name = sceneName,
                            IsActive = sceneName == currentProgramSceneName,
                            Index = sceneIndex
                        });
                    }
                }

                _logger.LogInformation("Retrieved {Count} scenes from OBS", scenes.Count);
                return scenes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scenes from OBS: {Message}", ex.Message);
                return [];
            }
        }

        /// <summary>
        /// Switches to the specified scene in OBS.
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to.</param>
        /// <returns>A task that represents the asynchronous operation. Returns true if successful.</returns>
        public async Task<bool> SwitchSceneAsync(string sceneName)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot switch scene: Not connected to OBS");
                    return false;
                }

                _logger.LogInformation("Switching to scene: {SceneName}", sceneName);

                var requestData = new JObject
                {
                    ["sceneName"] = sceneName
                };

                var response = await _client.SendRequestAsync("SetCurrentProgramScene", requestData);

                if (response == null)
                {
                    _logger.LogWarning("SetCurrentProgramScene returned null");
                    return false;
                }

                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (result)
                {
                    _logger.LogInformation("Successfully switched to scene: {SceneName}", sceneName);
                    SceneChanged?.Invoke(this, sceneName);
                }
                else
                {
                    var code = requestStatus?["code"]?.Value<int>() ?? 0;
                    var comment = requestStatus?["comment"]?.Value<string>();
                    _logger.LogWarning("SetCurrentProgramScene failed: Code={Code}, Comment={Comment}", code, comment);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching scene: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the current streaming status from OBS.
        /// </summary>
        /// <returns>The current streaming status.</returns>
        public async Task<StreamingStatus> GetStreamingStatusAsync()
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot get streaming status: Not connected to OBS");
                    return new StreamingStatus { IsStreaming = false };
                }

                var response = await _client.SendRequestAsync("GetStreamStatus");

                if (response == null)
                {
                    return new StreamingStatus { IsStreaming = false };
                }

                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (!result)
                {
                    return new StreamingStatus { IsStreaming = false };
                }

                var responseData = response["responseData"] as JObject;
                var outputActive = responseData?["outputActive"]?.Value<bool>() ?? false;
                var outputDuration = responseData?["outputDuration"]?.Value<long>() ?? 0;

                return new StreamingStatus
                {
                    IsStreaming = outputActive,
                    StreamDurationSeconds = (int)(outputDuration / 1000)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streaming status: {Message}", ex.Message);
                return new StreamingStatus { IsStreaming = false };
            }
        }

        /// <summary>
        /// Starts streaming in OBS.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. Returns true if successful.</returns>
        public async Task<bool> StartStreamingAsync()
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot start streaming: Not connected to OBS");
                    return false;
                }

                _logger.LogInformation("Starting stream...");

                var response = await _client.SendRequestAsync("StartStream");

                if (response == null)
                {
                    return false;
                }

                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (result)
                {
                    _logger.LogInformation("Successfully started streaming");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting stream: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops streaming in OBS.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. Returns true if successful.</returns>
        public async Task<bool> StopStreamingAsync()
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot stop streaming: Not connected to OBS");
                    return false;
                }

                _logger.LogInformation("Stopping stream...");

                var response = await _client.SendRequestAsync("StopStream");

                if (response == null)
                {
                    return false;
                }

                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (result)
                {
                    _logger.LogInformation("Successfully stopped streaming");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping stream: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        /// <returns>The current connection status.</returns>
        public OBSConnectionStatus GetConnectionStatus()
        {
            lock (_statusLock)
            {
                return _connectionStatus;
            }
        }


        private void OnConnected(object? sender, EventArgs e)
        {
            _logger.LogInformation("OBS WebSocket connected event received");
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            _logger.LogWarning("OBS WebSocket disconnected event received");

            lock (_statusLock)
            {
                _connectionStatus.IsConnected = false;
            }

            ConnectionStatusChanged?.Invoke(this, _connectionStatus);
        }

        private void OnEventReceived(object? sender, JObject eventData)
        {
            var eventType = eventData["eventType"]?.Value<string>();
            _logger.LogDebug("Received OBS event: {EventType}", eventType);

            // Handle specific events
            switch (eventType)
            {
                case "CurrentProgramSceneChanged":
                    var sceneName = eventData["eventData"]?["sceneName"]?.Value<string>();
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        SceneChanged?.Invoke(this, sceneName);
                    }
                    break;

                case "StreamStateChanged":
                    var outputActive = eventData["eventData"]?["outputActive"]?.Value<bool>() ?? false;
                    StreamingStatusChanged?.Invoke(this, new StreamingStatus
                    {
                        IsStreaming = outputActive,
                        StreamDurationSeconds = 0
                    });
                    break;
            }
        }

        /// <summary>
        /// Gets the list of scene items for a specific scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to get items for.</param>
        /// <returns>A list of scene items.</returns>
        public async Task<List<SceneItem>> GetSceneItemsAsync(string sceneName)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot get scene items: Not connected to OBS");
                    return new List<SceneItem>();
                }

                _logger.LogInformation("Getting scene items for scene: {SceneName}", sceneName);

                var requestData = new JObject
                {
                    ["sceneName"] = sceneName
                };

                var response = await _client.SendRequestAsync("GetSceneItemList", requestData);

                if (response == null)
                {
                    _logger.LogWarning("GetSceneItemList returned null");
                    return new List<SceneItem>();
                }

                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (!result)
                {
                    var code = requestStatus?["code"]?.Value<int>() ?? 0;
                    var comment = requestStatus?["comment"]?.Value<string>();
                    _logger.LogWarning("GetSceneItemList failed: Code={Code}, Comment={Comment}", code, comment);
                    return new List<SceneItem>();
                }

                var responseData = response["responseData"] as JObject;
                var sceneItemsArray = responseData?["sceneItems"] as JArray;

                if (sceneItemsArray == null)
                {
                    _logger.LogWarning("No scene items found in response");
                    return new List<SceneItem>();
                }

                var sceneItems = new List<SceneItem>();
                foreach (var item in sceneItemsArray)
                {
                    var sceneItem = new SceneItem
                    {
                        SceneItemId = item["sceneItemId"]?.Value<int>() ?? 0,
                        SceneItemIndex = item["sceneItemIndex"]?.Value<int>() ?? 0,
                        SourceName = item["sourceName"]?.Value<string>() ?? string.Empty,
                        SourceUuid = item["sourceUuid"]?.Value<string>() ?? string.Empty,
                        SourceType = item["sourceType"]?.Value<string>() ?? string.Empty,
                        SceneItemEnabled = item["sceneItemEnabled"]?.Value<bool>() ?? false
                    };
                    sceneItems.Add(sceneItem);
                }

                _logger.LogInformation("Found {Count} scene items", sceneItems.Count);
                return sceneItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scene items: {Message}", ex.Message);
                return new List<SceneItem>();
            }
        }

        /// <summary>
        /// Gets the media input status for a specific input.
        /// </summary>
        /// <param name="inputName">The name of the media input.</param>
        /// <returns>The media input status.</returns>
        public async Task<MediaInputStatus?> GetMediaInputStatusAsync(string inputName)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _logger.LogWarning("Cannot get media input status: Not connected to OBS");
                    return null;
                }

                _logger.LogInformation("Getting media input status for: {InputName}", inputName);

                var requestData = new JObject
                {
                    ["inputName"] = inputName
                };

                var response = await _client.SendRequestAsync("GetMediaInputStatus", requestData);

                if (response == null)
                {
                    _logger.LogWarning("GetMediaInputStatus returned null");
                    return null;
                }

                var requestStatus = response["requestStatus"] as JObject;
                var result = requestStatus?["result"]?.Value<bool>() ?? false;

                if (!result)
                {
                    var code = requestStatus?["code"]?.Value<int>() ?? 0;
                    var comment = requestStatus?["comment"]?.Value<string>();
                    _logger.LogWarning("GetMediaInputStatus failed: Code={Code}, Comment={Comment}", code, comment);
                    return null;
                }

                var responseData = response["responseData"] as JObject;

                if (responseData == null)
                {
                    _logger.LogWarning("No response data found");
                    return null;
                }

                var mediaStatus = new MediaInputStatus
                {
                    MediaState = responseData["mediaState"]?.Value<string>() ?? string.Empty,
                    MediaDuration = responseData["mediaDuration"]?.Value<long?>(),
                    MediaCursor = responseData["mediaCursor"]?.Value<long?>()
                };

                _logger.LogInformation("Media status: State={State}, Duration={Duration}ms, Cursor={Cursor}ms",
                    mediaStatus.MediaState, mediaStatus.MediaDuration, mediaStatus.MediaCursor);

                return mediaStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media input status: {Message}", ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _connectionLock?.Dispose();
        }
    }
}
