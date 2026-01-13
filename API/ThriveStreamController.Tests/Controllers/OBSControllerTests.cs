using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ThriveStreamController.API.Controllers;
using ThriveStreamController.Core.Interfaces;
using ThriveStreamController.Core.Models;
using ThriveStreamController.Core.Models.Requests;

namespace ThriveStreamController.Tests.Controllers
{
    [TestClass]
    public class OBSControllerTests
    {
        private Mock<IOBSService> _mockOBSService = null!;
        private Mock<ILogger<OBSController>> _mockLogger = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private OBSController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockOBSService = new Mock<IOBSService>();
            _mockLogger = new Mock<ILogger<OBSController>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _controller = new OBSController(
                _mockOBSService.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);
        }

        [TestMethod]
        public void GetStatus_ReturnsOkWithConnectionStatus()
        {
            // Arrange
            var expectedStatus = new OBSConnectionStatus
            {
                IsConnected = true,
                ServerUrl = "ws://localhost:4455",
                ConnectedAt = DateTime.UtcNow
            };
            _mockOBSService.Setup(s => s.ConnectionStatus).Returns(expectedStatus);

            // Act
            var result = _controller.GetStatus();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(expectedStatus, okResult.Value);
        }

        [TestMethod]
        public async Task Connect_WhenSuccessful_ReturnsOkWithStatus()
        {
            // Arrange
            var expectedStatus = new OBSConnectionStatus
            {
                IsConnected = true,
                ServerUrl = "ws://localhost:4455"
            };

            _mockConfiguration.Setup(c => c["OBS:WebSocketUrl"]).Returns("ws://localhost:4455");
            _mockConfiguration.Setup(c => c["OBS:Password"]).Returns("testpassword");
            _mockOBSService.Setup(s => s.ConnectAsync("ws://localhost:4455", "testpassword"))
                .ReturnsAsync(true);
            _mockOBSService.Setup(s => s.ConnectionStatus).Returns(expectedStatus);

            // Act
            var result = await _controller.Connect();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }

        [TestMethod]
        public async Task Connect_WhenFailed_ReturnsBadRequest()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OBS:WebSocketUrl"]).Returns("ws://localhost:4455");
            _mockConfiguration.Setup(c => c["OBS:Password"]).Returns((string?)null);
            _mockOBSService.Setup(s => s.ConnectAsync("ws://localhost:4455", null))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Connect();

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }

        [TestMethod]
        public async Task Disconnect_ReturnsOkMessage()
        {
            // Arrange
            _mockOBSService.Setup(s => s.DisconnectAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Disconnect();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }

        [TestMethod]
        public async Task GetScenes_ReturnsListOfScenes()
        {
            // Arrange
            var scenes = new List<OBSScene>
            {
                new() { Name = "Scene 1", IsActive = true, Index = 0 },
                new() { Name = "Scene 2", IsActive = false, Index = 1 }
            };
            _mockOBSService.Setup(s => s.GetScenesAsync()).ReturnsAsync(scenes);

            // Act
            var result = await _controller.GetScenes();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value as ScenesResponse;
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Scenes.Count);
            Assert.AreEqual("Scene 1", response.CurrentScene);
        }

        [TestMethod]
        public async Task SwitchScene_WhenSuccessful_ReturnsOk()
        {
            // Arrange
            var request = new SwitchSceneRequest { SceneName = "Scene 2" };
            _mockOBSService.Setup(s => s.SwitchSceneAsync("Scene 2")).ReturnsAsync(true);

            // Act
            var result = await _controller.SwitchScene(request);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }

        [TestMethod]
        public async Task SwitchScene_WhenFailed_ReturnsBadRequest()
        {
            // Arrange
            var request = new SwitchSceneRequest { SceneName = "NonExistent" };
            _mockOBSService.Setup(s => s.SwitchSceneAsync("NonExistent")).ReturnsAsync(false);

            // Act
            var result = await _controller.SwitchScene(request);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }

        [TestMethod]
        public async Task GetStreamingStatus_ReturnsCurrentStatus()
        {
            // Arrange
            var expectedStatus = new StreamingStatus
            {
                IsStreaming = true,
                StreamDurationSeconds = 300
            };
            _mockOBSService.Setup(s => s.GetStreamingStatusAsync()).ReturnsAsync(expectedStatus);

            // Act
            var result = await _controller.GetStreamingStatus();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(expectedStatus, okResult.Value);
        }

        [TestMethod]
        public async Task StartStreaming_WhenSuccessful_ReturnsOk()
        {
            // Arrange
            _mockOBSService.Setup(s => s.StartStreamingAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.StartStreaming();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }

        [TestMethod]
        public async Task StopStreaming_WhenSuccessful_ReturnsOk()
        {
            // Arrange
            _mockOBSService.Setup(s => s.StopStreamingAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.StopStreaming();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }
    }
}
