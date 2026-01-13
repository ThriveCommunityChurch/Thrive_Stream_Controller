namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents media status for a specific scene.
    /// </summary>
    public class SceneMediaStatus
    {
        /// <summary>
        /// Gets or sets the scene name.
        /// </summary>
        public string SceneName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the media input name.
        /// </summary>
        public string? MediaInputName { get; set; }

        /// <summary>
        /// Gets or sets the media input status.
        /// </summary>
        public MediaInputStatus? Status { get; set; }
    }
}

