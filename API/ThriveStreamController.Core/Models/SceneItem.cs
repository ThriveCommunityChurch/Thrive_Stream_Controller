namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents an item (source) within an OBS scene.
    /// </summary>
    public class SceneItem
    {
        /// <summary>
        /// Gets or sets the numeric ID of the scene item.
        /// </summary>
        public int SceneItemId { get; set; }

        /// <summary>
        /// Gets or sets the index position of the scene item.
        /// </summary>
        public int SceneItemIndex { get; set; }

        /// <summary>
        /// Gets or sets the name of the source.
        /// </summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UUID of the source.
        /// </summary>
        public string SourceUuid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the source.
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the scene item is enabled.
        /// </summary>
        public bool SceneItemEnabled { get; set; }
    }
}

