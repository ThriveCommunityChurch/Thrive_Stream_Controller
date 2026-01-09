using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;
using ThriveStreamController.Core.Interfaces;

namespace ThriveStreamController.Core.Services
{
    /// <summary>
    /// Implementation of credential encryption service using ASP.NET Core Data Protection API.
    /// </summary>
    public class CredentialEncryptionService : ICredentialEncryptionService
    {
        private readonly IDataProtector _protector;

        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialEncryptionService"/> class.
        /// </summary>
        /// <param name="dataProtectionProvider">The data protection provider.</param>
        public CredentialEncryptionService(IDataProtectionProvider dataProtectionProvider)
        {
            // Create a protector with a specific purpose string
            // This ensures that data encrypted for this purpose cannot be decrypted by other purposes
            _protector = dataProtectionProvider.CreateProtector("ThriveStreamController.Credentials.v1");
        }

        /// <inheritdoc />
        public Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));
            }

            try
            {
                // Convert plaintext to bytes
                var plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Protect (encrypt) the data
                var protectedBytes = _protector.Protect(plainBytes);

                // Convert to base64 for storage
                var encryptedText = Convert.ToBase64String(protectedBytes);

                return Task.FromResult(encryptedText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to encrypt credential.", ex);
            }
        }

        /// <inheritdoc />
        public Task<string> DecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                throw new ArgumentException("Encrypted text cannot be null or empty.", nameof(encryptedText));
            }

            try
            {
                // Convert from base64
                var protectedBytes = Convert.FromBase64String(encryptedText);

                // Unprotect (decrypt) the data
                var plainBytes = _protector.Unprotect(protectedBytes);

                // Convert bytes back to string
                var plainText = Encoding.UTF8.GetString(plainBytes);

                return Task.FromResult(plainText);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Invalid encrypted text format.", ex);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to decrypt credential. The data may be corrupted or encrypted with a different key.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt credential.", ex);
            }
        }
    }
}

