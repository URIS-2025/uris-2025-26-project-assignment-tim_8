using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AnonymousAPI.Controllers;
using SystemNotificationService.Data;
using SystemNotificationService.Models.DTOs.SystemNotification;
using Xunit;
using SystemNotificationService.Clients;

namespace SystemNotificationService.Tests
{
    public class SystemNotificationControllerTests
    {
        private readonly Mock<ISystemNotificationRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly SystemNotificationController _controller;
        private readonly Mock<LoggerServiceClient> _logger;
        public SystemNotificationControllerTests()
        {
            _mockRepo = new Mock<ISystemNotificationRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new SystemNotificationController(_mockRepo.Object, _mockMapper.Object, _logger.Object);
        }

        // GET ALL
        [Fact]
        public void GetNotifications_ReturnsOkResult_WithEmptyList()
        {
            var result = _controller.GetNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<SystemNotificationCreatedDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        [Fact]
        public async Task DeleteSystemNotification_ThrowsException_WhenNotificationNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteSystemNotification(id))
                     .Throws(new ArgumentException($"SystemNotification with ID {id} not found."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteSystemNotification(id));
        }
    }
}