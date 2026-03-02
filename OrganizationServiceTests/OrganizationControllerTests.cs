using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using AnonymousAPI.Controllers;

namespace OrganizationService.Tests.Controllers
{
    public class OrganizationControllerTests
    {
        private readonly Mock<IOrganizationRepository> _mockRepo;
        private readonly OrganizationController _controller;

        public OrganizationControllerTests()
        {
            _mockRepo = new Mock<IOrganizationRepository>();
            _controller = new OrganizationController(_mockRepo.Object);
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────

        [Fact]
        public void GetAllOrganizations_ReturnsOk_WithList()
        {
            var organizations = new List<OrganizationDTO>
            {
                new OrganizationDTO { Id = Guid.NewGuid(), Name = "Org A", AdminId = Guid.NewGuid() },
                new OrganizationDTO { Id = Guid.NewGuid(), Name = "Org B", AdminId = Guid.NewGuid() }
            };
            _mockRepo.Setup(r => r.GetAllOrganizations()).Returns(organizations);

            var result = _controller.GetAllOrganizations();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<OrganizationDTO>>(okResult.Value);
            Assert.Equal(2, returnedList.Count());
        }

        [Fact]
        public void GetAllOrganizations_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllOrganizations()).Returns(new List<OrganizationDTO>());

            var result = _controller.GetAllOrganizations();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<OrganizationDTO>>(okResult.Value));
        }

        // ─── GET BY ID ─────────────────────────────────────────────────────────

        [Fact]
        public void GetOrganizationById_ReturnsOk_WhenFound()
        {
            var id      = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var org = new OrganizationDTO { Id = id, Name = "Test Org", AdminId = adminId };
            _mockRepo.Setup(r => r.GetOrganizationById(id)).Returns(org);

            var result = _controller.GetOrganizationById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<OrganizationDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Test Org", returned.Name);
            Assert.Equal(adminId, returned.AdminId);
        }

        [Fact]
        public void GetOrganizationById_ReturnsOk_WithNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetOrganizationById(id)).Returns((OrganizationDTO)null);

            var result = _controller.GetOrganizationById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

        // ─── CREATE ────────────────────────────────────────────────────────────

        [Fact]
        public void CreateOrganization_ReturnsCreated_WithCorrectData()
        {
            var adminId     = Guid.NewGuid();
            var creationDto = new OrganizationCreationDTO { Name = "New Org", AdminId = adminId };
            var createdDto  = new OrganizationCreatedDTO  { Id = Guid.NewGuid(), Name = "New Org" };
            _mockRepo.Setup(r => r.CreateOrganization(creationDto)).Returns(createdDto);

            var result = _controller.CreateOrganization(creationDto);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returned = Assert.IsType<OrganizationCreatedDTO>(createdResult.Value);
            Assert.Equal("New Org", returned.Name);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public void CreateOrganization_CallsRepository_Once()
        {
            var creationDto = new OrganizationCreationDTO { Name = "New Org", AdminId = Guid.NewGuid() };
            _mockRepo.Setup(r => r.CreateOrganization(It.IsAny<OrganizationCreationDTO>()))
                     .Returns(new OrganizationCreatedDTO());

            _controller.CreateOrganization(creationDto);

            _mockRepo.Verify(r => r.CreateOrganization(creationDto), Times.Once);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────

        [Fact]
        public void UpdateOrganization_ReturnsOk_WithUpdatedData()
        {
            var id         = Guid.NewGuid();
            var orgDto     = new OrganizationDTO        { Id = id, Name = "Updated Org", AdminId = Guid.NewGuid() };
            var updatedDto = new OrganizationCreatedDTO { Id = id, Name = "Updated Org" };
            _mockRepo.Setup(r => r.UpdateOrganization(orgDto)).Returns(updatedDto);

            var result = _controller.UpdateOrganization(orgDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<OrganizationCreatedDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Updated Org", returned.Name);
        }

        [Fact]
        public void UpdateOrganization_CallsRepository_Once()
        {
            var orgDto = new OrganizationDTO { Id = Guid.NewGuid(), Name = "Updated Org", AdminId = Guid.NewGuid() };
            _mockRepo.Setup(r => r.UpdateOrganization(It.IsAny<OrganizationDTO>()))
                     .Returns(new OrganizationCreatedDTO());

            _controller.UpdateOrganization(orgDto);

            _mockRepo.Verify(r => r.UpdateOrganization(orgDto), Times.Once);
        }

        // ─── DELETE ────────────────────────────────────────────────────────────

        [Fact]
        public void DeleteOrganization_ReturnsNoContent()
        {
            var result = _controller.DeleteOrganization(Guid.NewGuid());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeleteOrganization_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteOrganization(id);

            _mockRepo.Verify(r => r.DeleteOrganization(id), Times.Once);
        }
    }
}
