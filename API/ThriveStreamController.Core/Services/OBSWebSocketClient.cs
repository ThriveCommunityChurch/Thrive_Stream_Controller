using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThriveStreamController.Core.Services
{
    /// <summary>
    /// Custom OBS WebSocket client designed for ASP.NET Core
    /// Implements the OBS WebSocket v5.x protocol
    /// </summary>
    public class OBSWebSocketClient : IDisposable
    {
        private readonly ILogger<OBSWebSocketClient> _logger;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _receiveTask;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, TaskCompletionSource<JObject>> _pendingRequests = new();
        private readonly object _requestLock = new object();

        // Connection state
        private string? _url;
        private string? _password;
        private bool _isIdentified;

        // Events
        public event EventHandler? Connected;
        public event EventHandler? Disconnected;
        public event EventHandler<JObject>? EventReceived;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open && _isIdentified;

        public OBSWebSocketClient(ILogger<OBSWebSocketClient> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Connect to OBS WebSocket server
        /// </summary>
        public async Task<bool> ConnectAsync(string url, string? password = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Connecting to OBS WebSocket at {Url}", url);

                _url = url;
                _password = password;
                _isIdentified = false;

                // Create new WebSocket
                _webSocket = new ClientWebSocket();
                _webSocket.Options.AddSubProtocol("obswebsocket.json");

                // Connect
                await _webSocket.ConnectAsync(new Uri(url), cancellationToken);
                _logger.LogInformation("WebSocket connected, waiting for Hello message...");

                // Start receive loop
                _cancellationTokenSource = new CancellationTokenSource();
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                // Wait for identification to complete (with timeout)
                var identifyTimeout = Task.Delay(5000, cancellationToken);
                while (!_isIdentified && !cancellationToken.IsCancellationRequested)
                {
                    if (await Task.WhenAny(Task.Delay(100, cancellationToken), identifyTimeout) == identifyTimeout)
                    {
                        _logger.LogError("Timeout waiting for identification");
                        await DisconnectAsync();
                        return false;
                    }
                }

                if (_isIdentified)
                {
                    _logger.LogInformation("Successfully connected and identified with OBS");
                    Connected?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to OBS WebSocket");
                await DisconnectAsync();
                return false;
            }
        }

        /// <summary>
        /// Disconnect from OBS WebSocket server
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _isIdentified = false;

                // Cancel receive loop
                _cancellationTokenSource?.Cancel();

                // Close WebSocket
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                }

                // Wait for receive task to complete
                if (_receiveTask != null)
                {
                    await _receiveTask;
                }

                _webSocket?.Dispose();
                _webSocket = null;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                Disconnected?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("Disconnected from OBS WebSocket");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from OBS WebSocket");
            }
        }

        /// <summary>
        /// Send a request to OBS and wait for response
        /// </summary>
        public async Task<JObject?> SendRequestAsync(string requestType, JObject? requestData = null, int timeoutMs = 10000)
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Cannot send request: Not connected");
                return null;
            }

            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<JObject>();

            // Register pending request
            lock (_requestLock)
            {
                _pendingRequests[requestId] = tcs;
            }

            try
            {
                // Build request message (OpCode 6 = Request)
                var request = new JObject
                {
                    ["op"] = 6,
                    ["d"] = new JObject
                    {
                        ["requestType"] = requestType,
                        ["requestId"] = requestId,
                        ["requestData"] = requestData ?? new JObject()
                    }
                };

                _logger.LogDebug("Sending request: {RequestType} (ID: {RequestId})", requestType, requestId);

                // Send request
                await SendMessageAsync(request);

                // Wait for response with timeout
                using var cts = new CancellationTokenSource(timeoutMs);
                cts.Token.Register(() => tcs.TrySetCanceled());

                var response = await tcs.Task;
                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request timeout: {RequestType}", requestType);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request: {RequestType}", requestType);
                return null;
            }
            finally
            {
                // Remove pending request
                lock (_requestLock)
                {
                    _pendingRequests.Remove(requestId);
                }
            }
        }

        private async Task SendMessageAsync(JObject message)
        {
            await _sendLock.WaitAsync();
            try
            {
                if (_webSocket?.State != WebSocketState.Open)
                {
                    throw new InvalidOperationException("WebSocket is not open");
                }

                var json = message.ToString(Formatting.None);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            try
            {
                while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
                {
                    var messageBuilder = new StringBuilder();

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogWarning("WebSocket close message received. Status: {Status}, Description: {Description}",
                                result.CloseStatus,
                                result.CloseStatusDescription);
                            await DisconnectAsync();
                            return;
                        }

                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    var messageJson = messageBuilder.ToString();
                    await ProcessMessageAsync(messageJson);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Receive loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in receive loop");
                await DisconnectAsync();
            }
        }

        private async Task ProcessMessageAsync(string messageJson)
        {
            try
            {
                var message = JObject.Parse(messageJson);
                var opCode = message["op"]?.Value<int>() ?? -1;

                _logger.LogDebug("Received message with OpCode: {OpCode}", opCode);

                switch (opCode)
                {
                    case 0: // Hello
                        await HandleHelloAsync(message["d"] as JObject);
                        break;

                    case 2: // Identified
                        HandleIdentified(message["d"] as JObject);
                        break;

                    case 5: // Event
                        HandleEvent(message["d"] as JObject);
                        break;

                    case 7: // RequestResponse
                        HandleRequestResponse(message["d"] as JObject);
                        break;

                    default:
                        _logger.LogWarning("Unknown OpCode: {OpCode}", opCode);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", messageJson);
            }
        }

        private async Task HandleHelloAsync(JObject? data)
        {
            if (data == null) return;

            _logger.LogInformation("Received Hello from OBS");

            var rpcVersion = data["rpcVersion"]?.Value<int>() ?? 1;
            var authentication = data["authentication"] as JObject;

            // Build Identify message (OpCode 1)
            // Event subscription bitmask:
            // General (1 << 0) = 1
            // Config (1 << 1) = 2
            // Scenes (1 << 2) = 4
            // Inputs (1 << 3) = 8
            // Transitions (1 << 4) = 16
            // Filters (1 << 5) = 32
            // Outputs (1 << 6) = 64
            // SceneItems (1 << 7) = 128
            // MediaInputs (1 << 8) = 256
            // Vendors (1 << 9) = 512
            // Ui (1 << 10) = 1024
            // All = 2047 (sum of all above)
            // InputVolumeMeters (1 << 16) = 65536 (high-volume event for audio levels)
            var eventSubscriptions = 2047 + 65536; // Subscribe to all events including InputVolumeMeters
            var identifyData = new JObject
            {
                ["rpcVersion"] = rpcVersion,
                ["eventSubscriptions"] = eventSubscriptions
            };

            // Handle authentication if required
            if (authentication != null)
            {
                if (string.IsNullOrEmpty(_password))
                {
                    _logger.LogWarning("OBS requires authentication but no password was provided");
                }
                else
                {
                    var challenge = authentication["challenge"]?.Value<string>();
                    var salt = authentication["salt"]?.Value<string>();

                    if (challenge != null && salt != null)
                    {
                        _logger.LogInformation("Generating authentication string (password length: {Length})", _password.Length);
                        var authString = GenerateAuthString(_password, salt, challenge);
                        identifyData["authentication"] = authString;
                        _logger.LogInformation("Authentication required, sending credentials");
                    }
                    else
                    {
                        _logger.LogWarning("Authentication required but challenge or salt is missing");
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_password))
            {
                _logger.LogInformation("Password provided but OBS does not require authentication");
            }
            else
            {
                _logger.LogInformation("No authentication required");
            }

            var identify = new JObject
            {
                ["op"] = 1,
                ["d"] = identifyData
            };

            _logger.LogInformation("Sending Identify message: {Message}", identify.ToString(Formatting.None));
            await SendMessageAsync(identify);
        }

        private void HandleIdentified(JObject? data)
        {
            if (data == null) return;

            var negotiatedRpcVersion = data["negotiatedRpcVersion"]?.Value<int>() ?? 1;
            _logger.LogInformation("Identified with OBS, RPC version: {Version}", negotiatedRpcVersion);

            _isIdentified = true;
        }

        private void HandleEvent(JObject? data)
        {
            if (data == null) return;

            var eventType = data["eventType"]?.Value<string>();
            _logger.LogDebug("Received event: {EventType}", eventType);

            EventReceived?.Invoke(this, data);
        }

        private void HandleRequestResponse(JObject? data)
        {
            if (data == null) return;

            var requestId = data["requestId"]?.Value<string>();
            if (string.IsNullOrEmpty(requestId)) return;

            var requestStatus = data["requestStatus"] as JObject;
            var result = requestStatus?["result"]?.Value<bool>() ?? false;
            var code = requestStatus?["code"]?.Value<int>() ?? 0;

            _logger.LogDebug("Received response for request {RequestId}: Result={Result}, Code={Code}", requestId, result, code);

            // Find and complete the pending request
            TaskCompletionSource<JObject>? tcs = null;
            lock (_requestLock)
            {
                if (_pendingRequests.TryGetValue(requestId, out tcs))
                {
                    _pendingRequests.Remove(requestId);
                }
            }

            if (tcs != null)
            {
                if (result)
                {
                    tcs.SetResult(data);
                }
                else
                {
                    var comment = requestStatus?["comment"]?.Value<string>();
                    _logger.LogWarning("Request failed: Code={Code}, Comment={Comment}", code, comment);
                    tcs.SetResult(data); // Still return the response so caller can handle the error
                }
            }
        }

        private string GenerateAuthString(string password, string salt, string challenge)
        {
            // Step 1: Concatenate password + salt
            var passwordSalt = password + salt;

            // Step 2: Generate SHA256 hash and base64 encode
            using var sha256 = SHA256.Create();
            var passwordSaltBytes = Encoding.UTF8.GetBytes(passwordSalt);
            var passwordSaltHash = sha256.ComputeHash(passwordSaltBytes);
            var base64Secret = Convert.ToBase64String(passwordSaltHash);

            // Step 3: Concatenate base64Secret + challenge
            var secretChallenge = base64Secret + challenge;

            // Step 4: Generate SHA256 hash and base64 encode
            var secretChallengeBytes = Encoding.UTF8.GetBytes(secretChallenge);
            var secretChallengeHash = sha256.ComputeHash(secretChallengeBytes);
            var authString = Convert.ToBase64String(secretChallengeHash);

            return authString;
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _sendLock.Dispose();
        }
    }
}

