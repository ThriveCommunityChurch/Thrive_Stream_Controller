using Microsoft.Extensions.Logging;

namespace ThriveStreamController.Core.System
{
    /// <summary>
    /// Generic validation response used to validate request objects.
    /// </summary>
    public class ValidationResponse : SystemResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResponse"/> class for failure scenarios.
        /// </summary>
        /// <param name="didError">Indicates whether an error occurred.</param>
        /// <param name="errorMsg">The error message.</param>
        public ValidationResponse(bool didError, string errorMsg)
        {
            HasErrors = didError;
            ErrorMessage = errorMsg;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResponse"/> class for success scenarios.
        /// </summary>
        /// <param name="successMsg">The success message.</param>
        public ValidationResponse(string successMsg)
        {
            HasErrors = false;
            SuccessMessage = successMsg;
        }
    }
}

