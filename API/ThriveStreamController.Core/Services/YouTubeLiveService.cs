using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;
using ThriveStreamController.Data;
using ThriveStreamController.Data.Entities;

namespace ThriveStreamController.Core.Services
{
    /// <summary>
    /// Service for managing YouTube Live broadcasts.
    /// </summary>
    public class YouTubeLiveService : IYouTubeLiveService
    {
        private readonly IYouTubeAuthService _authService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<YouTubeLiveService> _logger;
        private const string PersistentStreamTitle = "Thrive Stream Controller - Persistent Stream";

        public YouTubeLiveService(
            IYouTubeAuthService authService,
            ApplicationDbContext dbContext,
            ILogger<YouTubeLiveService> logger)
        {
            _authService = authService;
            _dbContext = dbContext;
            _logger = logger;
        }

        private async Task<YouTubeService> GetYouTubeServiceAsync(CancellationToken cancellationToken)
        {
            var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
            return new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
                ApplicationName = "Thrive Stream Controller"
            });
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo> CreateBroadcastAsync(
            string title,
            string? description = null,
            DateTime? scheduledStartTime = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating YouTube broadcast: {Title}", title);

                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);
                var startTime = scheduledStartTime ?? DateTime.UtcNow.AddMinutes(1);

                var broadcast = new LiveBroadcast
                {
                    Snippet = new LiveBroadcastSnippet
                    {
                        Title = title,
                        Description = description ?? string.Empty,
                        ScheduledStartTimeDateTimeOffset = startTime
                    },
                    Status = new LiveBroadcastStatus
                    {
                        PrivacyStatus = "public",
                        SelfDeclaredMadeForKids = false
                    },
                    ContentDetails = new LiveBroadcastContentDetails
                    {
                        EnableAutoStart = false,
                        EnableAutoStop = true,
                        EnableDvr = true,
                        EnableContentEncryption = true,
                        EnableEmbed = true,
                        RecordFromStart = true,
                        StartWithSlate = false,
                        EnableClosedCaptions = false,
                        ClosedCaptionsType = "closedCaptionsDisabled",
                        MonitorStream = new MonitorStreamInfo
                        {
                            EnableMonitorStream = false,
                            BroadcastStreamDelayMs = 0
                        }
                    }
                };

                var request = youtubeService.LiveBroadcasts.Insert(broadcast, "snippet,status,contentDetails");
                var response = await request.ExecuteAsync(cancellationToken);

                _logger.LogInformation("Created YouTube broadcast: {BroadcastId}", response.Id);

                return MapBroadcastToInfo(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating YouTube broadcast");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<YouTubeStreamInfo> GetOrCreatePersistentStreamAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if we have a persistent stream config in the database
                var persistentConfig = await _dbContext.PersistentStreamConfigs
                    .Where(c => c.Platform == "YouTube" && c.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                if (persistentConfig != null && !string.IsNullOrEmpty(persistentConfig.StreamId))
                {
                    _logger.LogInformation("Found existing persistent stream: {StreamId}", persistentConfig.StreamId);
                    
                    // Verify the stream still exists on YouTube
                    var existingStream = await GetStreamByIdAsync(persistentConfig.StreamId, cancellationToken);
                    if (existingStream != null)
                    {
                        return existingStream;
                    }
                    
                    _logger.LogWarning("Persistent stream {StreamId} no longer exists, creating new one", persistentConfig.StreamId);
                }

                // Create a new persistent stream
                return await CreatePersistentStreamAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating persistent stream");
                throw;
            }
        }

        private async Task<YouTubeStreamInfo> CreatePersistentStreamAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new persistent YouTube stream");

            var youtubeService = await GetYouTubeServiceAsync(cancellationToken);

            var stream = new LiveStream
            {
                Snippet = new LiveStreamSnippet
                {
                    Title = PersistentStreamTitle,
                    Description = "Persistent stream for Thrive Community Church live broadcasts"
                },
                Cdn = new CdnSettings
                {
                    FrameRate = "60fps",
                    Resolution = "1080p",
                    IngestionType = "rtmp"
                }
            };

            var request = youtubeService.LiveStreams.Insert(stream, "snippet,cdn,status");
            var response = await request.ExecuteAsync(cancellationToken);

            var streamInfo = MapStreamToInfo(response);

            // Store in database for future use
            await StorePersistentStreamConfigAsync(streamInfo, cancellationToken);

