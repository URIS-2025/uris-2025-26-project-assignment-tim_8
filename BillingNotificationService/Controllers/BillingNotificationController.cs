using AnonymousDomain.Models.BillingNotification;
using BillingNotificationService.Clients;
using BillingNotificationService.Data;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BillingNotificationController : Controller
    {
        private readonly IBillingNotificationRepository _billingNotificationRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public BillingNotificationController(IBillingNotificationRepository billingNotificationRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _billingNotificationRepository = billingNotificationRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BillingNotificationDTO>> GetAllBillingNotifications()
        {
            var result = _billingNotificationRepository.GetAllBillingNotifications();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<BillingNotificationDTO> GetBillingNotificationById(Guid id)
        {
            var result = _billingNotificationRepository.GetBillingNotificationById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BillingNotificationCreatedDTO>> CreateBillingNotification([FromBody] BillingNotificationCreationDTO billingNotification)
        {
            try
            {
                var result = _billingNotificationRepository.CreateBillingNotification(billingNotification);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_BILLING_NOTIFICATION",
                    EntityName = "BillingNotification",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "BillingNotificationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_BILLING_NOTIFICATION",
                    EntityName = "BillingNotification",
                    IsSuccess = false,
                    ServiceName = "BillingNotificationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<BillingNotificationCreatedDTO>> UpdateBillingNotification([FromBody] BillingNotificationUpdateDTO billingNotification)
        {
            try
            {
                var result = _billingNotificationRepository.UpdateBillingNotification(billingNotification);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_BILLING_NOTIFICATION",
                    EntityName = "BillingNotification",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "BillingNotificationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_BILLING_NOTIFICATION",
                    EntityName = "BillingNotification",
                    IsSuccess = false,
                    ServiceName = "BillingNotificationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBillingNotification(Guid id)
        {
            try
            {
                _billingNotificationRepository.DeleteBillingNotification(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_BILLING_NOTIFICATION",
                    EntityName = "BillingNotification",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "BillingNotificationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_BILLING_NOTIFICATION",
                    EntityName = "BillingNotification",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "BillingNotificationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}