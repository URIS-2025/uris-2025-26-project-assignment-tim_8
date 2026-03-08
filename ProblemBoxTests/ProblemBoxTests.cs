using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProblemBoxService.Clients;
using ProblemBoxService.Controllers;
using ProblemBoxService.Data;
using ProblemBoxService.Enums;
using ProblemBoxService.Models.DTOs;
using Xunit;

namespace ProblemBoxService.Tests
{
    public class ProblemBoxControllerTests
    {
        private readonly Mock<IProblemBoxRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProblemBoxController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public ProblemBoxControllerTests()
        {
            _mockRepo = new Mock<IProblemBoxRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new ProblemBoxController(_mockRepo.Object, _mockMapper.Object, _logger.Object);
        }

        // GET ALL
        [Fact]
        public void GetProblemBoxes_ReturnsOkResult_WithEmptyList()
        {
            var result = _controller.GetProblemBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemBoxDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // GET BY ORGANIZATION ID
        [Fact]
        public void GetProblemBoxByOrganizationId_ReturnsOkResult_WithListOfProblemBoxes()
        {
            var organizationId = Guid.NewGuid();
            var problemBoxes = new List<ProblemBoxDTO>
            {
                new ProblemBoxDTO { Id = Guid.NewGuid(), Name = "Box 1", OrganizationId = organizationId },
                new ProblemBoxDTO { Id = Guid.NewGuid(), Name = "Box 2", OrganizationId = organizationId }
            };
            _mockRepo.Setup(repo => repo.GetProblemBoxByOrganizationId(organizationId)).Returns(problemBoxes);

            var result = _controller.GetProblemBoxByOrganizationId(organizationId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemBoxDTO>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public void GetProblemBoxByOrganizationId_ReturnsOkResult_WithEmptyList()
        {
            var organizationId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemBoxByOrganizationId(organizationId))
                     .Returns(new List<ProblemBoxDTO>());

            var result = _controller.GetProblemBoxByOrganizationId(organizationId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemBoxDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        [Fact]
        public void GetProblemBoxByOrganizationId_ReturnsEmptyList_WhenOrganizationIdIsEmpty()
        {
            var organizationId = Guid.Empty;
            _mockRepo.Setup(repo => repo.GetProblemBoxByOrganizationId(organizationId))
                     .Returns(new List<ProblemBoxDTO>());

            var result = _controller.GetProblemBoxByOrganizationId(organizationId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ProblemBoxDTO>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        // GET BY ID
        [Fact]
        public void GetProblemBoxById_ReturnsOkResult_WithProblemBox()
        {
            var id = Guid.NewGuid();
            var problemBox = new ProblemBoxDTO { Id = id, Name = "Box 1" };
            _mockRepo.Setup(repo => repo.GetProblemBoxById(id)).Returns(problemBox);

            var result = _controller.GetProblemBoxById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemBoxDTO>(okResult.Value);
            Assert.Equal(id, returnValue.Id);
        }

        [Fact]
        public void GetProblemBoxById_ThrowsException_WhenProblemBoxNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemBoxById(id))
                     .Throws(new ArgumentException("ProblemBox with that Id does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.GetProblemBoxById(id));
        }

        // GET BY ACCESS LINK ID
        [Fact]
        public void GetProblemBoxByAccessLinkId_ReturnsOkResult_WithProblemBox()
        {
            var boxAccessLinkId = Guid.NewGuid();
            var problemBox = new ProblemBoxDTO { Id = Guid.NewGuid(), Name = "Box 1" };
            _mockRepo.Setup(repo => repo.GetProblemBoxByAccessLinkId(boxAccessLinkId)).Returns(problemBox);

            var result = _controller.GetProblemBoxByAccessLinkId(boxAccessLinkId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ProblemBoxDTO>(okResult.Value);
            Assert.NotNull(returnValue);
        }

        [Fact]
        public void GetProblemBoxByAccessLinkId_ThrowsException_WhenNotFound()
        {
            var boxAccessLinkId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetProblemBoxByAccessLinkId(boxAccessLinkId))
                     .Throws(new ArgumentException("ProblemBox with that AccessLinkId does not exist."));

            Assert.Throws<ArgumentException>(() => _controller.GetProblemBoxByAccessLinkId(boxAccessLinkId));
        }

        // POST
        [Fact]
        public async Task UpdateProblemBox_ThrowsException_WhenProblemBoxNotFound()
        {
            var updateDTO = new ProblemBoxUpdateDTO { Id = Guid.NewGuid() };
            _mockRepo.Setup(repo => repo.UpdateProblemBox(updateDTO))
                     .Throws(new ArgumentException("ProblemBox with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemBox(updateDTO));
        }

        [Fact]
        public async Task UpdateProblemBox_ThrowsException_WhenNameIsEmpty()
        {
            var updateDTO = new ProblemBoxUpdateDTO
            {
                Id = Guid.NewGuid(),
                Name = "",
                Description = "Updated Description",
                Password = "password123"
            };
            _mockRepo.Setup(repo => repo.UpdateProblemBox(updateDTO))
                     .Throws(new ArgumentException("Name must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemBox(updateDTO));
        }

        [Fact]
        public async Task UpdateProblemBox_ThrowsException_WhenDescriptionIsEmpty()
        {
            var updateDTO = new ProblemBoxUpdateDTO
            {
                Id = Guid.NewGuid(),
                Name = "Updated Box",
                Description = "",
                Password = "password123"
            };
            _mockRepo.Setup(repo => repo.UpdateProblemBox(updateDTO))
                     .Throws(new ArgumentException("Description must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemBox(updateDTO));
        }

        [Fact]
        public async Task UpdateProblemBox_ThrowsException_WhenPasswordIsEmpty()
        {
            var updateDTO = new ProblemBoxUpdateDTO
            {
                Id = Guid.NewGuid(),
                Name = "Updated Box",
                Description = "Updated Description",
                Password = ""
            };
            _mockRepo.Setup(repo => repo.UpdateProblemBox(updateDTO))
                     .Throws(new ArgumentException("Password must be provided."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateProblemBox(updateDTO));
        }

        // DELETE

        [Fact]
        public async Task DeleteProblemBox_ThrowsException_WhenProblemBoxNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteProblemBox(id))
                     .Throws(new ArgumentException("ProblemBox with that Id does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteProblemBox(id));
        }
    }
}