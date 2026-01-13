namespace ThriveStreamController.Core.System
{
    /// <summary>
    /// Base class for all system responses.
    /// </summary>
    public class SystemResponseBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the response has errors.
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        /// Gets or sets the error message if HasErrors is true.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the success message if HasErrors is false.
        /// </summary>
        public string? SuccessMessage { get; set; }
    }
}