            _logger.LogInformation("Created persistent stream: {StreamId}", streamInfo.Id);
            return streamInfo;
        }

        private async Task StorePersistentStreamConfigAsync(YouTubeStreamInfo streamInfo, CancellationToken cancellationToken)
        {
            // Deactivate any existing YouTube stream configs
            var existingConfigs = await _dbContext.PersistentStreamConfigs
                .Where(c => c.Platform == "YouTube" && c.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var config in existingConfigs)
            {
                config.IsActive = false;
            }

            // Create new config
            var newConfig = new PersistentStreamConfig
            {
                Platform = "YouTube",
                StreamId = streamInfo.Id,
                StreamKey = streamInfo.StreamKey,
                RtmpUrl = streamInfo.RtmpUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.PersistentStreamConfigs.Add(newConfig);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<YouTubeStreamInfo?> GetStreamByIdAsync(string streamId, CancellationToken cancellationToken)
        {
            try
            {
                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);
                var request = youtubeService.LiveStreams.List("snippet,cdn,status");
                request.Id = streamId;

                var response = await request.ExecuteAsync(cancellationToken);

                if (response.Items == null || response.Items.Count == 0)
                {
                    return null;
                }

                return MapStreamToInfo(response.Items[0]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching stream {StreamId}", streamId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> BindBroadcastToStreamAsync(string broadcastId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Binding broadcast {BroadcastId} to persistent stream", broadcastId);

                var streamInfo = await GetOrCreatePersistentStreamAsync(cancellationToken);
                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);

                var request = youtubeService.LiveBroadcasts.Bind(broadcastId, "id,snippet,contentDetails,status");
                request.StreamId = streamInfo.Id;

                var response = await request.ExecuteAsync(cancellationToken);

                _logger.LogInformation("Successfully bound broadcast {BroadcastId} to stream {StreamId}",
                    broadcastId, streamInfo.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error binding broadcast {BroadcastId} to stream", broadcastId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo> TransitionToTestingAsync(string broadcastId, CancellationToken cancellationToken = default)
        {
            return await TransitionBroadcastAsync(broadcastId, LiveBroadcastsResource.TransitionRequest.BroadcastStatusEnum.Testing, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo> TransitionToLiveAsync(string broadcastId, CancellationToken cancellationToken = default)
        {
            return await TransitionBroadcastAsync(broadcastId, LiveBroadcastsResource.TransitionRequest.BroadcastStatusEnum.Live, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo> EndBroadcastAsync(string broadcastId, CancellationToken cancellationToken = default)
        {
            return await TransitionBroadcastAsync(broadcastId, LiveBroadcastsResource.TransitionRequest.BroadcastStatusEnum.Complete, cancellationToken);
        }

        private async Task<YouTubeBroadcastInfo> TransitionBroadcastAsync(
            string broadcastId,
            LiveBroadcastsResource.TransitionRequest.BroadcastStatusEnum status,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Transitioning broadcast {BroadcastId} to {Status}", broadcastId, status);

                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);
                var request = youtubeService.LiveBroadcasts.Transition(status, broadcastId, "id,snippet,contentDetails,status");

                var response = await request.ExecuteAsync(cancellationToken);

                _logger.LogInformation("Broadcast {BroadcastId} transitioned to {Status}", broadcastId, status);
                return MapBroadcastToInfo(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning broadcast {BroadcastId} to {Status}", broadcastId, status);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo> GetBroadcastStatusAsync(string broadcastId, CancellationToken cancellationToken = default)
        {
            try
            {
                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);
                var request = youtubeService.LiveBroadcasts.List("id,snippet,contentDetails,status");
                request.Id = broadcastId;

                var response = await request.ExecuteAsync(cancellationToken);

                if (response.Items == null || response.Items.Count == 0)
                {
                    throw new InvalidOperationException($"Broadcast {broadcastId} not found");
                }

                return MapBroadcastToInfo(response.Items[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting broadcast status for {BroadcastId}", broadcastId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetThumbnailAsync(string broadcastId, string thumbnailPath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(thumbnailPath))
                {
                    _logger.LogError("Thumbnail file not found: {Path}", thumbnailPath);
                    return false;
                }

                _logger.LogInformation("Setting thumbnail for broadcast {BroadcastId}", broadcastId);

                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);

                using var stream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(thumbnailPath);

                var request = youtubeService.Thumbnails.Set(broadcastId, stream, contentType);
                var response = await request.UploadAsync(cancellationToken);

                if (response.Status == UploadStatus.Completed)
                {
                    _logger.LogInformation("Thumbnail set successfully for broadcast {BroadcastId}", broadcastId);
                    return true;
                }

                _logger.LogError("Failed to upload thumbnail: {Status}", response.Status);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting thumbnail for broadcast {BroadcastId}", broadcastId);
                return false;
            }
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo> UpdateBroadcastAsync(
            string broadcastId,
            string? title = null,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating broadcast {BroadcastId}", broadcastId);

                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);

                // Get current broadcast
                var getRequest = youtubeService.LiveBroadcasts.List("id,snippet,status");
                getRequest.Id = broadcastId;
                var current = await getRequest.ExecuteAsync(cancellationToken);

                if (current.Items == null || current.Items.Count == 0)
                {
                    throw new InvalidOperationException($"Broadcast {broadcastId} not found");
                }

                var broadcast = current.Items[0];

                // Update fields
                if (!string.IsNullOrEmpty(title))
                {
                    broadcast.Snippet.Title = title;
                }
                if (description != null)
                {
                    broadcast.Snippet.Description = description;
                }

                var updateRequest = youtubeService.LiveBroadcasts.Update(broadcast, "snippet");
                var response = await updateRequest.ExecuteAsync(cancellationToken);

                _logger.LogInformation("Updated broadcast {BroadcastId}", broadcastId);
                return MapBroadcastToInfo(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating broadcast {BroadcastId}", broadcastId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastInfo?> GetActiveBroadcastAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);
                var request = youtubeService.LiveBroadcasts.List("id,snippet,contentDetails,status");
                request.BroadcastStatus = LiveBroadcastsResource.ListRequest.BroadcastStatusEnum.Active;
                request.MaxResults = 1;

                var response = await request.ExecuteAsync(cancellationToken);

                if (response.Items == null || response.Items.Count == 0)
                {
                    return null;
                }

                return MapBroadcastToInfo(response.Items[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active broadcast");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<YouTubeBroadcastDefaults?> GetBroadcastDefaultsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var youtubeService = await GetYouTubeServiceAsync(cancellationToken);
                var request = youtubeService.LiveBroadcasts.List("id,snippet,status");
                request.BroadcastStatus = LiveBroadcastsResource.ListRequest.BroadcastStatusEnum.Completed;
                request.MaxResults = 1;

                var response = await request.ExecuteAsync(cancellationToken);

                if (response.Items == null || response.Items.Count == 0)
                {
                    return null;
                }

                var lastBroadcast = response.Items[0];
                return new YouTubeBroadcastDefaults
                {
                    TitleTemplate = lastBroadcast.Snippet.Title,
                    Description = lastBroadcast.Snippet.Description,
                    PrivacyStatus = lastBroadcast.Status?.PrivacyStatus ?? "public",
                    SourceBroadcastId = lastBroadcast.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting broadcast defaults");
                return null;
            }
        }

        private static YouTubeBroadcastInfo MapBroadcastToInfo(LiveBroadcast broadcast)
        {
            return new YouTubeBroadcastInfo
            {
                Id = broadcast.Id,
                Title = broadcast.Snippet?.Title ?? string.Empty,
                Description = broadcast.Snippet?.Description,
                ScheduledStartTime = broadcast.Snippet?.ScheduledStartTimeDateTimeOffset?.DateTime,
                ActualStartTime = broadcast.Snippet?.ActualStartTimeDateTimeOffset?.DateTime,
                ActualEndTime = broadcast.Snippet?.ActualEndTimeDateTimeOffset?.DateTime,
                LifecycleStatus = broadcast.Status?.LifeCycleStatus ?? "unknown",
                PrivacyStatus = broadcast.Status?.PrivacyStatus ?? "private",
                BoundStreamId = broadcast.ContentDetails?.BoundStreamId,
                WatchUrl = $"https://www.youtube.com/watch?v={broadcast.Id}",
                ThumbnailUrl = broadcast.Snippet?.Thumbnails?.High?.Url
                               ?? broadcast.Snippet?.Thumbnails?.Medium?.Url
                               ?? broadcast.Snippet?.Thumbnails?.Default__?.Url
            };
        }

        private static YouTubeStreamInfo MapStreamToInfo(LiveStream stream)
        {
            return new YouTubeStreamInfo
            {
                Id = stream.Id,
                Title = stream.Snippet?.Title ?? string.Empty,
                StreamKey = stream.Cdn?.IngestionInfo?.StreamName ?? string.Empty,
                RtmpUrl = stream.Cdn?.IngestionInfo?.IngestionAddress ?? string.Empty,
                HealthStatus = stream.Status?.HealthStatus?.Status,
                IsActive = stream.Status?.StreamStatus == "active"
            };
        }
    }
}
