using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Clients;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public ActionResult<IEnumerable<AnonymousUserDTO>> GetAllAnonymousUsers()
        {
            var result = _anonymousUserRepository.GetAllAnonymousUsers();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<AnonymousUserDTO> GetAnonymousUserById(Guid id)
        {
            var result = _anonymousUserRepository.GetAnonymousUserById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<AnonymousUserDTO> Create([FromBody] AnonymousUserCreationDTO anonUser)
        {
            var result = _anonymousUserRepository.CreateUser(anonUser);
            return Created("", result);
        }

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