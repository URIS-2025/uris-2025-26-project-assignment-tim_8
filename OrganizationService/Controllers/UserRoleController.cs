using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Clients;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserRoleController : Controller
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly LoggerServiceClient _loggerClient;

        public UserRoleController(IUserRoleRepository userRoleRepository, LoggerServiceClient loggerClient)
        {
            _userRoleRepository = userRoleRepository;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserRoleDTO>> GetAllUserRoles()
        {
            var result = _userRoleRepository.GetAllUserRoles();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<UserRoleDTO> GetUserRoleById(Guid id)
        {
            var result = _userRoleRepository.GetUserRoleById(id);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<UserRoleCreatedDTO>> CreateUserRole([FromBody] UserRoleCreationDTO userRole)
        {
            try
            {
                var result = _userRoleRepository.CreateUserRole(userRole);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_USER_ROLE",
                    EntityName = "UserRole",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_USER_ROLE",
                    EntityName = "UserRole",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<UserRoleCreatedDTO>> UpdateUserRole([FromBody] UserRoleDTO userRole)
        {
            try
            {
                var oldRole = _userRoleRepository.GetUserRoleById(userRole.Id);
                var result = _userRoleRepository.UpdateUserRole(userRole);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_USER_ROLE",
                    EntityName = "UserRole",
                    OldValues = JsonSerializer.Serialize(oldRole),
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_USER_ROLE",
                    EntityName = "UserRole",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserRole(Guid id)
        {
            try
            {
                _userRoleRepository.DeleteUserRole(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_USER_ROLE",
                    EntityName = "UserRole",
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
                    Action = "DELETE_USER_ROLE",
                    EntityName = "UserRole",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}
