using Microsoft.AspNetCore.Mvc;
using SuggestionBoxService.Clients;
using SuggestionBoxService.Data;
using SuggestionBoxService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionBoxController : ControllerBase
    {
        private readonly ISuggestionBoxRepository _repository;
        private readonly LoggerServiceClient _loggerClient;

        public SuggestionBoxController(ISuggestionBoxRepository repository, LoggerServiceClient loggerClient)
        {
            _repository = repository;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionBoxDTO>> GetAllSuggestionBoxes()
        {
            return Ok(_repository.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionBoxDTO> GetById(Guid id)
        {
            var result = _repository.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("organization/{organizationId}")]
        public ActionResult<IEnumerable<SuggestionBoxDTO>> GetByOrganizationId(Guid organizationId)
        {
            return Ok(_repository.GetByOrganizationId(organizationId));
        }

        [HttpPost]
        public async Task<ActionResult<SuggestionBoxDTO>> Create([FromBody] SuggestionBoxCreateDTO dto)
        {
            try
            {
                var result = _repository.Create(dto);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION_BOX",
                    EntityName = "SuggestionBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_SUGGESTION_BOX",
                    EntityName = "SuggestionBox",
                    IsSuccess = false,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<SuggestionBoxDTO>> Update([FromBody] SuggestionBoxUpdateDTO dto)
        {
            try
            {
                var result = _repository.Update(dto);
                if (result == null) return NotFound();

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_BOX",
                    EntityName = "SuggestionBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_BOX",
                    EntityName = "SuggestionBox",
                    IsSuccess = false,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<SuggestionBoxDTO>> UpdateSuggestionBoxStatus(Guid id, [FromBody] BoxStatusUpdateDTO dto)
        {
            try
            {
                var result = _repository.SetStatus(id, dto.Status);
                if (result == null) return NotFound(new { error = "SuggestionBox with that Id does not exist." });

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_BOX_STATUS",
                    EntityName = "SuggestionBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_BOX_STATUS",
                    EntityName = "SuggestionBox",
                    IsSuccess = false,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/password")]
        public async Task<ActionResult<SuggestionBoxDTO>> UpdateSuggestionBoxPassword(Guid id, [FromBody] BoxPasswordDTO dto)
        {
            try
            {
                var result = _repository.SetPassword(id, dto.Password);
                if (result == null) return NotFound(new { error = "SuggestionBox with that Id does not exist." });

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_BOX_PASSWORD",
                    EntityName = "SuggestionBox",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_SUGGESTION_BOX_PASSWORD",
                    EntityName = "SuggestionBox",
                    IsSuccess = false,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/verify-password")]
        public ActionResult VerifySuggestionBoxPassword(Guid id, [FromBody] BoxPasswordDTO dto)
        {
            var valid = _repository.VerifyPassword(id, dto.Password);
            return Ok(new { valid });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                _repository.Delete(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION_BOX",
                    EntityName = "SuggestionBox",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION_BOX",
                    EntityName = "SuggestionBox",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("organization/{organizationId}")]
        public async Task<IActionResult> DeleteByOrganizationId(Guid organizationId)
        {
            try
            {
                _repository.DeleteByOrganizationId(organizationId);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION_BOX_BY_ORGANIZATION",
                    EntityName = "SuggestionBox",
                    OldValues = organizationId.ToString(),
                    IsSuccess = true,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_SUGGESTION_BOX_BY_ORGANIZATION",
                    EntityName = "SuggestionBox",
                    OldValues = organizationId.ToString(),
                    IsSuccess = false,
                    ServiceName = "SuggestionBoxService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}