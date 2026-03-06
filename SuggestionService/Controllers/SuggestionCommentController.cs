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
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public SuggestionCommentController(ISuggestionCommentRepository suggestionCommentRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _suggestionCommentRepository = suggestionCommentRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
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
    }
}