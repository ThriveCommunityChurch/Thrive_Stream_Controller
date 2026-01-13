using Microsoft.AspNetCore.DataProtection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ThriveStreamController.Core.Services;

namespace ThriveStreamController.Tests.Services
{
    [TestClass]
    public class CredentialEncryptionServiceTests
    {
        private Mock<IDataProtectionProvider> _mockDataProtectionProvider = null!;
        private Mock<IDataProtector> _mockDataProtector = null!;
        private CredentialEncryptionService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            _mockDataProtector = new Mock<IDataProtector>();

            _mockDataProtectionProvider
                .Setup(p => p.CreateProtector(It.IsAny<string>()))
                .Returns(_mockDataProtector.Object);

            _service = new CredentialEncryptionService(_mockDataProtectionProvider.Object);
        }

        [TestMethod]
        public async Task EncryptAsync_WithValidPlainText_ReturnsBase64String()
        {
            // Arrange
            var plainText = "my-secret-token";
            var expectedBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = new byte[] { 1, 2, 3, 4, 5 };

            _mockDataProtector
                .Setup(p => p.Protect(It.IsAny<byte[]>()))
                .Returns(protectedBytes);

            // Act
            var result = await _service.EncryptAsync(plainText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(Convert.ToBase64String(protectedBytes), result);
            _mockDataProtector.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EncryptAsync_WithNullPlainText_ThrowsArgumentException()
        {
            // Act
            await _service.EncryptAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EncryptAsync_WithEmptyPlainText_ThrowsArgumentException()
        {
            // Act
            await _service.EncryptAsync(string.Empty);
        }

        [TestMethod]
        public async Task DecryptAsync_WithValidEncryptedText_ReturnsPlainText()
        {
            // Arrange
            var expectedPlainText = "my-secret-token";
            var protectedBytes = new byte[] { 1, 2, 3, 4, 5 };
            var encryptedText = Convert.ToBase64String(protectedBytes);
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(expectedPlainText);

            _mockDataProtector
                .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
                .Returns(plainBytes);

            // Act
            var result = await _service.DecryptAsync(encryptedText);

            // Assert
            Assert.AreEqual(expectedPlainText, result);
            _mockDataProtector.Verify(p => p.Unprotect(It.IsAny<byte[]>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DecryptAsync_WithNullEncryptedText_ThrowsArgumentException()
        {
            // Act
            await _service.DecryptAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DecryptAsync_WithEmptyEncryptedText_ThrowsArgumentException()
        {
            // Act
            await _service.DecryptAsync(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DecryptAsync_WithInvalidBase64_ThrowsInvalidOperationException()
        {
            // Act - "not-valid-base64!!!" is not valid base64
            await _service.DecryptAsync("not-valid-base64!!!");
        }

        [TestMethod]
        public async Task EncryptAndDecrypt_RoundTrip_Works()
        {
            // This test verifies the flow works with a real DataProtector
            var provider = DataProtectionProvider.Create("TestApp");
            var realService = new CredentialEncryptionService(provider);

            // Arrange
            var originalText = "my-super-secret-refresh-token-12345";

            // Act
            var encrypted = await realService.EncryptAsync(originalText);
            var decrypted = await realService.DecryptAsync(encrypted);

            // Assert
            Assert.AreEqual(originalText, decrypted);
            Assert.AreNotEqual(originalText, encrypted);
        }

        [TestMethod]
        public void Constructor_CreatesProtectorWithCorrectPurpose()
        {
            // Verify the protector was created with the expected purpose string
            _mockDataProtectionProvider.Verify(
                p => p.CreateProtector("ThriveStreamController.Credentials.v1"),
                Times.Once);
        }
    }
}

