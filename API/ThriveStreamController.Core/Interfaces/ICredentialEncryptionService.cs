namespace ThriveStreamController.Core.Interfaces
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive credentials (OAuth tokens, access tokens, etc.).
    /// </summary>
    public interface ICredentialEncryptionService
    {
        /// <summary>
        /// Encrypts a plaintext credential value.
        /// </summary>
        /// <param name="plainText">The plaintext value to encrypt.</param>
        /// <returns>The encrypted value as a base64-encoded string.</returns>
        Task<string> EncryptAsync(string plainText);

        /// <summary>
        /// Decrypts an encrypted credential value.
        /// </summary>
        /// <param name="encryptedText">The encrypted value (base64-encoded string).</param>
        /// <returns>The decrypted plaintext value.</returns>
        Task<string> DecryptAsync(string encryptedText);
    }
}

