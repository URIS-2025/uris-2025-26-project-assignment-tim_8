using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
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

        public ProblemCommentControllerTests()
        {
            _mockRepo = new Mock<IProblemCommentRepository>();
            _mockMapper = new Mock<IMapper>();
            _controller = new ProblemCommentController(_mockRepo.Object, _mockMapper.Object);
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
        public void CreateProblemComment_ReturnsCreatedResult_WithCreatedComment()
        {
            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "New Comment",
                IsAnonymous = false,
                ProblemId = Guid.NewGuid(),
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            var createdDTO = new ProblemCommentCreatedDTO
            {
                Id = Guid.NewGuid(),
                CommentText = "New Comment",
                IsAnonymous = false
            };
            _mockRepo.Setup(repo => repo.CreateProblemComment(creationDTO)).Returns(createdDTO);

            var result = _controller.CreateProblemComment(creationDTO);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCommentCreatedDTO>(createdResult.Value);
            Assert.Equal(createdDTO.Id, returnValue.Id);
        }

        [Fact]
        public void CreateProblemComment_ThrowsException_WhenProblemIdIsEmpty()
        {
            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "New Comment",
                ProblemId = Guid.Empty,
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            _mockRepo.Setup(repo => repo.CreateProblemComment(creationDTO))
                     .Throws(new ArgumentException("ProblemId must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.CreateProblemComment(creationDTO));
        }

        [Fact]
        public void CreateProblemComment_ThrowsException_WhenAuthorIdIsEmpty()
        {
            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "New Comment",
                ProblemId = Guid.NewGuid(),
                ProblemCommentAuthorId = Guid.Empty
            };
            _mockRepo.Setup(repo => repo.CreateProblemComment(creationDTO))
                     .Throws(new ArgumentException("ProblemCommentAuthorId must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.CreateProblemComment(creationDTO));
        }

        [Fact]
        public void CreateProblemComment_ThrowsException_WhenCommentTextIsEmpty()
        {
            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "",
                ProblemId = Guid.NewGuid(),
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            _mockRepo.Setup(repo => repo.CreateProblemComment(creationDTO))
                     .Throws(new ArgumentException("CommentText must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.CreateProblemComment(creationDTO));
        }

        [Fact]
        public void CreateProblemComment_ReturnsCreatedResult_WithAnonymousComment()
        {
            var creationDTO = new ProblemCommentCreationDTO
            {
                CommentText = "Anonymous Comment",
                IsAnonymous = true,
                ProblemId = Guid.NewGuid(),
                ProblemCommentAuthorId = Guid.NewGuid()
            };
            var createdDTO = new ProblemCommentCreatedDTO
            {
                Id = Guid.NewGuid(),
                CommentText = "Anonymous Comment",
                IsAnonymous = true
            };
            _mockRepo.Setup(repo => repo.CreateProblemComment(creationDTO)).Returns(createdDTO);

            var result = _controller.CreateProblemComment(creationDTO);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCommentCreatedDTO>(createdResult.Value);
            Assert.True(returnValue.IsAnonymous);
        }

        // PUT
        [Fact]
        public void UpdateProblemComment_ReturnsOkResult_WithUpdatedComment()
        {
            var updateDTO = new ProblemCommentUpdateDTO
            {
                Id = Guid.NewGuid(),
                CommentText = "Updated Comment",
                IsAnonymous = false
            };
            var updatedDTO = new ProblemCommentDTO
            {
                Id = updateDTO.Id,
                CommentText = "Updated Comment",
                IsAnonymous = false
            };
            _mockRepo.Setup(repo => repo.UpdateProblemComment(updateDTO)).Returns(updatedDTO);

            var result = _controller.UpdateProblemComment(updateDTO);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCommentDTO>(okResult.Value);
            Assert.Equal(updateDTO.Id, returnValue.Id);
        }

        [Fact]
        public void UpdateProblemComment_ThrowsException_WhenCommentNotFound()
        {
            var updateDTO = new ProblemCommentUpdateDTO { Id = Guid.NewGuid() };
            _mockRepo.Setup(repo => repo.UpdateProblemComment(updateDTO))
                     .Throws(new ArgumentException("ProblemComment with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.UpdateProblemComment(updateDTO));
        }

        // DELETE
        [Fact]
        public void DeleteProblemComment_ReturnsNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemComment(id));

            var result = _controller.DeleteProblemComment(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeleteProblemComment_ThrowsException_WhenCommentNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemComment(id))
                     .Throws(new ArgumentException("ProblemComment with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.DeleteProblemComment(id));
        }
    }
}