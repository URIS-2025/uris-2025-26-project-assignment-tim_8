using Moq;
using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;
using AnonymousAPI.Controllers;
using SubscriptionService.Clients;


namespace SubscriptionServiceTests
{
    public class PaymentControllerTests
    {
        private readonly Mock<IPaymentRepository> _mockRepo;
        private readonly PaymentController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public PaymentControllerTests()
        {
            _mockRepo = new Mock<IPaymentRepository>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new PaymentController(_mockRepo.Object, _logger.Object);
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
        public void CreatePayment_ReturnsCreatedAtAction_WithValidData()
        {
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = Guid.NewGuid(),
                Total = 250.0,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            var created = new PaymentCreatedDTO
            {
                Id = Guid.NewGuid(),
                Total = dto.Total,
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.CreatePayment(dto)).Returns(created);

            var result = _controller.CreatePayment(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetPaymentById), createdResult.ActionName);
            var returned = Assert.IsType<PaymentCreatedDTO>(createdResult.Value);
            Assert.Equal(created.Id, returned.Id);
            Assert.Equal(250.0, returned.Total);
        }

        [Fact]
        public void CreatePayment_ReturnsCorrectRouteValues()
        {
            var createdId = Guid.NewGuid();
            var dto = new PaymentCreationDTO { SubscriptionId = Guid.NewGuid(), Total = 50.0 };
            _mockRepo.Setup(r => r.CreatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = createdId, Total = 50.0 });

            var result = _controller.CreatePayment(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(createdId, createdResult.RouteValues["id"]);
        }

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
        public void CreatePayment_WithZeroTotal_ReturnsCreated()
        {
            var dto = new PaymentCreationDTO
            {
                SubscriptionId = Guid.NewGuid(),
                Total = 0.0,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            _mockRepo.Setup(r => r.CreatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = Guid.NewGuid(), Total = 0.0 });

            var result = _controller.CreatePayment(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<PaymentCreatedDTO>(createdResult.Value);
            Assert.Equal(0.0, returned.Total);
        }

        [Fact]
        public void CreatePayment_WithNegativeTotal_CallsRepository()
        {
            // Napomena: validacija negativnog Totala treba biti u repository/service sloju,
            // controller je samo prosljeðuje - ovaj test provjerava da controller ne blokira
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
        public void CreatePayment_ReturnsCreatedAt_WithValidCreatedAtDate()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var dto = new PaymentCreationDTO { SubscriptionId = Guid.NewGuid(), Total = 75.0 };
            var created = new PaymentCreatedDTO
            {
                Id = Guid.NewGuid(),
                Total = 75.0,
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.CreatePayment(dto)).Returns(created);

            var result = _controller.CreatePayment(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsType<PaymentCreatedDTO>(createdResult.Value);
            Assert.True(returned.CreatedAt > before);
            Assert.True(returned.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task CreatePayment_ThrowsException_WhenRepositoryFails()
        {
            var dto = new PaymentCreationDTO { SubscriptionId = Guid.NewGuid(), Total = 50.0 };
            _mockRepo.Setup(r => r.CreatePayment(dto)).Throws(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => _controller.CreatePayment(dto));
        }

        // =====================
        // UPDATE PAYMENT
        // =====================

        [Fact]
        public void UpdatePayment_ReturnsOk_WhenPaymentExists()
        {
            var dto = new PaymentDTO
            {
                Id = Guid.NewGuid(),
                Total = 300.0,
                Status = "Completed",
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };
            var updated = new PaymentCreatedDTO { Id = dto.Id, Total = 300.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto)).Returns(updated);

            var result = _controller.UpdatePayment(dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<PaymentCreatedDTO>(okResult.Value);
            Assert.Equal(dto.Id, returned.Id);
        }

        [Fact]
        public void UpdatePayment_ReturnsNotFound_WhenPaymentDoesNotExist()
        {
            var dto = new PaymentDTO { Id = Guid.NewGuid(), Total = 100.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto)).Returns((PaymentCreatedDTO)null);

            var result = _controller.UpdatePayment(dto);

            Assert.IsType<NotFoundResult>(result.Result);
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

        [Fact]
        public void UpdatePayment_ReturnsUpdatedTotal_Correctly()
        {
            var id = Guid.NewGuid();
            var dto = new PaymentDTO { Id = id, Total = 500.0, Status = "Completed" };
            var updated = new PaymentCreatedDTO { Id = id, Total = 500.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto)).Returns(updated);

            var result = _controller.UpdatePayment(dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<PaymentCreatedDTO>(okResult.Value);
            Assert.Equal(500.0, returned.Total);
        }

        [Fact]
        public void UpdatePayment_WithZeroTotal_ReturnsOk()
        {
            var dto = new PaymentDTO { Id = Guid.NewGuid(), Total = 0.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto))
                     .Returns(new PaymentCreatedDTO { Id = dto.Id, Total = 0.0 });

            var result = _controller.UpdatePayment(dto);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void UpdatePayment_WithEmptyGuidId_ReturnsNotFound()
        {
            var dto = new PaymentDTO { Id = Guid.Empty, Total = 100.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto)).Returns((PaymentCreatedDTO)null);

            var result = _controller.UpdatePayment(dto);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdatePayment_ThrowsException_WhenRepositoryFails()
        {
            var dto = new PaymentDTO { Id = Guid.NewGuid(), Total = 100.0 };
            _mockRepo.Setup(r => r.UpdatePayment(dto)).Throws(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() => _controller.UpdatePayment(dto));
        }

        // =====================
        // DELETE PAYMENT
        // =====================

        [Fact]
        public void DeletePayment_ReturnsNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();

            var result = _controller.DeletePayment(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeletePayment_CallsRepository_ExactlyOnce()
        {
            var id = Guid.NewGuid();

            _controller.DeletePayment(id);

            _mockRepo.Verify(r => r.DeletePayment(id), Times.Once);
        }

        [Fact]
        public void DeletePayment_ReturnsNoContent_ForNonExistentId()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeletePayment(id));

            var result = _controller.DeletePayment(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void DeletePayment_WithEmptyGuid_ReturnsNoContent()
        {
            var result = _controller.DeletePayment(Guid.Empty);

            Assert.IsType<NoContentResult>(result);
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
    }
}