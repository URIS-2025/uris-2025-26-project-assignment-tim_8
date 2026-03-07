using AutoMapper;
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

        public ProblemControllerTests()
        {
            _mockRepo = new Mock<IProblemRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new ProblemController(_mockRepo.Object, _mockMapper.Object, _logger.Object);
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
