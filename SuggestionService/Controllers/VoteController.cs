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
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public VoteController(IVoteRepository voteRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _voteRepository = voteRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
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
    }
}