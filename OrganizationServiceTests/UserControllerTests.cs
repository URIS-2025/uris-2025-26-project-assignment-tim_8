using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using AnonymousAPI.Controllers;
using OrganizationService.Clients;

namespace OrganizationService.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly UserController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public UserControllerTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new UserController(_mockRepo.Object, _logger.Object);
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────

        [Fact]
        public void GetAllUsers_ReturnsOk_WithList()
        {
            var users = new List<UserDTO>
            {
                new UserDTO
                {
                    Id             = Guid.NewGuid(),
                    Name           = "Marko",
                    Surname        = "Markovic",
                    Email          = "marko@test.com",
                    Username       = "markom",
                    CreatedAt      = DateTime.UtcNow,
                    RoleId         = Guid.NewGuid(),
                    OrganizationId = Guid.NewGuid()
                },
                new UserDTO
                {
                    Id             = Guid.NewGuid(),
                    Name           = "Ana",
                    Surname        = "Anic",
                    Email          = "ana@test.com",
                    Username       = "anaa",
                    CreatedAt      = DateTime.UtcNow,
                    RoleId         = Guid.NewGuid(),
                    OrganizationId = null
                }
            };
            _mockRepo.Setup(r => r.GetAllUsers()).Returns(users);

            var result = _controller.GetAllUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value);
            Assert.Equal(2, returnedList.Count());
        }

        [Fact]
        public void GetAllUsers_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllUsers()).Returns(new List<UserDTO>());

            var result = _controller.GetAllUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value));
        }

        // ─── GET BY ID ─────────────────────────────────────────────────────────

        [Fact]
        public void GetUserById_ReturnsOk_WhenFound()
        {
            var id   = Guid.NewGuid();
            var user = new UserDTO
            {
                Id             = id,
                Name           = "Marko",
                Surname        = "Markovic",
                Email          = "marko@test.com",
                Username       = "markom",
                CreatedAt      = DateTime.UtcNow,
                RoleId         = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };
            _mockRepo.Setup(r => r.GetUserById(id)).Returns(user);

            var result = _controller.GetUserById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Marko", returned.Name);
            Assert.Equal("Markovic", returned.Surname);
            Assert.Equal("marko@test.com", returned.Email);
        }

        [Fact]
        public void GetUserById_ReturnsOk_WithNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetUserById(id)).Returns((UserDTO)null);

            var result = _controller.GetUserById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

        // ─── CREATE ────────────────────────────────────────────────────────────

        [Fact]
        public void CreateUser_CallsRepository_Once()
        {
            var creationDto = new UserCreationDTO { Username = "markom", Email = "marko@test.com" };
            _mockRepo.Setup(r => r.CreateUser(It.IsAny<UserCreationDTO>())).Returns(new UserCreatedDTO());

            _controller.CreateUser(creationDto);

            _mockRepo.Verify(r => r.CreateUser(creationDto), Times.Once);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────

        [Fact]
        public void UpdateUser_CallsRepository_Once()
        {
            var updateDto = new UserUpdateDTO { Id = Guid.NewGuid(), Name = "Marko", Username = "markom" };
            _mockRepo.Setup(r => r.UpdateUser(It.IsAny<UserUpdateDTO>())).Returns(new UserCreatedDTO());

            _controller.UpdateUser(updateDto);

            _mockRepo.Verify(r => r.UpdateUser(updateDto), Times.Once);
        }

        // ─── DELETE ────────────────────────────────────────────────────────────

        [Fact]
        public void DeleteUser_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteUser(id);

            _mockRepo.Verify(r => r.DeleteUser(id), Times.Once);
        }

        // ─── LOGIN ─────────────────────────────────────────────────────────────

        [Fact]
        public void Login_CallsRepository_Once()
        {
            var loginDto = new UserLoginDTO { Username = "markom", Password = "tajnaSifra123" };
            _mockRepo.Setup(r => r.Login(It.IsAny<UserLoginDTO>())).Returns("token");

            _controller.Login(loginDto);

            _mockRepo.Verify(r => r.Login(loginDto), Times.Once);
        }
    }
}
