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
    public class ProblemController : Controller
    {
        private readonly IProblemRepository _problemRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public ProblemController(IProblemRepository problemRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _problemRepository = problemRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemDTO>> GetAllProblems()
        {
            var result = _problemRepository.GetAllProblems();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemDTO> GetProblemById(Guid id)
        {
            var result = _problemRepository.GetProblemById(id);
            return Ok(result);
        }

        [HttpGet("problembox/{problemBoxId}")]
        public ActionResult<IEnumerable<ProblemDTO>> GetProblemsByProblemBoxId(Guid problemBoxId)
        {
            var result = _problemRepository.GetProblemsByProblemBoxId(problemBoxId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProblemCreatedDTO>> CreateProblem([FromBody] ProblemCreationDTO problem)
        {
            try
            {
                var result = _problemRepository.CreateProblem(problem);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PROBLEM",
                    EntityName = "Problem",
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
                    Action = "CREATE_PROBLEM",
                    EntityName = "Problem",
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ProblemDTO>> UpdateProblem([FromBody] ProblemUpdateDTO problem)
        {
            try
            {
                var result = _problemRepository.UpdateProblem(problem);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM",
                    EntityName = "Problem",
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
                    Action = "UPDATE_PROBLEM",
                    EntityName = "Problem",
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblem(Guid id)
        {
            try
            {
                _problemRepository.DeleteProblem(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PROBLEM",
                    EntityName = "Problem",
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
                    Action = "DELETE_PROBLEM",
                    EntityName = "Problem",
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