using AttachmentService.Clients;
using AttachmentService.Interfaces;
using AttachmentService.Models.Attachment.DTOs;
using AttachmentService.Models.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : Controller
    {
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IMapper _mapper;
        private readonly LoggerServiceClient _loggerClient;

        public AttachmentController(IAttachmentRepository attachmentRepository, IMapper mapper, LoggerServiceClient loggerClient)
        {
            _attachmentRepository = attachmentRepository;
            _mapper = mapper;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<AttachmentDTO>> GetAllAttachments()
        {
            var result = _attachmentRepository.GetAll();
            return Ok(result);
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<AttachmentDTO>> GetAttachmentBySuggestionId(Guid id)
        {
            var result = _attachmentRepository.GetBySuggestionId(id);
            return Ok(result);
        }

        [HttpGet("problem/{id}")]
        public ActionResult<IEnumerable<AttachmentDTO>> GetAttachmentByProblemId(Guid id)
        {
            var result = _attachmentRepository.GetByProblemId(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AttachmentDTO>> CreateAttachment([FromBody] AttachmentCreationDTO attachment)
        {
            try
            {
                var result = _attachmentRepository.Create(attachment);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_ATTACHMENT",
                    EntityName = "Attachment",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "AttachmentService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_ATTACHMENT",
                    EntityName = "Attachment",
                    IsSuccess = false,
                    ServiceName = "AttachmentService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<AttachmentDTO>> UpdateAttachment([FromBody] AttachmentUpdateDTO attachment)
        {
            try
            {
                var result = _attachmentRepository.Update(attachment);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_ATTACHMENT",
                    EntityName = "Attachment",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "AttachmentService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_ATTACHMENT",
                    EntityName = "Attachment",
                    IsSuccess = false,
                    ServiceName = "AttachmentService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttachment(Guid id)
        {
            try
            {
                _attachmentRepository.Delete(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_ATTACHMENT",
                    EntityName = "Attachment",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "AttachmentService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_ATTACHMENT",
                    EntityName = "Attachment",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "AttachmentService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}