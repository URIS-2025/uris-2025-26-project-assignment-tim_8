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
            var returnValue = Assert.IsType<List<SystemNotificationCreatedDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // POST - with ProblemCommentId
        [Fact]
        public void CreateSystemNotification_ReturnsCreatedResult_WithProblemCommentId()
        {
            var creationDTO = new SystemNotificationCreationDTO
            {
                Text = "New Notification",
                OrganizationId = Guid.NewGuid(),
                AnonymousUserId = Guid.NewGuid(),
                ProblemCommentId = Guid.NewGuid(),
                SuggestionCommentId = null
            };
            var createdDTO = new SystemNotificationCreatedDTO
            {
                Id = Guid.NewGuid(),
                Text = "New Notification",
                CreatedAt = DateTime.UtcNow,
                OrganizationId = creationDTO.OrganizationId,
                AnonymousUserId = creationDTO.AnonymousUserId,
                ProblemCommentId = creationDTO.ProblemCommentId,
                SuggestionCommentId = null
            };
            _mockRepo.Setup(repo => repo.CreateSystemNotification(creationDTO)).Returns(createdDTO);

            var result = _controller.CreateSystemNotification(creationDTO);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returnValue = Assert.IsType<SystemNotificationCreatedDTO>(createdResult.Value);
            Assert.Equal(createdDTO.Id, returnValue.Id);
            Assert.NotNull(returnValue.ProblemCommentId);
            Assert.Null(returnValue.SuggestionCommentId);
        }

        // POST - with SuggestionCommentId
        [Fact]
        public void CreateSystemNotification_ReturnsCreatedResult_WithSuggestionCommentId()
        {
            var creationDTO = new SystemNotificationCreationDTO
            {
                Text = "New Notification",
                OrganizationId = Guid.NewGuid(),
                AnonymousUserId = Guid.NewGuid(),
                ProblemCommentId = null,
                SuggestionCommentId = Guid.NewGuid()
            };
            var createdDTO = new SystemNotificationCreatedDTO
            {
                Id = Guid.NewGuid(),
                Text = "New Notification",
                CreatedAt = DateTime.UtcNow,
                OrganizationId = creationDTO.OrganizationId,
                AnonymousUserId = creationDTO.AnonymousUserId,
                ProblemCommentId = null,
                SuggestionCommentId = creationDTO.SuggestionCommentId
            };
            _mockRepo.Setup(repo => repo.CreateSystemNotification(creationDTO)).Returns(createdDTO);

            var result = _controller.CreateSystemNotification(creationDTO);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returnValue = Assert.IsType<SystemNotificationCreatedDTO>(createdResult.Value);
            Assert.Equal(createdDTO.Id, returnValue.Id);
            Assert.Null(returnValue.ProblemCommentId);
            Assert.NotNull(returnValue.SuggestionCommentId);
        }

        // POST - both null
        [Fact]
        public async Task CreateSystemNotification_ThrowsException_WhenBothIdsAreNull()
        {
            var creationDTO = new SystemNotificationCreationDTO
            {
                Text = "New Notification",
                OrganizationId = Guid.NewGuid(),
                AnonymousUserId = Guid.NewGuid(),
                ProblemCommentId = null,
                SuggestionCommentId = null
            };
            _mockRepo.Setup(repo => repo.CreateSystemNotification(creationDTO))
                     .Throws(new ArgumentException(
                         "Either ProblemCommentId or SuggestionCommentId must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.CreateSystemNotification(creationDTO));
        }

        // POST - both provided
        [Fact]
        public async Task CreateSystemNotification_ThrowsException_WhenBothIdsAreProvided()
        {
            var creationDTO = new SystemNotificationCreationDTO
            {
                Text = "New Notification",
                OrganizationId = Guid.NewGuid(),
                AnonymousUserId = Guid.NewGuid(),
                ProblemCommentId = Guid.NewGuid(),
                SuggestionCommentId = Guid.NewGuid()
            };
            _mockRepo.Setup(repo => repo.CreateSystemNotification(creationDTO))
                     .Throws(new ArgumentException(
                         "Cannot provide both ProblemCommentId and SuggestionCommentId. Choose one."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.CreateSystemNotification(creationDTO));
        }

        // POST - without OrganizationId and AnonymousUserId (both optional)
        [Fact]
        public void CreateSystemNotification_ReturnsCreatedResult_WithoutOptionalFields()
        {
            var creationDTO = new SystemNotificationCreationDTO
            {
                Text = "New Notification",
                OrganizationId = null,
                AnonymousUserId = null,
                ProblemCommentId = Guid.NewGuid(),
                SuggestionCommentId = null
            };
            var createdDTO = new SystemNotificationCreatedDTO
            {
                Id = Guid.NewGuid(),
                Text = "New Notification",
                CreatedAt = DateTime.UtcNow,
                OrganizationId = null,
                AnonymousUserId = null,
                ProblemCommentId = creationDTO.ProblemCommentId,
                SuggestionCommentId = null
            };
            _mockRepo.Setup(repo => repo.CreateSystemNotification(creationDTO)).Returns(createdDTO);

            var result = _controller.CreateSystemNotification(creationDTO);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returnValue = Assert.IsType<SystemNotificationCreatedDTO>(createdResult.Value);
            Assert.Null(returnValue.OrganizationId);
            Assert.Null(returnValue.AnonymousUserId);
        }

        // DELETE
        [Fact]
        public void DeleteSystemNotification_ReturnsNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteSystemNotification(id));

            var result = _controller.DeleteSystemNotification(id);

            Assert.IsType<NoContentResult>(result);
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