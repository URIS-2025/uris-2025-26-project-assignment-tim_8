using Moq;
using Microsoft.AspNetCore.Mvc;
using SuggestionBoxService.Data;
using SuggestionBoxService.Models.DTOs;
using AnonymousAPI.Controllers;
using SuggestionBoxService.Clients;

namespace SuggestionBoxServiceTests
{
    public class SuggestionBoxControllerTests
    {
        private readonly Mock<ISuggestionBoxRepository> _mockRepo;
        private readonly SuggestionBoxController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public SuggestionBoxControllerTests()
        {
            _mockRepo = new Mock<ISuggestionBoxRepository>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new SuggestionBoxController(_mockRepo.Object, _logger.Object);
        }

        // =====================
        // GET ALL
        // =====================

        [Fact]
        public void GetAllSuggestionBoxes_ReturnsOk_WithListOfBoxes()
        {
            var boxes = new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO { Id = Guid.NewGuid(), Name = "Box 1", OrganizationId = Guid.NewGuid() },
                new SuggestionBoxDTO { Id = Guid.NewGuid(), Name = "Box 2", OrganizationId = Guid.NewGuid() }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(boxes);

            var result = _controller.GetAllSuggestionBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetAllSuggestionBoxes_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAll()).Returns(new List<SuggestionBoxDTO>());

            var result = _controller.GetAllSuggestionBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public void GetAllSuggestionBoxes_ReturnsExactData_FromRepository()
        {
            var id1 = Guid.NewGuid();
            var orgId1 = Guid.NewGuid();
            var accessLinkId1 = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            var boxes = new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO
                {
                    Id = id1,
                    Name = "Box 1",
                    Description = "First box",
                    IsDarkTheme = true,
                    CreatedAt = createdAt,
                    CreatedBy = "admin",
                    OrganizationId = orgId1,
                    BoxAccessLinkId = accessLinkId1
                }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(boxes);

            var result = _controller.GetAllSuggestionBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value).ToList();
            Assert.Equal(id1, returned[0].Id);
            Assert.Equal("Box 1", returned[0].Name);
            Assert.Equal("First box", returned[0].Description);
            Assert.True(returned[0].IsDarkTheme);
            Assert.Equal(createdAt, returned[0].CreatedAt);
            Assert.Equal("admin", returned[0].CreatedBy);
            Assert.Equal(orgId1, returned[0].OrganizationId);
            Assert.Equal(accessLinkId1, returned[0].BoxAccessLinkId);
        }

