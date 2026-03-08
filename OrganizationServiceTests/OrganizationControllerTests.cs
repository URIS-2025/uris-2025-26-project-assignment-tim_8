using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using AnonymousAPI.Controllers;
using OrganizationService.Clients;

namespace OrganizationService.Tests.Controllers
{
    public class OrganizationControllerTests
    {
        private readonly Mock<IOrganizationRepository> _mockRepo;
        private readonly OrganizationController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public OrganizationControllerTests()
        {
            _mockRepo = new Mock<IOrganizationRepository>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new OrganizationController(_mockRepo.Object, _logger.Object);
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
        public void DeleteOrganization_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteOrganization(id);

            _mockRepo.Verify(r => r.DeleteOrganization(id), Times.Once);
        }
    }
}
