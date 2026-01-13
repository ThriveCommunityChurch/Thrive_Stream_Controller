using ThriveStreamController.Core.System;

namespace ThriveStreamController.Core.Models.Requests
{
    /// <summary>
    /// Request model for switching scenes.
    /// </summary>
    public class SwitchSceneRequest
    {
        /// <summary>
        /// Gets or sets the name of the scene to switch to.
        /// </summary>
        public string SceneName { get; set; } = string.Empty;

        /// <summary>
        /// Validates the switch scene request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>A validation response indicating success or failure.</returns>
        public static ValidationResponse ValidateRequest(SwitchSceneRequest? request)
        {
            if (request == null)
            {
                return new ValidationResponse(true, SystemMessages.EmptyRequest);
            }

            if (string.IsNullOrWhiteSpace(request.SceneName))
            {
                return new ValidationResponse(true, SystemMessages.OBSSceneNameRequired);
            }

            return new ValidationResponse(SystemMessages.Success);
        }
    }
}

