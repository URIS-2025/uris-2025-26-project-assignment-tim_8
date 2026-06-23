using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProblemService.Clients;
using ProblemService.Controllers;
using ProblemService.Data;
using ProblemService.Enums;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;
using Xunit;

namespace ProblemService.Tests
{
    public class ProblemControllerTests
    {
        private readonly Mock<IProblemRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProblemController _controller;
        private readonly Mock<LoggerServiceClient> _logger;
        private readonly Mock<ProblemBoxServiceClient> _boxClient;

        public ProblemControllerTests()
        {
            _mockRepo = new Mock<IProblemRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _boxClient = new Mock<ProblemBoxServiceClient>();
            _boxClient
                .Setup(c => c.IsBoxActiveAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            // Default: treat boxes as public (no password) so existing happy-path tests skip verify.
            _boxClient
                .Setup(c => c.HasPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _controller = new ProblemController(_mockRepo.Object, _mockMapper.Object, _logger.Object, _boxClient.Object);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        }

        // GET ALL
        [Fact]
        public void GetAllProblems_ReturnsOkResult_WithListOfProblems()
        {
            var problems = new List<ProblemDTO>
            {
                new ProblemDTO { Id = Guid.NewGuid(), Title = "Problem 1", Description = "Desc 1" },
                new ProblemDTO { Id = Guid.NewGuid(), Title = "Problem 2", Description = "Desc 2" }
            };
            _mockRepo.Setup(repo => repo.GetAllProblems()).Returns(problems);

            var result = _controller.GetAllProblems();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemDTO>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public void GetAllProblems_ReturnsOkResult_WithEmptyList()
        {
            _mockRepo.Setup(repo => repo.GetAllProblems()).Returns(new List<ProblemDTO>());

            var result = _controller.GetAllProblems();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // GET BY ID
        [Fact]
        public void GetProblemById_ReturnsOkResult_WithProblem()
        {
            var id = Guid.NewGuid();
            var problem = new ProblemDTO { Id = id, Title = "Problem 1", Description = "Desc 1" };
            _mockRepo.Setup(repo => repo.GetProblemById(id)).Returns(problem);

            var result = _controller.GetProblemById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemDTO>(okResult.Value);
            Assert.Equal(id, returnValue.Id);
        }

        [Fact]
        public void GetProblemById_ThrowsException_WhenProblemNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemById(id))
                     .Throws(new ArgumentException("Problem with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.GetProblemById(id));
        }

        // GET BY PROBLEMBOX ID
        [Fact]
        public void GetProblemsByProblemBoxId_ReturnsOkResult_WithListOfProblems()
        {
            var problemBoxId = Guid.NewGuid();
            var problems = new List<ProblemDTO>
            {
                new ProblemDTO { Id = Guid.NewGuid(), Title = "Problem 1", ProblemBoxId = problemBoxId },
                new ProblemDTO { Id = Guid.NewGuid(), Title = "Problem 2", ProblemBoxId = problemBoxId }
            };
            _mockRepo.Setup(repo => repo.GetProblemsByProblemBoxId(problemBoxId)).Returns(problems);

            var result = _controller.GetProblemsByProblemBoxId(problemBoxId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemDTO>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public void GetProblemsByProblemBoxId_ReturnsOkResult_WithEmptyList()
        {
            var problemBoxId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemsByProblemBoxId(problemBoxId))
                     .Returns(new List<ProblemDTO>());

            var result = _controller.GetProblemsByProblemBoxId(problemBoxId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        [Fact]
        public void GetProblemsByProblemBoxId_ReturnsEmptyList_WhenProblemBoxIdIsEmpty()
        {
            var problemBoxId = Guid.Empty;
            _mockRepo.Setup(repo => repo.GetProblemsByProblemBoxId(problemBoxId))
                     .Returns(new List<ProblemDTO>());

            var result = _controller.GetProblemsByProblemBoxId(problemBoxId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // POST

        [Fact]
        public async Task CreateProblem_ReturnsCreated_WhenBoxIsActive()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "New Problem",
                Description = "Desc",
                ProblemBoxId = Guid.NewGuid()
            };
            var created = new ProblemCreatedDTO { Id = Guid.NewGuid(), Title = "New Problem" };
            _mockRepo.Setup(repo => repo.CreateProblem(creationDTO)).Returns(created);

            var result = await _controller.CreateProblem(creationDTO);

            Assert.IsType<CreatedResult>(result.Result);
            _mockRepo.Verify(repo => repo.CreateProblem(It.IsAny<ProblemCreationDTO>()), Times.Once);
        }

        [Fact]
        public async Task CreateProblem_ReturnsConflict_WhenBoxIsNotActive()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "New Problem",
                Description = "Desc",
                ProblemBoxId = Guid.NewGuid()
            };
            _boxClient
                .Setup(c => c.IsBoxActiveAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.CreateProblem(creationDTO);

            Assert.IsType<ConflictObjectResult>(result.Result);
            _mockRepo.Verify(repo => repo.CreateProblem(It.IsAny<ProblemCreationDTO>()), Times.Never);
        }

        [Fact]
        public async Task CreateProblem_ReturnsUnauthorized_WhenBoxIsProtected_AndPasswordWrong()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "New Problem",
                Description = "Desc",
                ProblemBoxId = Guid.NewGuid(),
                BoxPassword = "wrong"
            };
            _boxClient
                .Setup(c => c.HasPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _boxClient
                .Setup(c => c.VerifyPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.CreateProblem(creationDTO);

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
            _mockRepo.Verify(repo => repo.CreateProblem(It.IsAny<ProblemCreationDTO>()), Times.Never);
        }

        [Fact]
        public async Task CreateProblem_ReturnsUnauthorized_WhenBoxIsProtected_AndPasswordMissing()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "New Problem",
                Description = "Desc",
                ProblemBoxId = Guid.NewGuid()
                // BoxPassword omitted
            };
            _boxClient
                .Setup(c => c.HasPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _boxClient
                .Setup(c => c.VerifyPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.CreateProblem(creationDTO);

            Assert.IsType<UnauthorizedObjectResult>(result.Result);
            _mockRepo.Verify(repo => repo.CreateProblem(It.IsAny<ProblemCreationDTO>()), Times.Never);
        }

        [Fact]
        public async Task CreateProblem_ReturnsCreated_WhenBoxIsProtected_AndPasswordCorrect()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "New Problem",
                Description = "Desc",
                ProblemBoxId = Guid.NewGuid(),
                BoxPassword = "correct"
            };
            var created = new ProblemCreatedDTO { Id = Guid.NewGuid(), Title = "New Problem" };
            _mockRepo.Setup(repo => repo.CreateProblem(creationDTO)).Returns(created);
            _boxClient
                .Setup(c => c.HasPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _boxClient
                .Setup(c => c.VerifyPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.CreateProblem(creationDTO);

            Assert.IsType<CreatedResult>(result.Result);
            _mockRepo.Verify(repo => repo.CreateProblem(It.IsAny<ProblemCreationDTO>()), Times.Once);
        }

        [Fact]
        public async Task CreateProblem_ReturnsCreated_WhenBoxIsPublic_WithoutVerifyingPassword()
        {
            var creationDTO = new ProblemCreationDTO
            {
                Title = "New Problem",
                Description = "Desc",
                ProblemBoxId = Guid.NewGuid()
            };
            var created = new ProblemCreatedDTO { Id = Guid.NewGuid(), Title = "New Problem" };
            _mockRepo.Setup(repo => repo.CreateProblem(creationDTO)).Returns(created);
            // HasPasswordAsync defaults to false in the ctor (public box).

            var result = await _controller.CreateProblem(creationDTO);

            Assert.IsType<CreatedResult>(result.Result);
            _mockRepo.Verify(repo => repo.CreateProblem(It.IsAny<ProblemCreationDTO>()), Times.Once);
            _boxClient.Verify(c => c.VerifyPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProblem_ThrowsException_WhenProblemNotFound()
        {
            var updateDTO = new ProblemUpdateDTO { Id = Guid.NewGuid() };
            _mockRepo.Setup(repo => repo.UpdateProblem(updateDTO))
                     .Throws(new ArgumentException("Problem with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblem(updateDTO));
        }

        [Fact]
        public async Task UpdateProblem_ThrowsException_WhenProblemBoxIdIsEmpty()
        {
            var updateDTO = new ProblemUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "Updated Problem",
                Description = "Updated Description",
                ProblemBoxId = Guid.Empty
            };
            _mockRepo.Setup(repo => repo.UpdateProblem(updateDTO))
                     .Throws(new ArgumentException("ProblemBoxId must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblem(updateDTO));
        }

        // DELETE

        [Fact]
        public async Task DeleteProblem_ThrowsException_WhenProblemNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblem(id))
                     .Throws(new ArgumentException("Problem with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteProblem(id));
        }
    }
}
