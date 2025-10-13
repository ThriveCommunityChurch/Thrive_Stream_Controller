namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents an OBS scene with its name and active status.
    /// </summary>
    public class OBSScene
    {
        /// <summary>
        /// Gets or sets the name of the scene.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this scene is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the index of the scene in the OBS scene list.
        /// </summary>
        public int Index { get; set; }
    }
}

