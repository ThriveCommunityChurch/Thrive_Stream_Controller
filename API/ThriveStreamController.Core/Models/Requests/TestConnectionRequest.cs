using ThriveStreamController.Core.System;

namespace ThriveStreamController.Core.Models.Requests
{
    /// <summary>
    /// Request model for testing OBS connection.
    /// </summary>
    public class TestConnectionRequest
    {
        /// <summary>
        /// Gets or sets the WebSocket URL to test (e.g., "ws://localhost:4455").
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional password for the WebSocket connection.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Validates the test connection request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>A validation response indicating success or failure.</returns>
        public static ValidationResponse ValidateRequest(TestConnectionRequest? request)
        {
            if (request == null)
            {
                return new ValidationResponse(true, SystemMessages.EmptyRequest);
            }

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return new ValidationResponse(true, SystemMessages.OBSUrlRequired);
            }

            // Validate URL format
            if (!request.Url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
                !request.Url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResponse(true, SystemMessages.OBSInvalidUrl);
            }

            // Validate URL is a valid URI
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            {
                return new ValidationResponse(true, string.Format(SystemMessages.InvalidProperty, "Url", "Must be a valid WebSocket URL"));
            }

            return new ValidationResponse(SystemMessages.Success);
        }
    }
}

