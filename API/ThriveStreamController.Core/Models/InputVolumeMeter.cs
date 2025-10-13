namespace ThriveStreamController.Core.Models
{
    /// <summary>
    /// Represents volume meter data for a single audio input.
    /// </summary>
    public class InputVolumeMeter
    {
        /// <summary>
        /// Name of the input.
        /// </summary>
        public string InputName { get; set; } = string.Empty;

        /// <summary>
        /// UUID of the input.
        /// </summary>
        public string InputUuid { get; set; } = string.Empty;

        /// <summary>
        /// Array of volume levels for each channel.
        /// Each value represents the volume level in dB (typically ranging from -60 to 0).
        /// </summary>
        public List<double> InputLevelsMul { get; set; } = new List<double>();

        /// <summary>
        /// Whether the input is muted.
        /// </summary>
        public bool InputMuted { get; set; } = false;
    }

    /// <summary>
    /// Represents the volume meters event data containing all active inputs.
    /// </summary>
    public class InputVolumeMetersData
    {
        /// <summary>
        /// Array of active inputs with their associated volume levels.
        /// </summary>
        public List<InputVolumeMeter> Inputs { get; set; } = new List<InputVolumeMeter>();
    }
}

