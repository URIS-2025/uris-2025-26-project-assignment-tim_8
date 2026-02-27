using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using AttachmentService.Models.DTOs;
using AttachmentService.Interfaces;
using AttachmentService.Models.Attachment.DTOs;

namespace AnonymousAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : Controller
    {
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IMapper _mapper;

        public AttachmentController(IAttachmentRepository attachmentRepository, IMapper mapper)
        {
            _attachmentRepository = attachmentRepository;
            _mapper = mapper;
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
        public ActionResult<AttachmentDTO> CreateAttachment([FromBody] AttachmentCreationDTO attachment)
        {
            var result = _attachmentRepository.Create(attachment);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<AttachmentDTO> UpdateAttachment([FromBody] AttachmentUpdateDTO attachment)
        {
            var result = _attachmentRepository.Update(attachment);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAttachment(Guid id)
        {
            _attachmentRepository.Delete(id);
            return NoContent();
        }
    }
}