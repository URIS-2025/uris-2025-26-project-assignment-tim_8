using AnonymousDomain.Models.Suggestion;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionController : Controller
    {
        //private readonly ISuggestionService _suggestionService;
        //private readonly IMapper _mapper;
        public SuggestionController(/* ISuggestionService suggestionService, IMapper mapper*/)
        {
            // _suggestionService = suggestionService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Suggestion>> GetAllSuggestions() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _suggestionService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<Suggestion> GetSuggestionById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("user/{id}")]
        public ActionResult<IEnumerable<Suggestion>> GetSuggestionsByUserId(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionService.GetByUserId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<Suggestion> CreateSuggestion([FromBody] Suggestion suggestion) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionService.Create(suggestion);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<Suggestion> UpdateSuggestion([FromBody] Suggestion suggestion) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionService.Update(suggestion);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestion(Guid id)
        {
            // _suggestionService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
