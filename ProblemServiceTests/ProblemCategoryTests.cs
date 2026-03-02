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
    public class ProblemCategoryControllerTests
    {
        private readonly Mock<IProblemCategoryRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProblemCategoryController _controller;

        public ProblemCategoryControllerTests()
        {
            _mockRepo = new Mock<IProblemCategoryRepository>();
            _mockMapper = new Mock<IMapper>();
            _controller = new ProblemCategoryController(_mockRepo.Object, _mockMapper.Object);
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
        public void CreateProblemCategory_ReturnsCreatedResult_WithCreatedCategory()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "New Category",
                Description = "New Description"
            };
            var createdDTO = new ProblemCategoryCreatedDTO
            {
                Id = Guid.NewGuid(),
                Title = "New Category",
                Description = "New Description"
            };
            _mockRepo.Setup(repo => repo.CreateProblemCategory(creationDTO)).Returns(createdDTO);

            var result = _controller.CreateProblemCategory(creationDTO);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCategoryCreatedDTO>(createdResult.Value);
            Assert.Equal(createdDTO.Id, returnValue.Id);
        }

        [Fact]
        public void CreateProblemCategory_ThrowsException_WhenTitleIsEmpty()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "",
                Description = "New Description"
            };
            _mockRepo.Setup(repo => repo.CreateProblemCategory(creationDTO))
                     .Throws(new ArgumentException("Title must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.CreateProblemCategory(creationDTO));
        }

        [Fact]
        public void CreateProblemCategory_ThrowsException_WhenDescriptionIsEmpty()
        {
            var creationDTO = new ProblemCategoryCreationDTO
            {
                Title = "New Category",
                Description = ""
            };
            _mockRepo.Setup(repo => repo.CreateProblemCategory(creationDTO))
                     .Throws(new ArgumentException("Description must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.CreateProblemCategory(creationDTO));
        }

        // PUT
        [Fact]
        public void UpdateProblemCategory_ReturnsOkResult_WithUpdatedCategory()
        {
            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "Updated Category",
                Description = "Updated Description"
            };
            var updatedDTO = new ProblemCategoryDTO
            {
                Id = updateDTO.Id,
                Title = "Updated Category",
                Description = "Updated Description"
            };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO)).Returns(updatedDTO);

            var result = _controller.UpdateProblemCategory(updateDTO);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemCategoryDTO>(okResult.Value);
            Assert.Equal(updateDTO.Id, returnValue.Id);
        }

        [Fact]
        public void UpdateProblemCategory_ThrowsException_WhenCategoryNotFound()
        {
            var updateDTO = new ProblemCategoryUpdateDTO { Id = Guid.NewGuid() };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO))
                     .Throws(new ArgumentException("ProblemCategory with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.UpdateProblemCategory(updateDTO));
        }

        [Fact]
        public void UpdateProblemCategory_ThrowsException_WhenTitleIsEmpty()
        {
            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "",
                Description = "Updated Description"
            };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO))
                     .Throws(new ArgumentException("Title must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.UpdateProblemCategory(updateDTO));
        }

        [Fact]
        public void UpdateProblemCategory_ThrowsException_WhenDescriptionIsEmpty()
        {
            var updateDTO = new ProblemCategoryUpdateDTO
            {
                Id = Guid.NewGuid(),
                Title = "Updated Category",
                Description = ""
            };
            _mockRepo.Setup(repo => repo.UpdateProblemCategory(updateDTO))
                     .Throws(new ArgumentException("Description must be provided."));

            Assert.Throws<ArgumentException>(() => _controller.UpdateProblemCategory(updateDTO));
        }

        // DELETE
        [Fact]
        public void DeleteProblemCategory_ReturnsNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemCategory(id));

            var result = _controller.DeleteProblemCategory(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeleteProblemCategory_ThrowsException_WhenCategoryNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemCategory(id))
                     .Throws(new ArgumentException("ProblemCategory with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.DeleteProblemCategory(id));
        }
    }
}
