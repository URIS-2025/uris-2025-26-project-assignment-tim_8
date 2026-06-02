using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrganizationService.Clients;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly LoggerServiceClient _loggerClient;
        private readonly SystemNotificationServiceClient _notificationClient;

        public UserController(IUserRepository userRepository, LoggerServiceClient loggerClient,
            SystemNotificationServiceClient notificationClient)
        {
            _userRepository = userRepository;
            _loggerClient = loggerClient;
            _notificationClient = notificationClient;
        }

        [Authorize]
        [HttpGet]
        public ActionResult<IEnumerable<UserDTO>> GetAllUsers()
        {
            var result = _userRepository.GetAllUsers();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public ActionResult<UserDTO> GetUserById(Guid id)
        {
            var result = _userRepository.GetUserById(id);
            return Ok(result);
        }

        [HttpPost]
        [EnableRateLimiting("register")]
        public async Task<ActionResult<UserCreatedDTO>> CreateUser([FromBody] UserCreationDTO user)
        {
            try
            {
                var result = _userRepository.CreateUser(user);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_USER",
                    EntityName = "User",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                await NotifyMemberAddedAsync(user);

                return Created("", result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_USER",
                    EntityName = "User",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<UserCreatedDTO>> UpdateUser([FromBody] UserUpdateDTO user)
        {
            try
            {
                var oldUser = _userRepository.GetUserById(user.Id);
                var result = _userRepository.UpdateUser(user);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_USER",
                    EntityName = "User",
                    OldValues = JsonSerializer.Serialize(oldUser),
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                await NotifyRoleChangedAsync(oldUser, user);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_USER",
                    EntityName = "User",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                _userRepository.DeleteUser(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_USER",
                    EntityName = "User",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_USER",
                    EntityName = "User",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] UserLoginDTO login)
        {
            try
            {
                var result = _userRepository.Login(login);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = login.Username,
                    Action = "LOGIN",
                    EntityName = "User",
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = login.Username,
                    Action = "LOGIN",
                    EntityName = "User",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public ActionResult<LoginResponseDTO> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                var result = _userRepository.RefreshToken(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        // Non-fatal: fire a system notification when a new member is added to an organization.
        // The org is known directly off the creation DTO (no box lookup needed). Any failure is
        // swallowed so user creation is never broken.
        private async Task NotifyMemberAddedAsync(UserCreationDTO user)
        {
            try
            {
                if (user.OrganizationId == null)
                    return;   // no organization context — nothing org-scoped to notify

                await _notificationClient.TryNotifyAsync(new SystemNotificationCreationDTO
                {
                    Text = "A new member was added to your organization.",
                    OrganizationId = user.OrganizationId
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);
            }
            catch { }
        }

        // Non-fatal: fire a system notification when a member's role actually changes
        // (RoleId differs between the stored user and the update). Skipped when the role is
        // unchanged or the org cannot be resolved. Any failure is swallowed.
        private async Task NotifyRoleChangedAsync(UserDTO oldUser, UserUpdateDTO user)
        {
            try
            {
                if (oldUser == null || oldUser.RoleId == user.RoleId)
                    return;   // role did not change — avoid noise

                var organizationId = user.OrganizationId ?? oldUser.OrganizationId;
                if (organizationId == null)
                    return;   // no organization context

                await _notificationClient.TryNotifyAsync(new SystemNotificationCreationDTO
                {
                    Text = "A member's role was changed.",
                    OrganizationId = organizationId
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);
            }
            catch { }
        }
    }
}
