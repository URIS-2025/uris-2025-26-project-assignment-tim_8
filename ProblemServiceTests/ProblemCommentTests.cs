using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProblemService.Clients;
using ProblemService.Controllers;
using ProblemService.Data;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;
using Xunit;

namespace ProblemService.Tests
{
    public class ProblemCommentControllerTests
    {
        private readonly Mock<IProblemCommentRepository> _mockRepo;
        private readonly Mock<IProblemRepository> _mockProblemRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProblemCommentController _controller;
        private readonly Mock<LoggerServiceClient> _logger;
        private readonly Mock<ProblemBoxServiceClient> _boxClient;
        private readonly Mock<SystemNotificationServiceClient> _notificationClient;

        public ProblemCommentControllerTests()
        {
            _mockRepo = new Mock<IProblemCommentRepository>();
            _mockProblemRepo = new Mock<IProblemRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _boxClient = new Mock<ProblemBoxServiceClient>();
            _notificationClient = new Mock<SystemNotificationServiceClient>();
            _controller = new ProblemCommentController(_mockRepo.Object, _mockProblemRepo.Object, _mockMapper.Object,
                _logger.Object, _boxClient.Object, _notificationClient.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        // GET ALL
        [Fact]
        public void GetAllProblemComments_ReturnsOkResult_WithListOfComments()
        {
            var comments = new List<ProblemCommentDTO>
            {
                new ProblemCommentDTO { Id = Guid.NewGuid(), CommentText = "Comment 1" },
                new ProblemCommentDTO { Id = Guid.NewGuid(), CommentText = "Comment 2" }
            };
            _mockRepo.Setup(repo => repo.GetAllProblemComments()).Returns(comments);

            var result = _controller.GetAllProblemComments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemCommentDTO>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public void GetAllProblemComments_ReturnsOkResult_WithEmptyList()
        {
            _mockRepo.Setup(repo => repo.GetAllProblemComments()).Returns(new List<ProblemCommentDTO>());

            var result = _controller.GetAllProblemComments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemCommentDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // GET BY ID
        [Fact]
        public void GetProblemCommentById_ReturnsOkResult_WithComment()
        {
            var id = Guid.NewGuid();
            var comment = new ProblemCommentDTO { Id = id, CommentText = "Comment 1" };
            _mockRepo.Setup(repo => repo.GetProblemCommentById(id)).Returns(comment);

            var result = _controller.GetProblemCommentById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCommentDTO>(okResult.Value);
            Assert.Equal(id, returnValue.Id);
        }

        [Fact]
        public void GetProblemCommentById_ThrowsException_WhenCommentNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemCommentById(id))
                     .Throws(new ArgumentException("ProblemComment with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.GetProblemCommentById(id));
        }

        // POST

        [Fact]
        public async Task UpdateProblemComment_ThrowsException_WhenCommentNotFound()
        {
            var updateDTO = new ProblemCommentUpdateDTO { Id = Guid.NewGuid() };
            _mockRepo.Setup(repo => repo.UpdateProblemComment(updateDTO))
                     .Throws(new ArgumentException("ProblemComment with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemComment(updateDTO));
        }

        // DELETE
        [Fact]
        public async Task DeleteProblemComment_ThrowsException_WhenCommentNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemComment(id))
                     .Throws(new ArgumentException("ProblemComment with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteProblemComment(id));
        }

        // NOTIFICATION PRODUCER (task 002)

        private ProblemCommentCreationDTO SampleCreationDTO() => new ProblemCommentCreationDTO
        {
            CommentText = "A reply",
            ProblemId = Guid.NewGuid()
        };

        [Fact]
        public async Task CreateProblemComment_SendsNotification_WithResolvedOrgAndCommentId()
        {
            var problemId = Guid.NewGuid();
            var boxId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var created = new ProblemCommentCreatedDTO { Id = Guid.NewGuid(), CommentText = "A reply", ProblemId = problemId };

            _mockRepo.Setup(r => r.CreateProblemComment(It.IsAny<ProblemCommentCreationDTO>())).Returns(created);
            _mockProblemRepo.Setup(r => r.GetProblemById(problemId))
                            .Returns(new ProblemDTO { Id = problemId, ProblemBoxId = boxId });
            _boxClient.Setup(b => b.TryGetOrganizationIdAsync(boxId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(orgId);

            var result = await _controller.CreateProblemComment(SampleCreationDTO());

            Assert.IsType<CreatedResult>(result.Result);
            _notificationClient.Verify(n => n.TryNotifyAsync(
                It.Is<SystemNotificationCreationDTO>(d =>
                    d.OrganizationId == orgId &&
                    d.ProblemCommentId == created.Id &&
                    !string.IsNullOrWhiteSpace(d.Text)),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateProblemComment_BoxMissing_SkipsNotification_StillCreated()
        {
            var problemId = Guid.NewGuid();
            var created = new ProblemCommentCreatedDTO { Id = Guid.NewGuid(), CommentText = "A reply", ProblemId = problemId };

            _mockRepo.Setup(r => r.CreateProblemComment(It.IsAny<ProblemCommentCreationDTO>())).Returns(created);
            _mockProblemRepo.Setup(r => r.GetProblemById(problemId))
                            .Returns(new ProblemDTO { Id = problemId, ProblemBoxId = Guid.NewGuid() });
            _boxClient.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Guid?)null);

            var result = await _controller.CreateProblemComment(SampleCreationDTO());

            Assert.IsType<CreatedResult>(result.Result);
            _notificationClient.Verify(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateProblemComment_NotificationThrows_StillCreated()
        {
            var problemId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var created = new ProblemCommentCreatedDTO { Id = Guid.NewGuid(), CommentText = "A reply", ProblemId = problemId };

            _mockRepo.Setup(r => r.CreateProblemComment(It.IsAny<ProblemCommentCreationDTO>())).Returns(created);
            _mockProblemRepo.Setup(r => r.GetProblemById(problemId))
                            .Returns(new ProblemDTO { Id = problemId, ProblemBoxId = Guid.NewGuid() });
            _boxClient.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(orgId);
            _notificationClient.Setup(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("notification service down"));

            var result = await _controller.CreateProblemComment(SampleCreationDTO());

            Assert.IsType<CreatedResult>(result.Result);
        }
    }
}