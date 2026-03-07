using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using BillingNotificationService.Data;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;
using AutoMapper;
using AnonymousAPI.Controllers;
using BillingNotificationService.Clients;

namespace BillingNotificationService.Tests.Controllers
{
    public class BillingNotificationControllerTests
    {
        private readonly Mock<IBillingNotificationRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<LoggerServiceClient> _logger;
        private readonly BillingNotificationController _controller;

        public BillingNotificationControllerTests()
        {
            _mockRepo   = new Mock<IBillingNotificationRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new BillingNotificationController(_mockRepo.Object, _mockMapper.Object, _logger.Object);
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllBillingNotifications_ReturnsOk_WithList()
        {
            var notifications = new List<BillingNotificationDTO>
            {
                new BillingNotificationDTO { Id = Guid.NewGuid(), Text = "Notif 1", IsRead = false, OrganizationId = Guid.NewGuid(), PaymentId = Guid.NewGuid() },
                new BillingNotificationDTO { Id = Guid.NewGuid(), Text = "Notif 2", IsRead = true,  OrganizationId = Guid.NewGuid(), PaymentId = Guid.NewGuid() }
            };
            _mockRepo.Setup(r => r.GetAllBillingNotifications()).Returns(notifications);

            var result = _controller.GetAllBillingNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<BillingNotificationDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetAllBillingNotifications_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllBillingNotifications()).Returns(new List<BillingNotificationDTO>());

            var result = _controller.GetAllBillingNotifications();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<BillingNotificationDTO>>(okResult.Value));
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetBillingNotificationById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var notification = new BillingNotificationDTO { Id = id, Text = "Test", IsRead = false, OrganizationId = Guid.NewGuid(), PaymentId = Guid.NewGuid() };
            _mockRepo.Setup(r => r.GetBillingNotificationById(id)).Returns(notification);

            var result = _controller.GetBillingNotificationById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<BillingNotificationDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Test", returned.Text);
        }

        [Fact]
        public void GetBillingNotificationById_ReturnsOk_WithNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBillingNotificationById(id)).Returns((BillingNotificationDTO)null);

            var result = _controller.GetBillingNotificationById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public void CreateBillingNotification_ReturnsCreated_WithCorrectData()
        {
            var creationDto = new BillingNotificationCreationDTO { Text = "Nova notifikacija", OrganizationId = Guid.NewGuid(), PaymentId = Guid.NewGuid() };
            var createdDto  = new BillingNotificationCreatedDTO  { Id = Guid.NewGuid(), Text = "Nova notifikacija", OrganizationId = creationDto.OrganizationId, PaymentId = creationDto.PaymentId };
            _mockRepo.Setup(r => r.CreateBillingNotification(creationDto)).Returns(createdDto);

            var result = _controller.CreateBillingNotification(creationDto);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returned = Assert.IsType<BillingNotificationCreatedDTO>(createdResult.Value);
            Assert.Equal("Nova notifikacija", returned.Text);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public void CreateBillingNotification_CallsRepository_Once()
        {
            var creationDto = new BillingNotificationCreationDTO { Text = "Test", OrganizationId = Guid.NewGuid(), PaymentId = Guid.NewGuid() };
            _mockRepo.Setup(r => r.CreateBillingNotification(It.IsAny<BillingNotificationCreationDTO>()))
                     .Returns(new BillingNotificationCreatedDTO());

            _controller.CreateBillingNotification(creationDto);

            _mockRepo.Verify(r => r.CreateBillingNotification(creationDto), Times.Once);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────────

        [Fact]
        public void UpdateBillingNotification_ReturnsOk_WithUpdatedData()
        {
            var id        = Guid.NewGuid();
            var updateDto = new BillingNotificationUpdateDTO { Id = id, Text = "Azurirana notifikacija", OrganizationId = Guid.NewGuid(), PaymentId = Guid.NewGuid() };
            var updatedDto = new BillingNotificationDTO { Id = id, Text = "Azurirana notifikacija", IsRead = false, OrganizationId = updateDto.OrganizationId, PaymentId = updateDto.PaymentId };
            _mockRepo.Setup(r => r.UpdateBillingNotification(updateDto)).Returns(updatedDto);

            var result = _controller.UpdateBillingNotification(updateDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<BillingNotificationDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Azurirana notifikacija", returned.Text);
        }

        [Fact]
        public void UpdateBillingNotification_CallsRepository_Once()
        {
            var updateDto = new BillingNotificationUpdateDTO { Id = Guid.NewGuid(), Text = "Test" };
            _mockRepo.Setup(r => r.UpdateBillingNotification(It.IsAny<BillingNotificationUpdateDTO>()))
                     .Returns(new BillingNotificationDTO());

            _controller.UpdateBillingNotification(updateDto);

            _mockRepo.Verify(r => r.UpdateBillingNotification(updateDto), Times.Once);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteBillingNotification_ReturnsNoContent()
        {
            var result = _controller.DeleteBillingNotification(Guid.NewGuid());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeleteBillingNotification_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteBillingNotification(id);

            _mockRepo.Verify(r => r.DeleteBillingNotification(id), Times.Once);
        }
    }
}
