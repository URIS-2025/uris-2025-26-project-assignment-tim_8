using AnonymousDomain.Models.SuggestionBox;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionBoxController : Controller
    {
        //private readonly ISuggestionBoxService _suggestionBoxService;
        //private readonly IMapper _mapper;
        public SuggestionBoxController(/* ISuggestionBoxService suggestionBoxService, IMapper mapper*/)
        {
            // _suggestionBoxService = suggestionBoxService;
            // _mapper = mapper;
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionBox> GetSuggestionBoxById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionBoxService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("organization/{id}")]
        public ActionResult<IEnumerable<SuggestionBox>> GetSuggestionBoxesByOrganizationId(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionBoxService.GetByOrganizationId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<SuggestionBox> CreateSuggestionBox([FromBody] SuggestionBox suggestionBox) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionBoxService.Create(suggestionBox);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<SuggestionBox> UpdateSuggestionBox([FromBody] SuggestionBox suggestionBox) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionBoxService.Update(suggestionBox);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestionBox(Guid id)
        {
            // _suggestionBoxService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }


        //deleeteBySuggestionId da li treba da se radi i kako bi izgledao...
    }
}