        [Fact]
        public void GetAllSuggestionBoxes_CallsRepository_ExactlyOnce()
        {
            _mockRepo.Setup(r => r.GetAll()).Returns(new List<SuggestionBoxDTO>());

            _controller.GetAllSuggestionBoxes();

            _mockRepo.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAllSuggestionBoxes_ReturnsSingleItem_WhenOnlyOneExists()
        {
            _mockRepo.Setup(r => r.GetAll()).Returns(new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO { Id = Guid.NewGuid(), Name = "Only Box" }
            });

            var result = _controller.GetAllSuggestionBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value);
            Assert.Single(returned);
        }

        [Fact]
        public void GetAllSuggestionBoxes_ReturnsOk_WithNullStringFields()
        {
            var boxes = new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO { Id = Guid.NewGuid(), Name = null, Description = null, CreatedBy = null }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(boxes);

            var result = _controller.GetAllSuggestionBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value).ToList();
            Assert.Null(returned[0].Name);
            Assert.Null(returned[0].Description);
            Assert.Null(returned[0].CreatedBy);
        }

        [Fact]
        public void GetAllSuggestionBoxes_ReturnsOk_WithIsDarkThemeFalse()
        {
            var boxes = new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO { Id = Guid.NewGuid(), IsDarkTheme = false }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(boxes);

            var result = _controller.GetAllSuggestionBoxes();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value).ToList();
            Assert.False(returned[0].IsDarkTheme);
        }

        [Fact]
        public void GetAllSuggestionBoxes_ThrowsException_WhenRepositoryFails()
        {
            _mockRepo.Setup(r => r.GetAll()).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetAllSuggestionBoxes());
        }

        // =====================
        // GET BY ID
        // =====================

        [Fact]
        public void GetById_ReturnsOk_WhenBoxExists()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(id))
                     .Returns(new SuggestionBoxDTO { Id = id, Name = "Test Box" });

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SuggestionBoxDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
        }

        [Fact]
        public void GetById_ReturnsNotFound_WhenBoxDoesNotExist()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(id)).Returns((SuggestionBoxDTO)null);

            var result = _controller.GetById(id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetById_ReturnsNotFound_WhenIdIsEmptyGuid()
        {
            _mockRepo.Setup(r => r.GetById(Guid.Empty)).Returns((SuggestionBoxDTO)null);

            var result = _controller.GetById(Guid.Empty);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetById_ReturnsCorrectData_AllFields()
        {
            var id = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var accessLinkId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            var box = new SuggestionBoxDTO
            {
                Id = id,
                Name = "My Box",
                Description = "Some description",
                IsDarkTheme = true,
                CreatedAt = createdAt,
                CreatedBy = "user1",
                OrganizationId = orgId,
                BoxAccessLinkId = accessLinkId
            };
            _mockRepo.Setup(r => r.GetById(id)).Returns(box);

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SuggestionBoxDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("My Box", returned.Name);
            Assert.Equal("Some description", returned.Description);
            Assert.True(returned.IsDarkTheme);
            Assert.Equal(createdAt, returned.CreatedAt);
            Assert.Equal("user1", returned.CreatedBy);
            Assert.Equal(orgId, returned.OrganizationId);
            Assert.Equal(accessLinkId, returned.BoxAccessLinkId);
        }

        [Fact]
        public void GetById_CallsRepository_WithCorrectId()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(id)).Returns(new SuggestionBoxDTO { Id = id });

            _controller.GetById(id);

            _mockRepo.Verify(r => r.GetById(id), Times.Once);
        }

        [Fact]
        public void GetById_DoesNotCallRepository_WithDifferentId()
        {
            var id = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(id)).Returns(new SuggestionBoxDTO { Id = id });

            _controller.GetById(id);

            _mockRepo.Verify(r => r.GetById(otherId), Times.Never);
        }

        [Fact]
        public void GetById_ReturnsOk_WithIsDarkThemeFalse()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(id))
                     .Returns(new SuggestionBoxDTO { Id = id, IsDarkTheme = false });

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SuggestionBoxDTO>(okResult.Value);
            Assert.False(returned.IsDarkTheme);
        }

        [Fact]
        public void GetById_ThrowsException_WhenRepositoryFails()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(id)).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetById(id));
        }

        // =====================
        // GET BY ORGANIZATION ID
        // =====================

        [Fact]
        public void GetByOrganizationId_ReturnsOk_WithBoxes()
        {
            var orgId = Guid.NewGuid();
            var boxes = new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Box A" },
                new SuggestionBoxDTO { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Box B" }
            };
            _mockRepo.Setup(r => r.GetByOrganizationId(orgId)).Returns(boxes);

            var result = _controller.GetByOrganizationId(orgId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetByOrganizationId_ReturnsOk_WithEmptyList()
        {
            var orgId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetByOrganizationId(orgId)).Returns(new List<SuggestionBoxDTO>());

            var result = _controller.GetByOrganizationId(orgId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public void GetByOrganizationId_AllReturnedBoxes_HaveCorrectOrganizationId()
        {
            var orgId = Guid.NewGuid();
            var boxes = new List<SuggestionBoxDTO>
            {
                new SuggestionBoxDTO { Id = Guid.NewGuid(), OrganizationId = orgId },
                new SuggestionBoxDTO { Id = Guid.NewGuid(), OrganizationId = orgId },
                new SuggestionBoxDTO { Id = Guid.NewGuid(), OrganizationId = orgId }
            };
            _mockRepo.Setup(r => r.GetByOrganizationId(orgId)).Returns(boxes);

            var result = _controller.GetByOrganizationId(orgId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SuggestionBoxDTO>>(okResult.Value);
            Assert.All(returned, b => Assert.Equal(orgId, b.OrganizationId));
        }

        [Fact]
        public void GetByOrganizationId_CallsRepository_WithCorrectId()
        {
            var orgId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetByOrganizationId(orgId)).Returns(new List<SuggestionBoxDTO>());

            _controller.GetByOrganizationId(orgId);

            _mockRepo.Verify(r => r.GetByOrganizationId(orgId), Times.Once);
        }

        [Fact]
        public void GetByOrganizationId_WithEmptyGuid_ReturnsOk()
        {
            _mockRepo.Setup(r => r.GetByOrganizationId(Guid.Empty)).Returns(new List<SuggestionBoxDTO>());

            var result = _controller.GetByOrganizationId(Guid.Empty);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetByOrganizationId_ThrowsException_WhenRepositoryFails()
        {
            var orgId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetByOrganizationId(orgId)).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetByOrganizationId(orgId));
        }

        // =====================
        // CREATE
        // =====================

        [Fact]
        public void Create_CallsRepository_ExactlyOnce()
        {
            var dto = new SuggestionBoxCreateDTO { Name = "Box", OrganizationId = Guid.NewGuid() };
            _mockRepo.Setup(r => r.Create(dto))
                     .Returns(new SuggestionBoxDTO { Id = Guid.NewGuid() });

            _controller.Create(dto);

            _mockRepo.Verify(r => r.Create(dto), Times.Once);
        }

  

        [Fact]
        public void Create_WithEmptyOrganizationId_CallsRepository()
        {
            var dto = new SuggestionBoxCreateDTO { Name = "Box", OrganizationId = Guid.Empty };
            _mockRepo.Setup(r => r.Create(dto))
                     .Returns(new SuggestionBoxDTO { Id = Guid.NewGuid() });

            _controller.Create(dto);

            _mockRepo.Verify(r => r.Create(dto), Times.Once);
        }

        // =====================
        // UPDATE
        // =====================

        [Fact]
        public void Update_CallsRepository_ExactlyOnce()
        {
            var dto = new SuggestionBoxUpdateDTO { Id = Guid.NewGuid(), Name = "Box" };
            _mockRepo.Setup(r => r.Update(dto))
                     .Returns(new SuggestionBoxDTO { Id = dto.Id });

            _controller.Update(dto);

            _mockRepo.Verify(r => r.Update(dto), Times.Once);
        }

        // =====================
        // DELETE
        // =====================

        [Fact]
        public void Delete_CallsRepository_ExactlyOnce()
        {
            var id = Guid.NewGuid();

            _controller.Delete(id);

            _mockRepo.Verify(r => r.Delete(id), Times.Once);
        }

        [Fact]
        public void Delete_WithEmptyGuid_CallsRepository()
        {
            _controller.Delete(Guid.Empty);

            _mockRepo.Verify(r => r.Delete(Guid.Empty), Times.Once);
        }

        [Fact]
        public void Delete_CalledTwice_CallsRepository_Twice()
        {
            var id = Guid.NewGuid();

            _controller.Delete(id);
            _controller.Delete(id);

            _mockRepo.Verify(r => r.Delete(id), Times.Exactly(2));
        }

        [Fact]
        public async Task Delete_ThrowsException_WhenRepositoryFails()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.Delete(id)).Throws(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => _controller.Delete(id));
        }

        // =====================
        // DELETE BY ORGANIZATION ID
        // =====================

        [Fact]
        public void DeleteByOrganizationId_CallsRepository_ExactlyOnce()
        {
            var orgId = Guid.NewGuid();

            _controller.DeleteByOrganizationId(orgId);

            _mockRepo.Verify(r => r.DeleteByOrganizationId(orgId), Times.Once);
        }

        [Fact]
        public void DeleteByOrganizationId_WithEmptyGuid_CallsRepository()
        {
            _controller.DeleteByOrganizationId(Guid.Empty);

            _mockRepo.Verify(r => r.DeleteByOrganizationId(Guid.Empty), Times.Once);
        }

        [Fact]
        public void DeleteByOrganizationId_CalledTwice_CallsRepository_Twice()
        {
            var orgId = Guid.NewGuid();

            _controller.DeleteByOrganizationId(orgId);
            _controller.DeleteByOrganizationId(orgId);

            _mockRepo.Verify(r => r.DeleteByOrganizationId(orgId), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteByOrganizationId_ThrowsException_WhenRepositoryFails()
        {
            var orgId = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeleteByOrganizationId(orgId)).Throws(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => _controller.DeleteByOrganizationId(orgId));
        }
    }
}