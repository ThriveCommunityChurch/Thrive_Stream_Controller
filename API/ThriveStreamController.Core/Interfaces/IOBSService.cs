using ThriveStreamController.Core.Models;

namespace ThriveStreamController.Core.Interfaces
{
    /// <summary>
    /// Interface for OBS WebSocket service operations.
    /// Provides methods to connect, disconnect, and interact with OBS Studio.
    /// </summary>
    public interface IOBSService
    {
        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        OBSConnectionStatus ConnectionStatus { get; }

        /// <summary>
        /// Event raised when the connection status changes.
        /// </summary>
        event EventHandler<OBSConnectionStatus>? ConnectionStatusChanged;

        /// <summary>
        /// Event raised when the active scene changes in OBS.
        /// </summary>
        event EventHandler<string>? SceneChanged;

        /// <summary>
        /// Event raised when the streaming status changes in OBS.
        /// </summary>
        event EventHandler<StreamingStatus>? StreamingStatusChanged;

        /// <summary>
        /// Connects to the OBS WebSocket server.
        /// </summary>
        /// <param name="url">The WebSocket server URL (e.g., "ws://localhost:4455").</param>
        /// <param name="password">The WebSocket server password (optional).</param>
        /// <returns>A task that represents the asynchronous connect operation. Returns true if connection was successful.</returns>
        Task<bool> ConnectAsync(string url, string? password = null);

        /// <summary>
        /// Disconnects from the OBS WebSocket server.
        /// </summary>
        /// <returns>A task that represents the asynchronous disconnect operation.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Gets a list of all available scenes from OBS.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of OBS scenes.</returns>
        Task<List<OBSScene>> GetScenesAsync();

        /// <summary>
        /// Switches to the specified scene in OBS.
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to.</param>
        /// <returns>A task that represents the asynchronous operation. Returns true if the scene was switched successfully.</returns>
        Task<bool> SwitchSceneAsync(string sceneName);

        /// <summary>
        /// Gets the current streaming status from OBS.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current streaming status.</returns>
        Task<StreamingStatus> GetStreamingStatusAsync();

        /// <summary>
        /// Starts streaming in OBS.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. Returns true if streaming started successfully.</returns>
        Task<bool> StartStreamingAsync();

        /// <summary>
        /// Stops streaming in OBS.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. Returns true if streaming stopped successfully.</returns>
        Task<bool> StopStreamingAsync();

        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        /// <returns>The current OBS connection status.</returns>
        OBSConnectionStatus GetConnectionStatus();

        /// <summary>
        /// Gets the list of scene items for a specific scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to get items for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of scene items.</returns>
        Task<List<SceneItem>> GetSceneItemsAsync(string sceneName);

        /// <summary>
        /// Gets the media input status for a specific input.
        /// </summary>
        /// <param name="inputName">The name of the media input.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media input status.</returns>
        Task<MediaInputStatus?> GetMediaInputStatusAsync(string inputName);
    }
}

