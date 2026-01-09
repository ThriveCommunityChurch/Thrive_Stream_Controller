using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;
using ThriveStreamController.Data;
using ThriveStreamController.Data.Entities;

namespace ThriveStreamController.Core.Services
{
    /// <summary>
    /// Implementation of YouTube OAuth 2.0 authentication service.
    /// </summary>
    public class YouTubeAuthService : IYouTubeAuthService
    {
        private readonly YouTubeConfiguration _config;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICredentialEncryptionService _encryptionService;
        private readonly ILogger<YouTubeAuthService> _logger;
        private readonly GoogleAuthorizationCodeFlow _flow;

        public YouTubeAuthService(
            IOptions<YouTubeConfiguration> config,
            ApplicationDbContext dbContext,
            ICredentialEncryptionService encryptionService,
            ILogger<YouTubeAuthService> logger)
        {
            _config = config.Value;
            _dbContext = dbContext;
            _encryptionService = encryptionService;
            _logger = logger;

            // Initialize the OAuth flow
            _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _config.ClientId,
                    ClientSecret = _config.ClientSecret
                },
                Scopes = _config.Scopes,
                DataStore = new NullDataStore() // We'll handle storage ourselves
            });
        }

        /// <inheritdoc />
        public string GetAuthorizationUrl(string? state = null)
        {
            if (!_config.IsValid())
            {
                throw new InvalidOperationException("YouTube configuration is not valid. ClientId and ClientSecret are required.");
            }

            var codeRequestUrl = _flow.CreateAuthorizationCodeRequest(_config.RedirectUri);
            codeRequestUrl.State = state ?? Guid.NewGuid().ToString();
            
            var authUrl = codeRequestUrl.Build();
            _logger.LogInformation("Generated YouTube authorization URL");
            
            return authUrl.ToString();
        }

        /// <inheritdoc />
        public async Task<bool> ExchangeAuthorizationCodeAsync(string authorizationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exchanging authorization code for tokens");

                // Exchange the authorization code for tokens
                var tokenResponse = await _flow.ExchangeCodeForTokenAsync(
                    "user", // User ID (we only have one user for this app)
                    authorizationCode,
                    _config.RedirectUri,
                    cancellationToken);

                if (tokenResponse == null)
                {
                    _logger.LogError("Failed to exchange authorization code: null response");
                    return false;
                }

                // Store the tokens in the database
                await StoreTokensAsync(tokenResponse, cancellationToken);

                _logger.LogInformation("Successfully exchanged authorization code and stored tokens");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging authorization code");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get stored credentials
                var credential = await GetStoredCredentialAsync(cancellationToken);
                
                if (credential == null)
                {
                    throw new InvalidOperationException("No YouTube credentials found. Please authorize the application first.");
                }

                // Decrypt the refresh token
                var refreshToken = await _encryptionService.DecryptAsync(credential.EncryptedRefreshToken!);

                // Check if access token is still valid
                if (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
                {
                    // Access token is still valid
                    return await _encryptionService.DecryptAsync(credential.EncryptedValue);
                }

                // Access token expired, refresh it
                _logger.LogInformation("Access token expired, refreshing...");
                
                var tokenResponse = await _flow.RefreshTokenAsync(
                    "user",
                    refreshToken,
                    cancellationToken);

                // Update stored tokens
                await StoreTokensAsync(tokenResponse, cancellationToken);

                _logger.LogInformation("Successfully refreshed access token");
                return tokenResponse.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access token");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsConfiguredAsync()
        {
            if (!_config.IsValid())
            {
                return false;
            }

            var credential = await GetStoredCredentialAsync();
            return credential != null && !string.IsNullOrWhiteSpace(credential.EncryptedRefreshToken);
        }

        /// <inheritdoc />
        public async Task RevokeTokensAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var credential = await GetStoredCredentialAsync(cancellationToken);

                if (credential != null)
                {
                    // Revoke the token with Google
                    var accessToken = await _encryptionService.DecryptAsync(credential.EncryptedValue);
                    await _flow.RevokeTokenAsync("user", accessToken, cancellationToken);

                    // Remove from database
                    _dbContext.PlatformCredentials.Remove(credential);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Successfully revoked YouTube tokens");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking YouTube tokens");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<YouTubeChannelInfo?> GetChannelInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching YouTube channel information");

                // Get a valid access token
                var accessToken = await GetAccessTokenAsync(cancellationToken);

                // Create YouTube service
                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
                    ApplicationName = "Thrive Stream Controller"
                });

                // Request channel information for the authenticated user
                var channelsRequest = youtubeService.Channels.List("snippet,statistics,contentDetails");
                channelsRequest.Mine = true;

                var channelsResponse = await channelsRequest.ExecuteAsync(cancellationToken);

                if (channelsResponse.Items == null || channelsResponse.Items.Count == 0)
                {
                    _logger.LogWarning("No YouTube channel found for authenticated user");
                    return null;
                }

                var channel = channelsResponse.Items[0];

                var channelInfo = new YouTubeChannelInfo
                {
                    Id = channel.Id,
                    Title = channel.Snippet.Title,
                    Description = channel.Snippet.Description,
                    CustomUrl = channel.Snippet.CustomUrl,
                    ThumbnailUrl = channel.Snippet.Thumbnails?.High?.Url
                                   ?? channel.Snippet.Thumbnails?.Medium?.Url
                                   ?? channel.Snippet.Thumbnails?.Default__?.Url,
                    SubscriberCount = (long?)channel.Statistics?.SubscriberCount,
                    VideoCount = (long?)channel.Statistics?.VideoCount,
                    ViewCount = (long?)channel.Statistics?.ViewCount,
                    HiddenSubscriberCount = channel.Statistics?.HiddenSubscriberCount ?? false,
                    PublishedAt = channel.Snippet.PublishedAtDateTimeOffset?.DateTime
                };

                _logger.LogInformation("Successfully fetched YouTube channel info: {ChannelTitle}", channelInfo.Title);
                return channelInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching YouTube channel information");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetConnectionTimestampAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var credential = await GetStoredCredentialAsync(cancellationToken);
                return credential?.CreatedAt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting YouTube connection timestamp");
                throw;
            }
        }

        private async Task<PlatformCredential?> GetStoredCredentialAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.PlatformCredentials
                .Where(c => c.Platform == "YouTube" && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task StoreTokensAsync(TokenResponse tokenResponse, CancellationToken cancellationToken = default)
        {
            // Deactivate any existing credentials
            var existingCredentials = await _dbContext.PlatformCredentials
                .Where(c => c.Platform == "YouTube" && c.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var cred in existingCredentials)
            {
                cred.IsActive = false;
            }

            // Encrypt the tokens
            var encryptedAccessToken = await _encryptionService.EncryptAsync(tokenResponse.AccessToken);
            var encryptedRefreshToken = tokenResponse.RefreshToken != null
                ? await _encryptionService.EncryptAsync(tokenResponse.RefreshToken)
                : null;

            // Create new credential record
            var credential = new PlatformCredential
            {
                Platform = "YouTube",
                CredentialType = "OAuth2",
                EncryptedValue = encryptedAccessToken,
                EncryptedRefreshToken = encryptedRefreshToken,
                ExpiresAt = tokenResponse.IssuedUtc.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.PlatformCredentials.Add(credential);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Null data store for Google OAuth flow (we handle storage ourselves).
    /// </summary>
    internal class NullDataStore : IDataStore
    {
        public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
        public Task DeleteAsync<T>(string key) => Task.CompletedTask;
        public Task<T> GetAsync<T>(string key) => Task.FromResult(default(T)!);
        public Task ClearAsync() => Task.CompletedTask;
    }
}

