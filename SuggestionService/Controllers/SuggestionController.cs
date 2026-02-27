using AnonymousRepository.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionController : Controller
    {
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IMapper _mapper;

        public SuggestionController(ISuggestionRepository suggestionRepository, IMapper mapper)
        {
            _suggestionRepository = suggestionRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionDTO>> GetAllSuggestions()
        {
            var result = _suggestionRepository.GetAll();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionDTO> GetSuggestionById(Guid id)
        {
            var result = _suggestionRepository.GetById(id);
            return Ok(result);
        }

        [HttpGet("user/{id}")]
        public ActionResult<IEnumerable<SuggestionDTO>> GetSuggestionsByUserId(Guid id)
        {
            var result = _suggestionRepository.GetByUserId(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<SuggestionCreatedDTO> CreateSuggestion([FromBody] SuggestionCreationDTO suggestion)
        {
            var result = _suggestionRepository.Create(suggestion);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<SuggestionCreatedDTO> UpdateSuggestion([FromBody] SuggestionUpdateDTO suggestion)
        {
            var result = _suggestionRepository.Update(suggestion);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestion(Guid id)
        {
            _suggestionRepository.Delete(id);
            return NoContent();
        }
    }
}