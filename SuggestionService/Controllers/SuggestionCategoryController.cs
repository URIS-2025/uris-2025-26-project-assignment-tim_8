using AnonymousDomain.Models.Suggestion.DTOs;
using AnonymousRepository.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionCategoryController : Controller
    {
        private readonly ISuggestionCategoryRepository _suggestionCategoryRepository;
        private readonly IMapper _mapper;

        public SuggestionCategoryController(ISuggestionCategoryRepository suggestionCategoryRepository, IMapper mapper)
        {
            _suggestionCategoryRepository = suggestionCategoryRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionCategoryDTO>> GetAllSuggestionCategories()
        {
            var result = _suggestionCategoryRepository.GetAll();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionCategoryDTO> GetSuggestionCategoryById(Guid id)
        {
            var result = _suggestionCategoryRepository.GetById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<SuggestionCategoryDTO> CreateSuggestionCategory([FromBody] SuggestionCategoryDTO suggestionCategory)
        {
            var result = _suggestionCategoryRepository.Create(suggestionCategory);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<SuggestionCategoryDTO> UpdateSuggestionCategory([FromBody] SuggestionCategoryDTO suggestionCategory)
        {
            var result = _suggestionCategoryRepository.Update(suggestionCategory);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestionCategory(Guid id)
        {
            _suggestionCategoryRepository.Delete(id);
            return NoContent();
        }
    }
}