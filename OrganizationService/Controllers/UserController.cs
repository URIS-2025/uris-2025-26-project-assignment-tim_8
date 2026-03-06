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
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly LoggerServiceClient _loggerClient;

        public UserController(IUserRepository userRepository, LoggerServiceClient loggerClient)
        {
            _userRepository = userRepository;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserDTO>> GetAllUsers()
        {
            var result = _userRepository.GetAllUsers();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<UserDTO> GetUserById(Guid id)
        {
            var result = _userRepository.GetUserById(id);
            return Ok(result);
        }

        [HttpPost]
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

                return Created("", result);
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
        public async Task<ActionResult<string>> Login([FromBody] UserLoginDTO login)
        {
            try
            {
                var token = _userRepository.Login(login);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = login.Username,
                    Action = "LOGIN",
                    EntityName = "User",
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(token);
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
    }
}