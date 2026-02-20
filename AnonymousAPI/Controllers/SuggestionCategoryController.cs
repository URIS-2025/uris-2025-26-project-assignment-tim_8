using AnonymousDomain.Models.Suggestion;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionCategoryController : Controller
    {
        //private readonly ISuggestionCategoryService _suggestionCategoryService;
        //private readonly IMapper _mapper;
        public SuggestionCategoryController(/* ISuggestionCategoryService suggestionCategoryService, IMapper mapper*/)
        {
            // _suggestionCategoryService = suggestionCategoryService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionCategory>> GetAllSuggestionCategories() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _suggestionCategoryService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionCategory> GetSuggestionCategoryById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _suggestionCategoryService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<SuggestionCategory> CreateSuggestionCategory([FromBody] SuggestionCategory suggestionCategory) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionCategoryService.Create(suggestionCategory);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<SuggestionCategory> UpdateSuggestionCategory([FromBody] SuggestionCategory suggestionCategory) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _suggestionCategoryService.Update(suggestionCategory);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestionCategory(Guid id)
        {
            // _suggestionCategoryService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
