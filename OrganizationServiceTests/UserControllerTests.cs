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
        public void CreateUser_ReturnsCreated_WithCorrectData()
        {
            var creationDto = new UserCreationDTO
            {
                Name           = "Marko",
                Surname        = "Markovic",
                Email          = "marko@test.com",
                Password       = "tajnaSifra123",
                Username       = "markom",
                RoleId         = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };
            var createdDto = new UserCreatedDTO
            {
                Id       = Guid.NewGuid(),
                Username = "markom",
                Email    = "marko@test.com"
            };
            _mockRepo.Setup(r => r.CreateUser(creationDto)).Returns(createdDto);

            var result = _controller.CreateUser(creationDto);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returned = Assert.IsType<UserCreatedDTO>(createdResult.Value);
            Assert.Equal("markom", returned.Username);
            Assert.Equal("marko@test.com", returned.Email);
            Assert.NotEqual(Guid.Empty, returned.Id);
        }

        [Fact]
        public void CreateUser_CallsRepository_Once()
        {
            var creationDto = new UserCreationDTO { Username = "markom", Email = "marko@test.com" };
            _mockRepo.Setup(r => r.CreateUser(It.IsAny<UserCreationDTO>())).Returns(new UserCreatedDTO());

            _controller.CreateUser(creationDto);

            _mockRepo.Verify(r => r.CreateUser(creationDto), Times.Once);
        }

        [Fact]
        public void CreateUser_WithNullOrganizationId_ReturnsCreated()
        {
            var creationDto = new UserCreationDTO
            {
                Name           = "Slobodan",
                Surname        = "Slobodic",
                Email          = "slobodan@test.com",
                Password       = "sifra123",
                Username       = "slobos",
                RoleId         = Guid.NewGuid(),
                OrganizationId = null   // korisnik bez organizacije
            };
            var createdDto = new UserCreatedDTO { Id = Guid.NewGuid(), Username = "slobos", Email = "slobodan@test.com" };
            _mockRepo.Setup(r => r.CreateUser(creationDto)).Returns(createdDto);

            var result = _controller.CreateUser(creationDto);

            Assert.IsType<CreatedResult>(result.Result);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────

        [Fact]
        public void UpdateUser_ReturnsOk_WithUpdatedData()
        {
            var id        = Guid.NewGuid();
            var updateDto = new UserUpdateDTO
            {
                Id             = id,
                Name           = "Marko Updated",
                Surname        = "Markovic Updated",
                Username       = "markom_new",
                RoleId         = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };
            var updatedDto = new UserCreatedDTO { Id = id, Username = "markom_new", Email = "marko@test.com" };
            _mockRepo.Setup(r => r.UpdateUser(updateDto)).Returns(updatedDto);

            var result = _controller.UpdateUser(updateDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserCreatedDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("markom_new", returned.Username);
        }

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
        public void DeleteUser_ReturnsNoContent()
        {
            var result = _controller.DeleteUser(Guid.NewGuid());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeleteUser_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteUser(id);

            _mockRepo.Verify(r => r.DeleteUser(id), Times.Once);
        }

        // ─── LOGIN ─────────────────────────────────────────────────────────────

        [Fact]
        public void Login_ReturnsOk_WithToken()
        {
            var loginDto = new UserLoginDTO { Username = "markom", Password = "tajnaSifra123" };
            _mockRepo.Setup(r => r.Login(loginDto)).Returns("fake-jwt-token");

            var result = _controller.Login(loginDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("fake-jwt-token", okResult.Value);
        }

        [Fact]
        public void Login_ReturnsOk_WithNull_WhenCredentialsInvalid()
        {
            var loginDto = new UserLoginDTO { Username = "nepoznat", Password = "pogresno" };
            _mockRepo.Setup(r => r.Login(loginDto)).Returns((string)null);

            var result = _controller.Login(loginDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

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
