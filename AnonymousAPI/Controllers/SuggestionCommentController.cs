using AnonymousDomain.Models.Suggestion;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionCommentController : Controller
    {
        //private readonly ISuggestionCommentService _suggestionCommentService;
        //private readonly IMapper _mapper;
        public SuggestionCommentController(/* ISuggestionCommentService suggestionCommentService, IMapper mapper*/)
        {
            // _suggestionCommentService = suggestionCommentService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionComment>> GetAllSuggestionComments() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _suggestionCommentService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionComment> GetSuggestionCommentById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionCommentService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<Suggestion>> GetSuggestionCommentsBySuggestionId(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionCommentService.GetBySuggestionId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<SuggestionComment> CreateSuggestionComment([FromBody] SuggestionComment suggestionComment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionCommentService.Create(suggestionComment);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<SuggestionComment> UpdateSuggestionComment([FromBody] SuggestionComment suggestionComment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionCommentService.Update(suggestionComment);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestionComment(Guid id)
        {
            // _suggestionCommentService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
