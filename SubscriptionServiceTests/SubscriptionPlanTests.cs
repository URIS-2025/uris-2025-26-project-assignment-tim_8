using Moq;
using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;
using AnonymousAPI.Controllers;

namespace SubscriptionServiceTests
{
    public class SubscriptionPlanControllerTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _mockRepo;
        private readonly SubscriptionPlanController _controller;

        public SubscriptionPlanControllerTests()
        {
            _mockRepo = new Mock<ISubscriptionPlanRepository>();
            _controller = new SubscriptionPlanController(_mockRepo.Object);
        }

        // =====================
        // GET ALL
        // =====================

        [Fact]
        public void GetAll_ReturnsOk_WithListOfPlans()
        {
            var plans = new List<SubscriptionPlanDTO>
            {
                new SubscriptionPlanDTO { Id = Guid.NewGuid(), Title = "Basic",    Description = "Basic plan" },
                new SubscriptionPlanDTO { Id = Guid.NewGuid(), Title = "Pro",      Description = "Pro plan" },
                new SubscriptionPlanDTO { Id = Guid.NewGuid(), Title = "Ultimate", Description = "Ultimate plan" }
            };
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(plans);

            var result = _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlanDTO>>(okResult.Value);
            Assert.Equal(3, returned.Count());
        }

        [Fact]
        public void GetAll_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(new List<SubscriptionPlanDTO>());

            var result = _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlanDTO>>(okResult.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public void GetAll_ReturnsExactData_FromRepository()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var plans = new List<SubscriptionPlanDTO>
            {
                new SubscriptionPlanDTO { Id = id1, Title = "Basic", Description = "Basic plan" },
                new SubscriptionPlanDTO { Id = id2, Title = "Pro",   Description = "Pro plan" }
            };
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(plans);

            var result = _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlanDTO>>(okResult.Value).ToList();
            Assert.Equal(id1, returned[0].Id);
            Assert.Equal("Basic", returned[0].Title);
            Assert.Equal("Basic plan", returned[0].Description);
            Assert.Equal(id2, returned[1].Id);
            Assert.Equal("Pro", returned[1].Title);
            Assert.Equal("Pro plan", returned[1].Description);
        }

        [Fact]
        public void GetAll_CallsRepository_ExactlyOnce()
        {
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(new List<SubscriptionPlanDTO>());

            _controller.GetAll();

            _mockRepo.Verify(r => r.GetAllSubscriptionPlans(), Times.Once);
        }

        [Fact]
        public void GetAll_ReturnsSingleItem_WhenOnlyOnePlanExists()
        {
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(new List<SubscriptionPlanDTO>
            {
                new SubscriptionPlanDTO { Id = Guid.NewGuid(), Title = "Basic" }
            });

            var result = _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlanDTO>>(okResult.Value);
            Assert.Single(returned);
        }

        [Fact]
        public void GetAll_ReturnsOk_WithNullTitleAndDescription()
        {
            var plans = new List<SubscriptionPlanDTO>
            {
                new SubscriptionPlanDTO { Id = Guid.NewGuid(), Title = null, Description = null }
            };
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(plans);

            var result = _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlanDTO>>(okResult.Value).ToList();
            Assert.Null(returned[0].Title);
            Assert.Null(returned[0].Description);
        }

        [Fact]
        public void GetAll_ReturnsOk_WithEmptyStringTitleAndDescription()
        {
            var plans = new List<SubscriptionPlanDTO>
            {
                new SubscriptionPlanDTO { Id = Guid.NewGuid(), Title = "", Description = "" }
            };
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(plans);

            var result = _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionPlanDTO>>(okResult.Value).ToList();
            Assert.Equal("", returned[0].Title);
            Assert.Equal("", returned[0].Description);
        }

        [Fact]
        public void GetAll_ThrowsException_WhenRepositoryFails()
        {
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetAll());
        }

        [Fact]
        public void GetAll_DoesNotCallGetById_WhenCallingGetAll()
        {
            _mockRepo.Setup(r => r.GetAllSubscriptionPlans()).Returns(new List<SubscriptionPlanDTO>());

            _controller.GetAll();

            _mockRepo.Verify(r => r.GetSubscriptionPlanById(It.IsAny<Guid>()), Times.Never);
        }

        // =====================
        // GET BY ID
        // =====================

        [Fact]
        public void GetById_ReturnsOk_WhenPlanExists()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id))
                     .Returns(new SubscriptionPlanDTO { Id = id, Title = "Basic" });

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SubscriptionPlanDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
        }

        [Fact]
        public void GetById_ReturnsNotFound_WhenPlanDoesNotExist()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id)).Returns((SubscriptionPlanDTO)null);

            var result = _controller.GetById(id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetById_ReturnsNotFound_WhenIdIsEmptyGuid()
        {
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(Guid.Empty)).Returns((SubscriptionPlanDTO)null);

            var result = _controller.GetById(Guid.Empty);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetById_ReturnsCorrectData_AllFields()
        {
            var id = Guid.NewGuid();
            var plan = new SubscriptionPlanDTO
            {
                Id = id,
                Title = "Ultimate",
                Description = "All features included"
            };
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id)).Returns(plan);

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SubscriptionPlanDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal("Ultimate", returned.Title);
            Assert.Equal("All features included", returned.Description);
        }

        [Fact]
        public void GetById_CallsRepository_WithCorrectId()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id))
                     .Returns(new SubscriptionPlanDTO { Id = id });

            _controller.GetById(id);

            _mockRepo.Verify(r => r.GetSubscriptionPlanById(id), Times.Once);
        }

        [Fact]
        public void GetById_DoesNotCallRepository_WithDifferentId()
        {
            var id = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id))
                     .Returns(new SubscriptionPlanDTO { Id = id });

            _controller.GetById(id);

            _mockRepo.Verify(r => r.GetSubscriptionPlanById(otherId), Times.Never);
        }

        [Fact]
        public void GetById_ReturnsOk_WithNullTitleAndDescription()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id))
                     .Returns(new SubscriptionPlanDTO { Id = id, Title = null, Description = null });

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SubscriptionPlanDTO>(okResult.Value);
            Assert.Null(returned.Title);
            Assert.Null(returned.Description);
        }

        [Fact]
        public void GetById_ReturnsOk_WithEmptyStringFields()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id))
                     .Returns(new SubscriptionPlanDTO { Id = id, Title = "", Description = "" });

            var result = _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SubscriptionPlanDTO>(okResult.Value);
            Assert.Equal("", returned.Title);
            Assert.Equal("", returned.Description);
        }

        [Fact]
        public void GetById_ThrowsException_WhenRepositoryFails()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id)).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetById(id));
        }

        [Fact]
        public void GetById_DoesNotCallGetAll_WhenCallingGetById()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetSubscriptionPlanById(id))
                     .Returns(new SubscriptionPlanDTO { Id = id });

            _controller.GetById(id);

            _mockRepo.Verify(r => r.GetAllSubscriptionPlans(), Times.Never);
        }
    }
}