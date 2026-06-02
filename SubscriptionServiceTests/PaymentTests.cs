using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;
using SubscriptionService.Models.ExternalDTOs;
using AnonymousAPI.Controllers;
using SubscriptionService.Clients;
using SubscriptionService.ServiceCalls;


namespace SubscriptionServiceTests
{
    public class PaymentControllerTests
    {
        private readonly Mock<IPaymentRepository> _mockRepo;
        private readonly Mock<ISubscriptionRepository> _mockSubRepo;
        private readonly Mock<BillingServiceCall> _billing;
        private readonly PaymentController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public PaymentControllerTests()
        {
            _mockRepo = new Mock<IPaymentRepository>();
            _mockSubRepo = new Mock<ISubscriptionRepository>();
            _billing = new Mock<BillingServiceCall>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new PaymentController(_mockRepo.Object, _mockSubRepo.Object, _billing.Object, _logger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        // =====================
        // GET ALL PAYMENTS
        // =====================

        [Fact]
        public void GetAllPayments_ReturnsOk_WithListOfPayments()
        {
            var payments = new List<PaymentDTO>
            {
                new PaymentDTO { Id = Guid.NewGuid(), Total = 100.0, Status = "Completed" },
                new PaymentDTO { Id = Guid.NewGuid(), Total = 200.0, Status = "Pending" }
            };
            _mockRepo.Setup(r => r.GetAllPayments()).Returns(payments);

            var result = _controller.GetAllPayments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetAllPayments_ReturnsOk_WithEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllPayments()).Returns(new List<PaymentDTO>());

            var result = _controller.GetAllPayments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public void GetAllPayments_ReturnsExactData_FromRepository()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var payments = new List<PaymentDTO>
            {
                new PaymentDTO { Id = id1, Total = 111.11, Status = "Completed", Currency = "USD", PaymentMethod = "CreditCard" },
                new PaymentDTO { Id = id2, Total = 222.22, Status = "Pending",   Currency = "EUR", PaymentMethod = "PayPal" }
            };
            _mockRepo.Setup(r => r.GetAllPayments()).Returns(payments);

            var result = _controller.GetAllPayments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value).ToList();
            Assert.Equal(id1, returned[0].Id);
            Assert.Equal(111.11, returned[0].Total);
            Assert.Equal("Completed", returned[0].Status);
            Assert.Equal("USD", returned[0].Currency);
            Assert.Equal("CreditCard", returned[0].PaymentMethod);
            Assert.Equal(id2, returned[1].Id);
            Assert.Equal(222.22, returned[1].Total);
        }

        [Fact]
        public void GetAllPayments_CallsRepository_ExactlyOnce()
        {
            _mockRepo.Setup(r => r.GetAllPayments()).Returns(new List<PaymentDTO>());

            _controller.GetAllPayments();

            _mockRepo.Verify(r => r.GetAllPayments(), Times.Once);
        }

        [Fact]
        public void GetAllPayments_ThrowsException_WhenRepositoryFails()
        {
            _mockRepo.Setup(r => r.GetAllPayments()).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetAllPayments());
        }

