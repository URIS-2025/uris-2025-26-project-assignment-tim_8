using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AnonymousUserService.Models.DTOs.BoxAccessLink;
using AutoMapper;
using AnonymousAPI.Controllers;

namespace AnonymousUserService.Tests.Controllers
{
    public class AnonymousUserControllerTests
    {
        private readonly Mock<IAnonymousUserRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AnonymousUserController _controller;

        public AnonymousUserControllerTests()
        {
            _mockRepo   = new Mock<IAnonymousUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _controller = new AnonymousUserController(_mockRepo.Object, _mockMapper.Object);
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllAnonymousUsers_ReturnsOk_WithList()
        {
            var users = new List<AnonymousUserDTO>
            {
                new AnonymousUserDTO { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, BoxAccessLinkId = Guid.NewGuid() },
                new AnonymousUserDTO { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, BoxAccessLinkId = Guid.NewGuid() }
            };
            _mockRepo.Setup(r => r.GetAllAnonymousUsers()).Returns(users);

            var result = _controller.GetAllAnonymousUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AnonymousUserDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetAllAnonymousUsers_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllAnonymousUsers()).Returns(new List<AnonymousUserDTO>());

            var result = _controller.GetAllAnonymousUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<AnonymousUserDTO>>(okResult.Value));
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetAnonymousUserById_ReturnsOk_WhenFound()
        {
            var id   = Guid.NewGuid();
            var user = new AnonymousUserDTO { Id = id, CreatedAt = DateTime.UtcNow, BoxAccessLinkId = Guid.NewGuid() };
            _mockRepo.Setup(r => r.GetAnonymousUserById(id)).Returns(user);

            var result = _controller.GetAnonymousUserById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<AnonymousUserDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
        }

        [Fact]
        public void GetAnonymousUserById_ReturnsOk_WithNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetAnonymousUserById(id)).Returns((AnonymousUserDTO)null);

            var result = _controller.GetAnonymousUserById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteAnonymousUser_ReturnsNoContent()
        {
            var result = _controller.DeleteAnonymousUser(Guid.NewGuid());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeleteAnonymousUser_CallsRepository_Once()
        {
            var id = Guid.NewGuid();

            _controller.DeleteAnonymousUser(id);

            _mockRepo.Verify(r => r.DeleteAnonymousUser(id), Times.Once);
        }
    }

    public class BoxAccessLinkControllerTests
    {
        private readonly Mock<IBoxAccessLinkRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly BoxAccessLinkController _controller;

        public BoxAccessLinkControllerTests()
        {
            _mockRepo   = new Mock<IBoxAccessLinkRepository>();
            _mockMapper = new Mock<IMapper>();
            _controller = new BoxAccessLinkController(_mockRepo.Object, _mockMapper.Object);
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllBoxAccessLinks_ReturnsOk_WithList()
        {
            var links = new List<BoxAccessLinkDTO>
            {
                new BoxAccessLinkDTO { Id = Guid.NewGuid(), AccessToken = "token1", IsActive = true,  CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7) },
                new BoxAccessLinkDTO { Id = Guid.NewGuid(), AccessToken = "token2", IsActive = false, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(1) }
            };
            _mockRepo.Setup(r => r.GetAllBoxAccessLinks()).Returns(links);

            var result = _controller.GetAllBoxAccessLinks();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<BoxAccessLinkDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetAllBoxAccessLinks_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllBoxAccessLinks()).Returns(new List<BoxAccessLinkDTO>());

            var result = _controller.GetAllBoxAccessLinks();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<BoxAccessLinkDTO>>(okResult.Value));
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetBoxAccessLinkById_ReturnsOk_WhenFound()
        {
            var id   = Guid.NewGuid();
            var link = new BoxAccessLinkDTO { Id = id, AccessToken = "token1", IsActive = true, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7) };
            _mockRepo.Setup(r => r.GetBoxAccessLinkById(id)).Returns(link);

            var result = _controller.GetBoxAccessLinkById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<BoxAccessLinkDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("token1", returned.AccessToken);
            Assert.True(returned.IsActive);
        }

        [Fact]
        public void GetBoxAccessLinkById_ReturnsOk_WithNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBoxAccessLinkById(id)).Returns((BoxAccessLinkDTO)null);

            var result = _controller.GetBoxAccessLinkById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }
    }
}
