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
    public class ProblemCommentController : Controller
    {
        private readonly IProblemCommentRepository _problemCommentRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public ProblemCommentController(IProblemCommentRepository problemCommentRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _problemCommentRepository = problemCommentRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemCommentDTO>> GetAllProblemComments()
        {
            var result = _problemCommentRepository.GetAllProblemComments();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemCommentDTO> GetProblemCommentById(Guid id)
        {
            var result = _problemCommentRepository.GetProblemCommentById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProblemCommentCreatedDTO>> CreateProblemComment([FromBody] ProblemCommentCreationDTO problemComment)
        {
            try
            {
                var result = _problemCommentRepository.CreateProblemComment(problemComment);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PROBLEM_COMMENT",
                    EntityName = "ProblemComment",
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
                    Action = "CREATE_PROBLEM_COMMENT",
                    EntityName = "ProblemComment",
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ProblemCommentDTO>> UpdateProblemComment([FromBody] ProblemCommentUpdateDTO problemComment)
        {
            try
            {
                var result = _problemCommentRepository.UpdateProblemComment(problemComment);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_COMMENT",
                    EntityName = "ProblemComment",
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
                    Action = "UPDATE_PROBLEM_COMMENT",
                    EntityName = "ProblemComment",
                    IsSuccess = false,
                    ServiceName = "ProblemService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblemComment(Guid id)
        {
            try
            {
                _problemCommentRepository.DeleteProblemComment(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PROBLEM_COMMENT",
                    EntityName = "ProblemComment",
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
                    Action = "DELETE_PROBLEM_COMMENT",
                    EntityName = "ProblemComment",
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