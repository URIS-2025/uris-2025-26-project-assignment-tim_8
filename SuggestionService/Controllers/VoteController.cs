using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Clients;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;
using AnonymousRepository.Interfaces;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoteController : Controller
    {
        private readonly IVoteRepository _voteRepository;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;
        private readonly SuggestionBoxServiceClient _suggestionBoxClient;
        private readonly SystemNotificationServiceClient _notificationClient;

        public VoteController(IVoteRepository voteRepository, ISuggestionRepository suggestionRepository, IMapper mapper,
            LoggerServiceClient loggerClient, SuggestionBoxServiceClient suggestionBoxClient, SystemNotificationServiceClient notificationClient)
        {
            _voteRepository = voteRepository;
            _suggestionRepository = suggestionRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
            _suggestionBoxClient = suggestionBoxClient;
            _notificationClient = notificationClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<VoteDTO>> GetVotes()
        {
            return Ok(new List<VoteDTO>());
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<VoteDTO>> GetVotesBySuggestionId(Guid id)
        {
            return Ok(_voteRepository.GetBySuggestionId(id));
        }

        [HttpPost]
        public async Task<ActionResult<VoteCreationDTO>> CreateVote([FromBody] VoteCreationDTO vote)
        {
            try
            {
                var result = _voteRepository.Create(vote);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_VOTE",
                    EntityName = "Vote",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                await NotifyVoteCreatedAsync(result);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_VOTE",
                    EntityName = "Vote",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVote(Guid id)
        {
            try
            {
                _voteRepository.Delete(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_VOTE",
                    EntityName = "Vote",
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
                    Action = "DELETE_VOTE",
                    EntityName = "Vote",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        // Non-fatal: resolve the owning org through the vote's suggestion -> box, then fire
        // a system notification. Any failure is swallowed so vote creation is never broken.
        private async Task NotifyVoteCreatedAsync(VoteCreationDTO result)
        {
            try
            {
                var bearer = Request.Headers["Authorization"];
                var ct = HttpContext.RequestAborted;

                var suggestion = _suggestionRepository.GetById(result.SuggestionId);
                if (suggestion == null)
                    return;   // suggestion unresolvable — skip the notification

                var organizationId = await _suggestionBoxClient.TryGetOrganizationIdAsync(suggestion.SuggestionBoxId, bearer, ct);
                if (organizationId == null)
                    return;   // box missing/unresolvable — skip the notification

                await _notificationClient.TryNotifyAsync(new SystemNotificationCreationDTO
                {
                    Text = "A suggestion received a new vote.",
                    OrganizationId = organizationId
                }, bearer, ct);
            }
            catch { }
        }
    }
}