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
    public class ProblemCategoryControllerTests
    {
        private readonly Mock<IProblemCategoryRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProblemCategoryController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public ProblemCategoryControllerTests()
        {
            _mockRepo = new Mock<IProblemCategoryRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new ProblemCategoryController(_mockRepo.Object, _mockMapper.Object, _logger.Object);
        }

        // GET ALL
        [Fact]
        public void GetAllProblemCategories_ReturnsOkResult_WithListOfCategories()
        {
            var categories = new List<ProblemCategoryDTO>
            {
                new ProblemCategoryDTO { Id = Guid.NewGuid(), Title = "Category 1", Description = "Desc 1" },
                new ProblemCategoryDTO { Id = Guid.NewGuid(), Title = "Category 2", Description = "Desc 2" }
            };
            _mockRepo.Setup(repo => repo.GetAllProblemCategories()).Returns(categories);

            var result = _controller.GetAllProblemCategories();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemCategoryDTO>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public void GetAllProblemCategories_ReturnsOkResult_WithEmptyList()
        {
            _mockRepo.Setup(repo => repo.GetAllProblemCategories()).Returns(new List<ProblemCategoryDTO>());

            var result = _controller.GetAllProblemCategories();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemCategoryDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // GET BY ID
        [Fact]
        public void GetProblemCategoryById_ReturnsOkResult_WithCategory()
        {
            var id = Guid.NewGuid();
            var category = new ProblemCategoryDTO { Id = id, Title = "Category 1", Description = "Desc 1" };
            _mockRepo.Setup(repo => repo.GetProblemCategoryById(id)).Returns(category);

            var result = _controller.GetProblemCategoryById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCategoryDTO>(okResult.Value);
            Assert.Equal(id, returnValue.Id);
        }

        [Fact]
        public void GetProblemCategoryById_ThrowsException_WhenCategoryNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemCategoryById(id))
                     .Throws(new ArgumentException("ProblemCategory with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.GetProblemCategoryById(id));
        }

        // POST

        [Fact]
        public async Task UpdateProblemCategory_ThrowsException_WhenCategoryNotFound()
        {
            var updateDTO = new ProblemCategoryUpdateDTO { Id = Guid.NewGuid() };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO))
                     .Throws(new ArgumentException("ProblemCategory with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemCategory(updateDTO));
        }

        [Fact]
        public async Task UpdateProblemCategory_ThrowsException_WhenTitleIsEmpty()
        {
            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "",
                Description = "Updated Description"
            };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO))
                     .Throws(new ArgumentException("Title must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemCategory(updateDTO));
        }

        [Fact]
        public async Task UpdateProblemCategory_ThrowsException_WhenDescriptionIsEmpty()
        {
            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "Updated Category",
                Description = ""
            };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO))
                     .Throws(new ArgumentException("Description must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemCategory(updateDTO));
        }

        // DELETE

        [Fact]
        public async Task DeleteProblemCategory_ThrowsException_WhenCategoryNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemCategory(id))
                     .Throws(new ArgumentException("ProblemCategory with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteProblemCategory(id));
        }
    }
}
