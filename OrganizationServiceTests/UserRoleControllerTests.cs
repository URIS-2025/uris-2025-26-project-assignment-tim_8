using AnonymousAPI.Controllers;
using LoggerService.Data;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrganizationService.Clients;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using Xunit;

namespace OrganizationService.Tests.Controllers
{
    public class UserRoleControllerTests
    {
        private readonly Mock<IUserRoleRepository> _mockRepo;
        private readonly UserRoleController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public UserRoleControllerTests()
        {
            _mockRepo = new Mock<IUserRoleRepository>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new UserRoleController(_mockRepo.Object, _logger.Object);
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────

        [Fact]
        public void GetAllUserRoles_ReturnsOk_WithList()
        {
            var roles = new List<UserRoleDTO>
            {
                new UserRoleDTO { Id = Guid.NewGuid(), Title = "Admin",   Description = "Puni pristup" },
                new UserRoleDTO { Id = Guid.NewGuid(), Title = "Viewer",  Description = "Samo citanje" },
                new UserRoleDTO { Id = Guid.NewGuid(), Title = "Manager", Description = "Upravljanje timom" }
            };
            _mockRepo.Setup(r => r.GetAllUserRoles()).Returns(roles);

            var result = _controller.GetAllUserRoles();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<UserRoleDTO>>(okResult.Value);
            Assert.Equal(3, returnedList.Count());
        }

        [Fact]
        public void GetAllUserRoles_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllUserRoles()).Returns(new List<UserRoleDTO>());

            var result = _controller.GetAllUserRoles();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<UserRoleDTO>>(okResult.Value));
        }

        // ─── GET BY ID ─────────────────────────────────────────────────────────

        [Fact]
        public void GetUserRoleById_ReturnsOk_WhenFound()
        {
            var id   = Guid.NewGuid();
            var role = new UserRoleDTO { Id = id, Title = "Admin", Description = "Puni pristup" };
            _mockRepo.Setup(r => r.GetUserRoleById(id)).Returns(role);

            var result = _controller.GetUserRoleById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserRoleDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Admin", returned.Title);
            Assert.Equal("Puni pristup", returned.Description);
        }

        [Fact]
        public void GetUserRoleById_ReturnsOk_WithNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetUserRoleById(id)).Returns((UserRoleDTO)null);

            var result = _controller.GetUserRoleById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

        // ─── CREATE ────────────────────────────────────────────────────────────

        [Fact]
        public void CreateUserRole_CallsRepository_Once()
        {
            var creationDto = new UserRoleCreationDTO { Title = "Manager", Description = "Opis" };
            _mockRepo.Setup(r => r.CreateUserRole(It.IsAny<UserRoleCreationDTO>()))
                     .Returns(new UserRoleCreatedDTO());

            _controller.CreateUserRole(creationDto);

            _mockRepo.Verify(r => r.CreateUserRole(creationDto), Times.Once);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────
        [Fact]
        public void UpdateUserRole_CallsRepository_Once()
        {
            var roleDto = new UserRoleDTO { Id = Guid.NewGuid(), Title = "Senior Manager", Description = "Opis" };
            _mockRepo.Setup(r => r.UpdateUserRole(It.IsAny<UserRoleDTO>()))
                     .Returns(new UserRoleCreatedDTO());

            _controller.UpdateUserRole(roleDto);

            _mockRepo.Verify(r => r.UpdateUserRole(roleDto), Times.Once);
        }

        // ─── DELETE ────────────────────────────────────────────────────────────
        [Fact]
        public void DeleteUserRole_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteUserRole(id);

            _mockRepo.Verify(r => r.DeleteUserRole(id), Times.Once);
        }
    }
}
