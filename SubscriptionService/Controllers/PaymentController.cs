using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Clients;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _repository;
        private readonly LoggerServiceClient _loggerClient;

        public PaymentController(IPaymentRepository repository, LoggerServiceClient loggerClient)
        {
            _repository = repository;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PaymentDTO>> GetAllPayments()
        {
            return Ok(_repository.GetAllPayments());
        }

        [HttpGet("{id}")]
        public ActionResult<PaymentDTO> GetPaymentById(Guid id)
        {
            var result = _repository.GetPaymentById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("bySubscription/{subscriptionId}")]
        public ActionResult<IEnumerable<PaymentDTO>> GetPaymentsBySubscriptionId(Guid subscriptionId)
        {
            return Ok(_repository.GetPaymentsBySubscriptionId(subscriptionId));
        }

        [HttpPost]
        public async Task<ActionResult<PaymentCreatedDTO>> CreatePayment([FromBody] PaymentCreationDTO dto)
        {
            try
            {
                var result = _repository.CreatePayment(dto);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PAYMENT",
                    EntityName = "Payment",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return CreatedAtAction(nameof(GetPaymentById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PAYMENT",
                    EntityName = "Payment",
                    IsSuccess = false,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<PaymentCreatedDTO>> UpdatePayment([FromBody] PaymentDTO dto)
        {
            try
            {
                var result = _repository.UpdatePayment(dto);
                if (result == null) return NotFound();

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PAYMENT",
                    EntityName = "Payment",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PAYMENT",
                    EntityName = "Payment",
                    IsSuccess = false,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            try
            {
                _repository.DeletePayment(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PAYMENT",
                    EntityName = "Payment",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PAYMENT",
                    EntityName = "Payment",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}