        [Fact]
        public void GetAllPayments_ReturnsSingleItem_WhenOnlyOnePaymentExists()
        {
            var payments = new List<PaymentDTO>
            {
                new PaymentDTO { Id = Guid.NewGuid(), Total = 50.0, Status = "Completed" }
            };
            _mockRepo.Setup(r => r.GetAllPayments()).Returns(payments);

            var result = _controller.GetAllPayments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value);
            Assert.Single(returned);
        }

        // =====================
        // GET BY ID
        // =====================

        [Fact]
        public void GetPaymentById_ReturnsOk_WhenPaymentExists()
        {
            var id = Guid.NewGuid();
            var payment = new PaymentDTO { Id = id, Total = 150.0, Status = "Completed" };
            _mockRepo.Setup(r => r.GetPaymentById(id)).Returns(payment);

            var result = _controller.GetPaymentById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<PaymentDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
        }

        [Fact]
        public void GetPaymentById_ReturnsNotFound_WhenPaymentDoesNotExist()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentById(id)).Returns((PaymentDTO)null);

            var result = _controller.GetPaymentById(id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetPaymentById_ReturnsNotFound_WhenIdIsEmptyGuid()
        {
            _mockRepo.Setup(r => r.GetPaymentById(Guid.Empty)).Returns((PaymentDTO)null);

            var result = _controller.GetPaymentById(Guid.Empty);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetPaymentById_ReturnsCorrectData_AllFields()
        {
            var id = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();
            var payment = new PaymentDTO
            {
                Id = id,
                Total = 999.99,
                SubscriptionId = subscriptionId,
                Status = "Completed",
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            _mockRepo.Setup(r => r.GetPaymentById(id)).Returns(payment);

            var result = _controller.GetPaymentById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<PaymentDTO>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal(999.99, returned.Total);
            Assert.Equal(subscriptionId, returned.SubscriptionId);
            Assert.Equal("Completed", returned.Status);
            Assert.Equal("USD", returned.Currency);
            Assert.Equal("CreditCard", returned.PaymentMethod);
        }

        [Fact]
        public void GetPaymentById_CallsRepository_WithCorrectId()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentById(id)).Returns(new PaymentDTO { Id = id });

            _controller.GetPaymentById(id);

            _mockRepo.Verify(r => r.GetPaymentById(id), Times.Once);
        }

        [Fact]
        public void GetPaymentById_DoesNotCallRepository_WithDifferentId()
        {
            var id = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentById(id)).Returns(new PaymentDTO { Id = id });

            _controller.GetPaymentById(id);

            _mockRepo.Verify(r => r.GetPaymentById(otherId), Times.Never);
        }

        [Fact]
        public void GetPaymentById_ThrowsException_WhenRepositoryFails()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentById(id)).Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetPaymentById(id));
        }

        // =====================
        // GET BY SUBSCRIPTION ID
        // =====================

        [Fact]
        public void GetPaymentsBySubscriptionId_ReturnsOk_WithPayments()
        {
            var subscriptionId = Guid.NewGuid();
            var payments = new List<PaymentDTO>
            {
                new PaymentDTO { Id = Guid.NewGuid(), SubscriptionId = subscriptionId, Total = 99.0 },
                new PaymentDTO { Id = Guid.NewGuid(), SubscriptionId = subscriptionId, Total = 49.0 }
            };
            _mockRepo.Setup(r => r.GetPaymentsBySubscriptionId(subscriptionId)).Returns(payments);

            var result = _controller.GetPaymentsBySubscriptionId(subscriptionId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetPaymentsBySubscriptionId_ReturnsOk_WithEmptyList()
        {
            var subscriptionId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentsBySubscriptionId(subscriptionId))
                     .Returns(new List<PaymentDTO>());

            var result = _controller.GetPaymentsBySubscriptionId(subscriptionId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public void GetPaymentsBySubscriptionId_AllReturnedPayments_HaveCorrectSubscriptionId()
        {
            var subscriptionId = Guid.NewGuid();
            var payments = new List<PaymentDTO>
            {
                new PaymentDTO { Id = Guid.NewGuid(), SubscriptionId = subscriptionId, Total = 10.0 },
                new PaymentDTO { Id = Guid.NewGuid(), SubscriptionId = subscriptionId, Total = 20.0 },
                new PaymentDTO { Id = Guid.NewGuid(), SubscriptionId = subscriptionId, Total = 30.0 }
            };
            _mockRepo.Setup(r => r.GetPaymentsBySubscriptionId(subscriptionId)).Returns(payments);

            var result = _controller.GetPaymentsBySubscriptionId(subscriptionId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(okResult.Value);
            Assert.All(returned, p => Assert.Equal(subscriptionId, p.SubscriptionId));
        }

        [Fact]
        public void GetPaymentsBySubscriptionId_CallsRepository_WithCorrectId()
        {
            var subscriptionId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentsBySubscriptionId(subscriptionId))
                     .Returns(new List<PaymentDTO>());

            _controller.GetPaymentsBySubscriptionId(subscriptionId);

            _mockRepo.Verify(r => r.GetPaymentsBySubscriptionId(subscriptionId), Times.Once);
        }

        [Fact]
        public void GetPaymentsBySubscriptionId_WithEmptyGuid_ReturnsOk()
        {
            _mockRepo.Setup(r => r.GetPaymentsBySubscriptionId(Guid.Empty))
                     .Returns(new List<PaymentDTO>());

            var result = _controller.GetPaymentsBySubscriptionId(Guid.Empty);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void GetPaymentsBySubscriptionId_ThrowsException_WhenRepositoryFails()
        {
            var subscriptionId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetPaymentsBySubscriptionId(subscriptionId))
                     .Throws(new Exception("DB error"));

            Assert.Throws<Exception>(() => _controller.GetPaymentsBySubscriptionId(subscriptionId));
        }

        // =====================
        // CREATE PAYMENT
        // =====================

        [Fact]
        public void CreatePayment_CallsRepository_ExactlyOnce()
        {
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = Guid.NewGuid(),
                Total = 100.0,
                Currency = "EUR",
                PaymentMethod = "PayPal"
            };
            _mockRepo.Setup(r => r.CreatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = Guid.NewGuid(), Total = 100.0 });

            _controller.CreatePayment(dto);

            _mockRepo.Verify(r => r.CreatePayment(dto), Times.Once);
        }

        [Fact]
        public void CreatePayment_WithNegativeTotal_CallsRepository()
        {
            // Napomena: validacija negativnog Totala treba biti u repository/service sloju,
            // controller je samo proslje�uje - ovaj test provjerava da controller ne blokira
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = Guid.NewGuid(),
                Total = -100.0,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            _mockRepo.Setup(r => r.CreatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = Guid.NewGuid(), Total = -100.0 });

            _controller.CreatePayment(dto);

            _mockRepo.Verify(r => r.CreatePayment(dto), Times.Once);
        }

        [Fact]
        public void CreatePayment_WithEmptySubscriptionId_CallsRepository()
        {
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = Guid.Empty,
                Total = 100.0,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            _mockRepo.Setup(r => r.CreatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = Guid.NewGuid(), Total = 100.0 });

            _controller.CreatePayment(dto);

            _mockRepo.Verify(r => r.CreatePayment(dto), Times.Once);
        }

        [Fact]
        public void UpdatePayment_CallsRepository_ExactlyOnce()
        {
            var dto = new PaymentDTO { Id = Guid.NewGuid(), Total = 75.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = dto.Id });

            _controller.UpdatePayment(dto);

            _mockRepo.Verify(r => r.UpdatePayment(dto), Times.Once);
        }

        // =====================
        // DELETE PAYMENT
        // =====================

        [Fact]
        public void DeletePayment_CallsRepository_ExactlyOnce()
        {
            var id = Guid.NewGuid();

            _controller.DeletePayment(id);

            _mockRepo.Verify(r => r.DeletePayment(id), Times.Once);
        }

        [Fact]
        public void DeletePayment_WithEmptyGuid_CallsRepository()
        {
            _controller.DeletePayment(Guid.Empty);

            _mockRepo.Verify(r => r.DeletePayment(Guid.Empty), Times.Once);
        }

        [Fact]
        public void DeletePayment_CalledTwice_CallsRepository_Twice()
        {
            var id = Guid.NewGuid();

            _controller.DeletePayment(id);
            _controller.DeletePayment(id);

            _mockRepo.Verify(r => r.DeletePayment(id), Times.Exactly(2));
        }

        [Fact]
        public async Task DeletePayment_ThrowsException_WhenRepositoryFails()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeletePayment(id)).Throws(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => _controller.DeletePayment(id));
        }

        // =====================
        // BILLING PRODUCER (task 005)
        // =====================

        [Fact]
        public async Task CreatePayment_Success_NotifiesPayingOrg_Once()
        {
            var subId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var dto = new PaymentCreationDTO { SubscriptionId = subId, Total = 100.0, Currency = "EUR", PaymentMethod = "PayPal" };

            _mockRepo.Setup(r => r.CreatePayment(dto)).Returns(new PaymentCreatedDTO { Id = paymentId, Total = 100.0 });
            _mockSubRepo.Setup(s => s.GetSubscriptionById(subId)).Returns(new SubscriptionDTO { Id = subId, OrganizationId = orgId });

            var result = await _controller.CreatePayment(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            _billing.Verify(b => b.CreateBillingNotificationAsync(
                It.Is<BillingNotificationCreateDTO>(d => d.OrganizationId == orgId && d.PaymentId == paymentId),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePayment_Failure_NotifiesPayingOrg_Once_AndReturnsBadRequest()
        {
            var subId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var dto = new PaymentCreationDTO { SubscriptionId = subId, Total = 100.0, Currency = "EUR", PaymentMethod = "PayPal" };

            _mockRepo.Setup(r => r.CreatePayment(dto)).Throws(new Exception("gateway declined"));
            _mockSubRepo.Setup(s => s.GetSubscriptionById(subId)).Returns(new SubscriptionDTO { Id = subId, OrganizationId = orgId });

            var result = await _controller.CreatePayment(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            _billing.Verify(b => b.CreateBillingNotificationAsync(
                It.Is<BillingNotificationCreateDTO>(d => d.OrganizationId == orgId && d.Text.Contains("failed")),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePayment_SubscriptionMissing_SkipsNotification_StillCreated()
        {
            var subId = Guid.NewGuid();
            var dto = new PaymentCreationDTO { SubscriptionId = subId, Total = 100.0, Currency = "EUR", PaymentMethod = "PayPal" };

            _mockRepo.Setup(r => r.CreatePayment(dto)).Returns(new PaymentCreatedDTO { Id = Guid.NewGuid() });
            _mockSubRepo.Setup(s => s.GetSubscriptionById(subId)).Returns((SubscriptionDTO)null);

            var result = await _controller.CreatePayment(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            _billing.Verify(b => b.CreateBillingNotificationAsync(
                It.IsAny<BillingNotificationCreateDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreatePayment_BillingThrows_StillCreated()
        {
            var subId = Guid.NewGuid();
            var dto = new PaymentCreationDTO { SubscriptionId = subId, Total = 100.0, Currency = "EUR", PaymentMethod = "PayPal" };

            _mockRepo.Setup(r => r.CreatePayment(dto)).Returns(new PaymentCreatedDTO { Id = Guid.NewGuid() });
            _mockSubRepo.Setup(s => s.GetSubscriptionById(subId)).Returns(new SubscriptionDTO { Id = subId, OrganizationId = Guid.NewGuid() });
            _billing.Setup(b => b.CreateBillingNotificationAsync(
                It.IsAny<BillingNotificationCreateDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("billing down"));

            var result = await _controller.CreatePayment(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }
    }
}