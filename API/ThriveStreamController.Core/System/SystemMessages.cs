namespace ThriveStreamController.Core.System
{
    /// <summary>
    /// Contains standard system messages for validation and error handling.
    /// </summary>
    public static class SystemMessages
    {
        // General validation messages
        public const string EmptyRequest = "Request cannot be null or empty.";
        public const string NullProperty = "Property '{0}' cannot be null or empty.";
        public const string InvalidProperty = "Property '{0}' has an invalid value: {1}";
        public const string Success = "Operation completed successfully.";

        // OBS-specific messages
        public const string OBSConnectionSuccess = "Successfully connected to OBS.";
        public const string OBSConnectionFailed = "Failed to connect to OBS. Please verify the URL and password are correct, and that OBS is running with WebSocket server enabled.";
        public const string OBSNotConnected = "Not connected to OBS. Please connect first.";
        public const string OBSDisconnectSuccess = "Successfully disconnected from OBS.";
        public const string OBSInvalidUrl = "Invalid WebSocket URL. URL must start with 'ws://' or 'wss://'.";
        public const string OBSUrlRequired = "WebSocket URL is required.";
        public const string OBSSceneNameRequired = "Scene name is required.";
        public const string OBSSceneSwitchSuccess = "Successfully switched to scene '{0}'.";
        public const string OBSSceneSwitchFailed = "Failed to switch to scene '{0}'.";
        public const string OBSStreamStartSuccess = "Stream started successfully.";
        public const string OBSStreamStartFailed = "Failed to start stream.";
        public const string OBSStreamStopSuccess = "Stream stopped successfully.";
        public const string OBSStreamStopFailed = "Failed to stop stream.";
        public const string OBSConnectionTimeout = "Connection to OBS timed out. Please ensure OBS is running and the WebSocket server is enabled.";
        public const string OBSAuthenticationFailed = "Authentication failed. Please check your password.";
    }
}

