namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Response containing the list of scenes and the current active scene.
    /// </summary>
    public class ScenesResponse
    {
        /// <summary>
        /// Gets or sets the name of the currently active scene.
        /// </summary>
        public string CurrentScene { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of all available scenes.
        /// </summary>
        public List<OBSScene> Scenes { get; set; } = new();
    }
}

