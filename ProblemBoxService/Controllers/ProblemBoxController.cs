using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ProblemBoxService.Clients;
using ProblemBoxService.Data;
using ProblemBoxService.Models.DTOs;
using System.Text.Json;

namespace ProblemBoxService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemBoxController : Controller
    {
        private readonly IProblemBoxRepository _problemBoxRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public ProblemBoxController(IProblemBoxRepository problemBoxRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _problemBoxRepository = problemBoxRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemBoxDTO>> GetProblemBoxes()
        {
            var result = _problemBoxRepository.GetAll();
            return Ok(result);
        }

        [HttpGet("organization/{id}")]
        public ActionResult<IEnumerable<ProblemBoxDTO>> GetProblemBoxByOrganizationId(Guid id)
        {
            var result = _problemBoxRepository.GetProblemBoxByOrganizationId(id);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemBoxDTO> GetProblemBoxById(Guid id)
        {
            var result = _problemBoxRepository.GetProblemBoxById(id);
            return Ok(result);
        }

        [HttpGet("boxaccesslink/{boxAccessLinkId}")]
        public ActionResult<ProblemBoxDTO> GetProblemBoxByAccessLinkId(Guid boxAccessLinkId)
        {
            var result = _problemBoxRepository.GetProblemBoxByAccessLinkId(boxAccessLinkId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProblemBoxCreatedDTO>> CreateProblemBox([FromBody] ProblemBoxCreationDTO problemBox)
        {
            try
            {
                var result = _problemBoxRepository.CreateProblemBox(problemBox);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PROBLEM_BOX",
                    EntityName = "ProblemBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_PROBLEM_BOX",
                    EntityName = "ProblemBox",
                    IsSuccess = false,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ProblemBoxDTO>> UpdateProblemBox([FromBody] ProblemBoxUpdateDTO problemBox)
        {
            try
            {
                var result = _problemBoxRepository.UpdateProblemBox(problemBox);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_BOX",
                    EntityName = "ProblemBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_BOX",
                    EntityName = "ProblemBox",
                    IsSuccess = false,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<ProblemBoxDTO>> UpdateProblemBoxStatus(Guid id, [FromBody] BoxStatusUpdateDTO dto)
        {
            try
            {
                var result = _problemBoxRepository.SetStatus(id, dto.Status);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_BOX_STATUS",
                    EntityName = "ProblemBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_PROBLEM_BOX_STATUS",
                    EntityName = "ProblemBox",
                    IsSuccess = false,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblemBox(Guid id)
        {
            try
            {
                _problemBoxRepository.DeleteProblemBox(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PROBLEM_BOX",
                    EntityName = "ProblemBox",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_PROBLEM_BOX",
                    EntityName = "ProblemBox",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "ProblemBoxService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}