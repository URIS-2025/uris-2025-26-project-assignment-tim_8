using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Clients;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionCategoryController : Controller
    {
        private readonly ISuggestionCategoryRepository _suggestionCategoryRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public SuggestionCategoryController(ISuggestionCategoryRepository suggestionCategoryRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _suggestionCategoryRepository = suggestionCategoryRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionCategoryDTO>> GetAllSuggestionCategories()
        {
            return Ok(_suggestionCategoryRepository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionCategoryDTO> GetSuggestionCategoryById(Guid id)
        {
            return Ok(_suggestionCategoryRepository.GetById(id));
        }

        [HttpPost]
        public async Task<ActionResult<SuggestionCategoryDTO>> CreateSuggestionCategory([FromBody] SuggestionCategoryDTO suggestionCategory)
        {
            try
            {
                var result = _suggestionCategoryRepository.Create(suggestionCategory);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION_CATEGORY",
                    EntityName = "SuggestionCategory",
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
                    Action = "CREATE_SUGGESTION_CATEGORY",
                    EntityName = "SuggestionCategory",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<SuggestionCategoryDTO>> UpdateSuggestionCategory([FromBody] SuggestionCategoryDTO suggestionCategory)
        {
            try
            {
                var result = _suggestionCategoryRepository.Update(suggestionCategory);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_CATEGORY",
                    EntityName = "SuggestionCategory",
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
                    Action = "UPDATE_SUGGESTION_CATEGORY",
                    EntityName = "SuggestionCategory",
                    IsSuccess = false,
                    ServiceName = "SuggestionService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSuggestionCategory(Guid id)
        {
            try
            {
                _suggestionCategoryRepository.Delete(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION_CATEGORY",
                    EntityName = "SuggestionCategory",
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
                    Action = "DELETE_SUGGESTION_CATEGORY",
                    EntityName = "SuggestionCategory",
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