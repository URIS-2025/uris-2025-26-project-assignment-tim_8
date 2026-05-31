using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Clients;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnonymousUserController : Controller
    {
        private readonly IAnonymousUserRepository _anonymousUserRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public AnonymousUserController(IAnonymousUserRepository anonymousUserRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _anonymousUserRepository = anonymousUserRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [Authorize]
        [HttpGet]
        public ActionResult<IEnumerable<AnonymousUserDTO>> GetAllAnonymousUsers()
        {
            var result = _anonymousUserRepository.GetAllAnonymousUsers();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public ActionResult<AnonymousUserDTO> GetAnonymousUserById(Guid id)
        {
            var result = _anonymousUserRepository.GetAnonymousUserById(id);
            return Ok(result);
        }

        [HttpPost]
        [EnableRateLimiting("register")]
        public ActionResult<AnonymousUserDTO> Create([FromBody] AnonymousUserCreationDTO anonUser)
        {
            try
            {
                var result = _anonymousUserRepository.CreateUser(anonUser);
                return Created("", result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<AnonymousLoginResponseDTO>> Login([FromBody] AnonymousUserLoginDTO login)
        {
            try
            {
                var result = _anonymousUserRepository.Login(login);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = login.Username,
                    Action = "LOGIN",
                    EntityName = "AnonymousUser",
                    IsSuccess = true,
                    ServiceName = "AnonymousUserService",
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
                    EntityName = "AnonymousUser",
                    IsSuccess = false,
                    ServiceName = "AnonymousUserService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public ActionResult<AnonymousLoginResponseDTO> RefreshToken([FromBody] AnonymousRefreshTokenRequestDTO request)
        {
            try
            {
                var result = _anonymousUserRepository.RefreshToken(request.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnonymousUser(Guid id)
        {
            try
            {
                _anonymousUserRepository.DeleteAnonymousUser(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_ANONYMOUS_USER",
                    EntityName = "AnonymousUser",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "AnonymousUserService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_ANONYMOUS_USER",
                    EntityName = "AnonymousUser",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "AnonymousUserService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}
