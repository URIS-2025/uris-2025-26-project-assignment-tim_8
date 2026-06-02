using AnonymousAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SubscriptionService.Clients;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;
using SubscriptionService.Models.ExternalDTOs;
using SubscriptionService.ServiceCalls;

public class SubscriptionControllerTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepo;
    private readonly Mock<BillingServiceCall> _billing;
    private readonly SubscriptionController _controller;
    private readonly Mock<LoggerServiceClient> _logger;
    public SubscriptionControllerTests()
    {
        _mockRepo = new Mock<ISubscriptionRepository>();
        _billing = new Mock<BillingServiceCall>();
        _logger = new Mock<LoggerServiceClient>();

        _controller = new SubscriptionController(_mockRepo.Object, _billing.Object, _logger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // =====================
    // GET ALL
    // =====================

    [Fact]
    public void GetAll_ReturnsOk_WithListOfSubscriptions()
    {
        var subscriptions = new List<SubscriptionDTO>
        {
            new SubscriptionDTO { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid() },
            new SubscriptionDTO { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid() }
        };
        _mockRepo.Setup(r => r.GetAllSubscriptions()).Returns(subscriptions);

        var result = _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionDTO>>(okResult.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public void GetAll_ReturnsOk_WithEmptyList()
    {
        _mockRepo.Setup(r => r.GetAllSubscriptions()).Returns(new List<SubscriptionDTO>());

        var result = _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionDTO>>(okResult.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public void GetAll_ReturnsExactData_FromRepository()
    {
        var id1 = Guid.NewGuid();
        var orgId1 = Guid.NewGuid();
        var planId1 = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);

        var subscriptions = new List<SubscriptionDTO>
        {
            new SubscriptionDTO
            {
                Id = id1,
                StartDate = start,
                EndDate = end,
                OrganizationId = orgId1,
                SubscriptionPlanId = planId1
            }
        };
        _mockRepo.Setup(r => r.GetAllSubscriptions()).Returns(subscriptions);

        var result = _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionDTO>>(okResult.Value).ToList();
        Assert.Equal(id1, returned[0].Id);
        Assert.Equal(start, returned[0].StartDate);
        Assert.Equal(end, returned[0].EndDate);
        Assert.Equal(orgId1, returned[0].OrganizationId);
        Assert.Equal(planId1, returned[0].SubscriptionPlanId);
    }

    [Fact]
    public void GetAll_CallsRepository_ExactlyOnce()
    {
        _mockRepo.Setup(r => r.GetAllSubscriptions()).Returns(new List<SubscriptionDTO>());

        _controller.GetAll();

        _mockRepo.Verify(r => r.GetAllSubscriptions(), Times.Once);
    }

    [Fact]
    public void GetAll_ReturnsSingleItem_WhenOnlyOneExists()
    {
        _mockRepo.Setup(r => r.GetAllSubscriptions()).Returns(new List<SubscriptionDTO>
        {
            new SubscriptionDTO { Id = Guid.NewGuid() }
        });

        var result = _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<SubscriptionDTO>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public void GetAll_ThrowsException_WhenRepositoryFails()
    {
        _mockRepo.Setup(r => r.GetAllSubscriptions()).Throws(new Exception("DB error"));

        Assert.Throws<Exception>(() => _controller.GetAll());
    }

    // =====================
    // GET BY ID
    // =====================

    [Fact]
    public void GetById_ReturnsOk_WhenSubscriptionExists()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns(new SubscriptionDTO { Id = id });

        var result = _controller.GetById(id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<SubscriptionDTO>(okResult.Value);
        Assert.Equal(id, returned.Id);
    }

    [Fact]
    public void GetById_ReturnsNotFound_WhenSubscriptionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns((SubscriptionDTO)null);

        var result = _controller.GetById(id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void GetById_ReturnsNotFound_WhenIdIsEmptyGuid()
    {
        _mockRepo.Setup(r => r.GetSubscriptionById(Guid.Empty)).Returns((SubscriptionDTO)null);

        var result = _controller.GetById(Guid.Empty);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void GetById_ReturnsCorrectData_AllFields()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var end = start.AddMonths(6);

        var subscription = new SubscriptionDTO
        {
            Id = id,
            StartDate = start,
            EndDate = end,
            OrganizationId = orgId,
            SubscriptionPlanId = planId
        };
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns(subscription);

        var result = _controller.GetById(id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<SubscriptionDTO>(okResult.Value);
        Assert.Equal(id, returned.Id);
        Assert.Equal(start, returned.StartDate);
        Assert.Equal(end, returned.EndDate);
        Assert.Equal(orgId, returned.OrganizationId);
        Assert.Equal(planId, returned.SubscriptionPlanId);
    }

    [Fact]
    public void GetById_CallsRepository_WithCorrectId()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns(new SubscriptionDTO { Id = id });

        _controller.GetById(id);

        _mockRepo.Verify(r => r.GetSubscriptionById(id), Times.Once);
    }

    [Fact]
    public void GetById_DoesNotCallRepository_WithDifferentId()
    {
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns(new SubscriptionDTO { Id = id });

        _controller.GetById(id);

        _mockRepo.Verify(r => r.GetSubscriptionById(otherId), Times.Never);
    }

    [Fact]
    public void GetById_ThrowsException_WhenRepositoryFails()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Throws(new Exception("DB error"));

        Assert.Throws<Exception>(() => _controller.GetById(id));
    }

    // =====================
    // UPDATE
    // =====================

    [Fact]
    public void Update_CallsRepository_ExactlyOnce()
    {
        var dto = new SubscriptionDTO { Id = Guid.NewGuid() };
        _mockRepo.Setup(r => r.UpdateSubscription(dto))
                 .Returns(new SubscriptionCreatedDTO { Id = dto.Id });

        _controller.Update(dto);

        _mockRepo.Verify(r => r.UpdateSubscription(dto), Times.Once);
    }

    [Fact]
    public void Update_WithStartDateAfterEndDate_CallsRepository()
    {
        var dto = new SubscriptionDTO
        {
            Id = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow
        };
        _mockRepo.Setup(r => r.UpdateSubscription(dto))
                 .Returns(new SubscriptionCreatedDTO { Id = dto.Id });

        _controller.Update(dto);

        _mockRepo.Verify(r => r.UpdateSubscription(dto), Times.Once);
    }

    // =====================
    // DELETE
    // =====================

    [Fact]
    public void Delete_CallsRepository_ExactlyOnce()
    {
        var id = Guid.NewGuid();

        _controller.Delete(id);

        _mockRepo.Verify(r => r.DeleteSubscription(id), Times.Once);
    }

    [Fact]
    public void Delete_WithEmptyGuid_CallsRepository()
    {
        _controller.Delete(Guid.Empty);

        _mockRepo.Verify(r => r.DeleteSubscription(Guid.Empty), Times.Once);
    }

    [Fact]
    public void Delete_CalledTwice_CallsRepository_Twice()
    {
        var id = Guid.NewGuid();

        _controller.Delete(id);
        _controller.Delete(id);

        _mockRepo.Verify(r => r.DeleteSubscription(id), Times.Exactly(2));
    }

    [Fact]
    public async Task Delete_ThrowsException_WhenRepositoryFails()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.DeleteSubscription(id)).Throws(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.Delete(id));
    }

    // =====================
    // CANCELLATION BILLING NOTIFICATION (task 005)
    // =====================

    [Fact]
    public async Task Delete_Cancellation_NotifiesOwningOrg_Once()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns(new SubscriptionDTO { Id = id, OrganizationId = orgId });

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        _mockRepo.Verify(r => r.DeleteSubscription(id), Times.Once);
        _billing.Verify(b => b.CreateBillingNotificationAsync(
            It.Is<BillingNotificationCreateDTO>(d => d.OrganizationId == orgId),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_SubscriptionMissing_SkipsNotification_StillDeletes()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns((SubscriptionDTO)null);

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        _mockRepo.Verify(r => r.DeleteSubscription(id), Times.Once);
        _billing.Verify(b => b.CreateBillingNotificationAsync(
            It.IsAny<BillingNotificationCreateDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_BillingThrows_StillDeletes()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetSubscriptionById(id)).Returns(new SubscriptionDTO { Id = id, OrganizationId = Guid.NewGuid() });
        _billing.Setup(b => b.CreateBillingNotificationAsync(
            It.IsAny<BillingNotificationCreateDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("billing down"));

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        _mockRepo.Verify(r => r.DeleteSubscription(id), Times.Once);
    }
}