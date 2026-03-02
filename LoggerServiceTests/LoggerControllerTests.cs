using LoggerService.Controllers;
using LoggerService.Data;
using LoggerService.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerServiceTests
{
    public class LoggerControllerTests
    {
        private readonly Mock<ILoggerRepository> _repoMock;
        private readonly LoggerController _controller;

        public LoggerControllerTests()
        {
            _repoMock = new Mock<ILoggerRepository>();
            _controller = new LoggerController(_repoMock.Object);
        }

        // ============ GET ALL ============

        [Fact]
        public void GetAll_WhenLogsExist_ReturnsOk()
        {
            // Arrange
            var fakeLogs = new List<LogDTO>
            {
                new LogDTO { Id = Guid.NewGuid(), Action = "TestAction", ServiceName = "LoggerService" },
                new LogDTO { Id = Guid.NewGuid(), Action = "TestAction2", ServiceName = "LoggerService" }
            };
            _repoMock.Setup(r => r.GetAll(100)).Returns(fakeLogs);

            // Act
            var result = _controller.GetAll(100);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAll_WhenNoLogs_ReturnsNoContent()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAll(100)).Returns(new List<LogDTO>());

            // Act
            var result = _controller.GetAll(100);

            // Assert
            Assert.IsType<NoContentResult>(result.Result);
        }

        // ============ GET BY ID ============

        [Fact]
        public void GetById_WhenLogExists_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fakeLog = new LogDTO { Id = id, Action = "TestAction" };
            _repoMock.Setup(r => r.GetById(id)).Returns(fakeLog);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetById_WhenLogDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((LogDTO?)null);

            // Act
            var result = _controller.GetById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ============ SEARCH ============

        [Fact]
        public void Search_WhenResultsExist_ReturnsOk()
        {
            // Arrange
            var fakeLogs = new List<LogDTO>
            {
                new LogDTO { Id = Guid.NewGuid(), Action = "Create", ServiceName = "LoggerService" }
            };
            _repoMock.Setup(r => r.Search(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
                .Returns(fakeLogs);

            // Act
            var result = _controller.Search(null, "Create", null, "LoggerService", null, null, null, null, 100);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void Search_WhenNoResults_ReturnsNoContent()
        {
            // Arrange
            _repoMock.Setup(r => r.Search(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
                .Returns(new List<LogDTO>());

            // Act
            var result = _controller.Search(null, null, null, null, null, null, null, null, 100);

            // Assert
            Assert.IsType<NoContentResult>(result.Result);
        }

        // ============ CREATE ============

        [Fact]
        public void Create_WhenValid_ReturnsCreated()
        {
            // Arrange
            var dto = new LogCreationDTO { Action = "Create", ServiceName = "LoggerService" };
            var fakeLog = new LogDTO { Id = Guid.NewGuid(), Action = "Create", ServiceName = "LoggerService" };
            _repoMock.Setup(r => r.Create(dto)).Returns(fakeLog);

            // Act
            var result = _controller.Create(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(createdResult.Value);
        }

        [Fact]
        public void Create_WhenInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var dto = new LogCreationDTO();
            _controller.ModelState.AddModelError("Action", "Action is required");

            // Act
            var result = _controller.Create(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // ============ DELETE ============

        [Fact]
        public void Delete_WhenLogExists_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var fakeLog = new LogDTO { Id = id, Action = "TestAction" };
            _repoMock.Setup(r => r.GetById(id)).Returns(fakeLog);

            // Act
            var result = _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void Delete_WhenLogDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((LogDTO?)null);

            // Act
            var result = _controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

