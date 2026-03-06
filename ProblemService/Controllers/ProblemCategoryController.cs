using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ProblemService.Clients;
using ProblemService.Data;
using ProblemService.Models.DTOs;
using System.Text.Json;

namespace ProblemService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemCategoryController : Controller
    {
        private readonly IProblemCategoryRepository _problemCategoryRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public ProblemCategoryController(IProblemCategoryRepository problemCategoryRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _problemCategoryRepository = problemCategoryRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemCategoryDTO>> GetAllProblemCategories()
        {
            var result = _problemCategoryRepository.GetAllProblemCategories();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemCategoryDTO> GetProblemCategoryById(Guid id)
        {
            var result = _problemCategoryRepository.GetProblemCategoryById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProblemCategoryCreatedDTO>> CreateProblemCategory([FromBody] ProblemCategoryCreationDTO problemCategory)
        {
            try
            {
                var result = _problemCategoryRepository.CreateProblemCategory(problemCategory);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PROBLEM_CATEGORY",
                    EntityName = "ProblemCategory",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "ProblemService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PROBLEM_CATEGORY",
                    EntityName = "ProblemCategory",
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ProblemCategoryDTO>> UpdateProblemCategory([FromBody] ProblemCategoryUpdateDTO problemCategory)
        {
            try
            {
                var result = _problemCategoryRepository.UpdateProblemCategory(problemCategory);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_CATEGORY",
                    EntityName = "ProblemCategory",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "ProblemService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_CATEGORY",
                    EntityName = "ProblemCategory",
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblemCategory(Guid id)
        {
            try
            {
                _problemCategoryRepository.DeleteProblemCategory(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PROBLEM_CATEGORY",
                    EntityName = "ProblemCategory",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "ProblemService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PROBLEM_CATEGORY",
                    EntityName = "ProblemCategory",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}