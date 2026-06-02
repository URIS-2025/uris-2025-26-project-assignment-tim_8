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
        private readonly IProblemRepository _problemRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;
        private readonly ProblemBoxServiceClient _problemBoxClient;
        private readonly SystemNotificationServiceClient _notificationClient;

        public ProblemCommentController(IProblemCommentRepository problemCommentRepository, IProblemRepository problemRepository,
            IMapper mapper, LoggerServiceClient loggerClient, ProblemBoxServiceClient problemBoxClient,
            SystemNotificationServiceClient notificationClient)
        {
            _problemCommentRepository = problemCommentRepository;
            _problemRepository = problemRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
            _problemBoxClient = problemBoxClient;
            _notificationClient = notificationClient;
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

                await NotifyCommentCreatedAsync(result);

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

        // Non-fatal: resolve the owning org through the comment's parent problem -> box,
        // then fire a system notification carrying the comment id. Any failure is swallowed
        // so comment creation is never broken.
        private async Task NotifyCommentCreatedAsync(ProblemCommentCreatedDTO result)
        {
            try
            {
                var bearer = Request.Headers["Authorization"];
                var ct = HttpContext.RequestAborted;

                var problem = _problemRepository.GetProblemById(result.ProblemId);
                if (problem == null)
                    return;   // parent problem unresolvable — skip the notification

                var organizationId = await _problemBoxClient.TryGetOrganizationIdAsync(problem.ProblemBoxId, bearer, ct);
                if (organizationId == null)
                    return;   // box missing/unresolvable — skip the notification

                await _notificationClient.TryNotifyAsync(new SystemNotificationCreationDTO
                {
                    Text = "A new comment was added to a problem.",
                    OrganizationId = organizationId,
                    ProblemCommentId = result.Id
                }, bearer, ct);
            }
            catch { }
        }
    }
}