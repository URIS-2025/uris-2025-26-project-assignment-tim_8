using AutoMapper;
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
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProblemCommentController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public ProblemCommentControllerTests()
        {
            _mockRepo = new Mock<IProblemCommentRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new ProblemCommentController(_mockRepo.Object, _mockMapper.Object, _logger.Object);
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
    }
}