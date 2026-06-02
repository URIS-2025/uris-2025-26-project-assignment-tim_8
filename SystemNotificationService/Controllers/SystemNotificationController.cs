using AnonymousDomain.Models.SystemNotification;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SystemNotificationService.Clients;
using SystemNotificationService.Data;
using SystemNotificationService.Models.DTOs.SystemNotification;

namespace AnonymousAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SystemNotificationController : Controller
    {
        private readonly ISystemNotificationRepository _systemNotificationRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public SystemNotificationController(ISystemNotificationRepository notificationRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _systemNotificationRepository = notificationRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SystemNotificationCreatedDTO>> GetNotifications()
        {
            var result = _systemNotificationRepository.GetAllSystemNotifications();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<SystemNotificationCreatedDTO>> CreateSystemNotification([FromBody] SystemNotificationCreationDTO notification)
        {
            try
            {
                var result = _systemNotificationRepository.CreateSystemNotification(notification);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SYSTEM_NOTIFICATION",
                    EntityName = "SystemNotification",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SystemNotificationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SYSTEM_NOTIFICATION",
                    EntityName = "SystemNotification",
                    IsSuccess = false,
                    ServiceName = "SystemNotificationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSystemNotification(Guid id)
        {
            try
            {
                _systemNotificationRepository.DeleteSystemNotification(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SYSTEM_NOTIFICATION",
                    EntityName = "SystemNotification",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "SystemNotificationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SYSTEM_NOTIFICATION",
                    EntityName = "SystemNotification",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "SystemNotificationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}