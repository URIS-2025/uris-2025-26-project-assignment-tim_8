using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Clients;
using SuggestionService.Models.DTOs;
using AnonymousRepository.Interfaces;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionController : Controller
    {
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;
        private readonly SuggestionBoxServiceClient _suggestionBoxClient;
        private readonly SystemNotificationServiceClient _notificationClient;

        public SuggestionController(ISuggestionRepository suggestionRepository, IMapper mapper, LoggerServiceClient loggerClient,
            SuggestionBoxServiceClient suggestionBoxClient, SystemNotificationServiceClient notificationClient)
        {
            _suggestionRepository = suggestionRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
            _suggestionBoxClient = suggestionBoxClient;
            _notificationClient = notificationClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionDTO>> GetAllSuggestions()
        {
            return Ok(_suggestionRepository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionDTO> GetSuggestionById(Guid id)
        {
            return Ok(_suggestionRepository.GetById(id));
        }

        [HttpGet("user/{id}")]
        public ActionResult<IEnumerable<SuggestionDTO>> GetSuggestionsByUserId(Guid id)
        {
            return Ok(_suggestionRepository.GetByUserId(id));
        }

        [HttpPost]
        public async Task<ActionResult<SuggestionCreatedDTO>> CreateSuggestion([FromBody] SuggestionCreationDTO suggestion)
        {
            try
            {
                var result = _suggestionRepository.Create(suggestion);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION",
                    EntityName = "Suggestion",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                await NotifySuggestionSubmittedAsync(result);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION",
                    EntityName = "Suggestion",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<SuggestionCreatedDTO>> UpdateSuggestion([FromBody] SuggestionUpdateDTO suggestion)
        {
            try
            {
                var result = _suggestionRepository.Update(suggestion);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION",
                    EntityName = "Suggestion",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION",
                    EntityName = "Suggestion",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSuggestion(Guid id)
        {
            try
            {
                _suggestionRepository.Delete(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION",
                    EntityName = "Suggestion",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "SuggestionService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION",
                    EntityName = "Suggestion",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        // Non-fatal: resolve the owning org from the suggestion's box and fire a system
        // notification. Any failure (box missing, notification error) is swallowed so the
        // suggestion create is never broken.
        private async Task NotifySuggestionSubmittedAsync(SuggestionCreatedDTO result)
        {
            try
            {
                var bearer = Request.Headers["Authorization"];
                var ct = HttpContext.RequestAborted;

                var organizationId = await _suggestionBoxClient.TryGetOrganizationIdAsync(result.SuggestionBoxId, bearer, ct);
                if (organizationId == null)
                    return;   // box missing/unresolvable — skip the notification

                await _notificationClient.TryNotifyAsync(new SystemNotificationCreationDTO
                {
                    Text = "A new suggestion was submitted.",
                    OrganizationId = organizationId
                }, bearer, ct);
            }
            catch { }
        }
    }
}