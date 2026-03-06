using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Clients;
using SubscriptionService.Data;
using SubscriptionService.Enums;
using SubscriptionService.Models.DTOs;
using SubscriptionService.Models.ExternalDTOs;
using SubscriptionService.ServiceCalls;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionRepository _repository;
        private readonly BillingServiceCall _billingServiceCall;
        private readonly LoggerServiceClient _loggerClient;

        public SubscriptionController(ISubscriptionRepository repository, BillingServiceCall billingServiceCall, LoggerServiceClient loggerClient)
        {
            _repository = repository;
            _billingServiceCall = billingServiceCall;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SubscriptionDTO>> GetAll()
        {
            return Ok(_repository.GetAllSubscriptions());
        }

        [HttpGet("{id}")]
        public ActionResult<SubscriptionDTO> GetById(Guid id)
        {
            var result = _repository.GetSubscriptionById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<SubscriptionCreatedDTO>> Create([FromBody] SubscriptionCreationDTO dto)
        {
            try
            {
                var result = _repository.CreateSubscription(dto);

                await _billingServiceCall.CreateBillingNotificationAsync(new BillingNotificationCreateDTO
                {
                    Text = "Subscription successfully created.",
                    OrganizationId = dto.OrganizationId,
                    PaymentId = Guid.NewGuid(),
                    Type = TypeSubject.BillingNotification
                });

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUBSCRIPTION",
                    EntityName = "Subscription",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUBSCRIPTION",
                    EntityName = "Subscription",
                    IsSuccess = false,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<SubscriptionCreatedDTO>> Update([FromBody] SubscriptionDTO dto)
        {
            try
            {
                var result = _repository.UpdateSubscription(dto);
                if (result == null) return NotFound();

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUBSCRIPTION",
                    EntityName = "Subscription",
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
                    Action = "UPDATE_SUBSCRIPTION",
                    EntityName = "Subscription",
                    IsSuccess = false,
                    ServiceName = "SubscriptionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                _repository.DeleteSubscription(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUBSCRIPTION",
                    EntityName = "Subscription",
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
                    Action = "DELETE_SUBSCRIPTION",
                    EntityName = "Subscription",
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