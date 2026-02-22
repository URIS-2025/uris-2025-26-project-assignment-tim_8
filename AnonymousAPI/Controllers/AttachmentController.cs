using AnonymousDomain.Models.Attachment;
using AnonymousDomain.Models.Suggestion;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : Controller
    {
        //private readonly IAttachmentService _attachmentService;
        //private readonly IMapper _mapper;
        public AttachmentController(/* IAttachmentService attachmentService, IMapper mapper*/)
        {
            // _attachmentService = attachmentService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Attachment>> GetAllAttachments() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _attachmentService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<Attachment> CreateAttachment([FromBody] Attachment attachment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _attachmentService.Create(suggestion);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<Attachment> UpdateAttachment([FromBody] Attachment attachment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _attachmentService.Update(attachment);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }


        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<Attachment>> GetAttachmentBySuggestionId(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _attachementService.GetBySuggestionId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("problem/{id}")]
        public ActionResult<IEnumerable<Attachment>> GetAttachmentByProblemId(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _attachementService.GetByProblemId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAttachment(Guid id)
        {
            // _attachmentService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
