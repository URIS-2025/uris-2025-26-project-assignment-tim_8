using AnonymousDomain.Models.Suggestion.DTOs;
using AnonymousRepository.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Clients;
using SuggestionService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionCommentController : Controller
    {
        private readonly ISuggestionCommentRepository _suggestionCommentRepository;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;
        private readonly SuggestionBoxServiceClient _suggestionBoxClient;
        private readonly SystemNotificationServiceClient _notificationClient;

        public SuggestionCommentController(ISuggestionCommentRepository suggestionCommentRepository, ISuggestionRepository suggestionRepository,
            IMapper mapper, LoggerServiceClient loggerClient,
            SuggestionBoxServiceClient suggestionBoxClient, SystemNotificationServiceClient notificationClient)
        {
            _suggestionCommentRepository = suggestionCommentRepository;
            _suggestionRepository = suggestionRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
            _suggestionBoxClient = suggestionBoxClient;
            _notificationClient = notificationClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionCommentDTO>> GetAllSuggestionComments()
        {
            return Ok(_suggestionCommentRepository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionCommentDTO> GetSuggestionCommentById(Guid id)
        {
            return Ok(_suggestionCommentRepository.GetById(id));
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<SuggestionCommentDTO>> GetSuggestionCommentsBySuggestionId(Guid id)
        {
            return Ok(_suggestionCommentRepository.GetBySuggestionId(id));
        }

        [HttpPost]
        public async Task<ActionResult<SuggestionCommentCreationDTO>> CreateSuggestionComment([FromBody] SuggestionCommentCreationDTO suggestionComment)
        {
            try
            {
                var result = _suggestionCommentRepository.Create(suggestionComment);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION_COMMENT",
                    EntityName = "SuggestionComment",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                await NotifyCommentCreatedAsync(result);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION_COMMENT",
                    EntityName = "SuggestionComment",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<SuggestionCommentUpdateDTO>> UpdateSuggestionComment([FromBody] SuggestionCommentUpdateDTO suggestionComment)
        {
            try
            {
                var result = _suggestionCommentRepository.Update(suggestionComment);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_COMMENT",
                    EntityName = "SuggestionComment",
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
                    Action = "UPDATE_SUGGESTION_COMMENT",
                    EntityName = "SuggestionComment",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSuggestionComment(Guid id)
        {
            try
            {
                _suggestionCommentRepository.Delete(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION_COMMENT",
                    EntityName = "SuggestionComment",
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
                    Action = "DELETE_SUGGESTION_COMMENT",
                    EntityName = "SuggestionComment",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        // Non-fatal: resolve the owning org through the comment's parent suggestion -> box,
        // then fire a system notification carrying the comment id. Any failure is swallowed
        // so comment creation is never broken.
        private async Task NotifyCommentCreatedAsync(SuggestionCommentCreationDTO result)
        {
            try
            {
                var bearer = Request.Headers["Authorization"];
                var ct = HttpContext.RequestAborted;

                var suggestion = _suggestionRepository.GetById(result.SuggestionId);
                if (suggestion == null)
                    return;   // parent suggestion unresolvable — skip the notification

                var organizationId = await _suggestionBoxClient.TryGetOrganizationIdAsync(suggestion.SuggestionBoxId, bearer, ct);
                if (organizationId == null)
                    return;   // box missing/unresolvable — skip the notification

                await _notificationClient.TryNotifyAsync(new SystemNotificationCreationDTO
                {
                    Text = "A new comment was added to a suggestion.",
                    OrganizationId = organizationId,
                    SuggestionCommentId = result.Id
                }, bearer, ct);
            }
            catch { }
        }
    }
}