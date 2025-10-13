using Microsoft.Extensions.Logging;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;

namespace ThriveStreamController.Core.Services
{
    /// <summary>
    /// Service that tracks media status for all scenes and broadcasts updates.
    /// </summary>
    public class MediaStatusTracker
    {
        private readonly ILogger<MediaStatusTracker> _logger;
        private readonly IOBSService _obsService;
        private readonly Dictionary<string, SceneMediaStatus> _sceneMediaCache = new();
        private readonly object _cacheLock = new object();
        private List<OBSScene> _scenes = new();
        private Task? _trackingTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<SceneMediaStatus>? MediaStatusChanged;

        public MediaStatusTracker(ILogger<MediaStatusTracker> logger, IOBSService obsService)
        {
            _logger = logger;
            _obsService = obsService;
        }

        public void Start()
        {
            if (_trackingTask != null)
            {
                return; // Already started
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _trackingTask = Task.Run(() => ExecuteAsync(_cancellationTokenSource.Token));
        }

        public async Task StopAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_trackingTask != null)
            {
                await _trackingTask;
            }
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Media Status Tracker started");

            // Wait for OBS to be connected
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_obsService.ConnectionStatus.IsConnected)
                {
                    break;
                }
                await Task.Delay(1000, stoppingToken);
            }

            // Main tracking loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_obsService.ConnectionStatus.IsConnected)
                    {
                        await UpdateAllSceneMediaStatusAsync();
                    }

                    // Poll every second for cursor updates
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in media status tracking loop");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogDebug("Media Status Tracker stopped");
        }

        private async Task UpdateAllSceneMediaStatusAsync()
        {
            try
            {
                // Get all scenes
                var scenes = await _obsService.GetScenesAsync();
                _scenes = scenes;

                // Check each scene for media sources
                foreach (var scene in scenes)
                {
                    await UpdateSceneMediaStatusAsync(scene.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all scene media status");
            }
        }

        private async Task UpdateSceneMediaStatusAsync(string sceneName)
        {
            try
            {
                // Get scene items
                var sceneItems = await _obsService.GetSceneItemsAsync(sceneName);

                // Find first media source
                SceneItem? mediaSource = null;
                foreach (var item in sceneItems)
                {
                    if (item.SourceType == "OBS_SOURCE_TYPE_INPUT" && item.SceneItemEnabled)
                    {
                        // Try to get media status to see if it's a media source
                        try
                        {
                            var status = await _obsService.GetMediaInputStatusAsync(item.SourceName);
                            if (status != null)
                            {
                                mediaSource = item;
                                
                                // Create or update scene media status
                                var sceneMediaStatus = new SceneMediaStatus
                                {
                                    SceneName = sceneName,
                                    MediaInputName = item.SourceName,
                                    Status = status
                                };

                                // Check if status has changed
                                bool hasChanged = false;
                                lock (_cacheLock)
                                {
                                    if (!_sceneMediaCache.TryGetValue(sceneName, out var cached) ||
                                        cached.Status?.MediaCursor != status.MediaCursor ||
                                        cached.Status?.MediaState != status.MediaState)
                                    {
                                        _sceneMediaCache[sceneName] = sceneMediaStatus;
                                        hasChanged = true;
                                    }
                                }

                                // Broadcast if changed
                                if (hasChanged)
                                {
                                    MediaStatusChanged?.Invoke(this, sceneMediaStatus);
                                }

                                break; // Found media source, stop looking
                            }
                        }
                        catch
                        {
                            // Not a media source, continue
                        }
                    }
                }

                // If no media source found, clear cache for this scene
                if (mediaSource == null)
                {
                    lock (_cacheLock)
                    {
                        if (_sceneMediaCache.ContainsKey(sceneName))
                        {
                            _sceneMediaCache.Remove(sceneName);
                            
                            // Broadcast null status
                            MediaStatusChanged?.Invoke(this, new SceneMediaStatus
                            {
                                SceneName = sceneName,
                                MediaInputName = null,
                                Status = null
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating media status for scene {SceneName}", sceneName);
            }
        }

        public Dictionary<string, SceneMediaStatus> GetAllMediaStatus()
        {
            lock (_cacheLock)
            {
                return new Dictionary<string, SceneMediaStatus>(_sceneMediaCache);
            }
        }
    }
}

