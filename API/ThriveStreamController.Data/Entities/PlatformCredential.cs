namespace ThriveStreamController.Data.Entities
{
    /// <summary>
    /// Represents encrypted credentials for streaming platforms (YouTube, Facebook).
    /// </summary>
    public class PlatformCredential
    {
        /// <summary>
        /// Gets or sets the unique identifier for the credential.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the platform name (YouTube, Facebook).
        /// </summary>
        public string Platform { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the credential type (OAuth, AccessToken, ApiKey).
        /// </summary>
        public string CredentialType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the encrypted credential value.
        /// </summary>
        public string EncryptedValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the encrypted refresh token (for OAuth).
        /// </summary>
        public string? EncryptedRefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the expiration date/time for the credential.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets whether this credential is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the date and time when this record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}

