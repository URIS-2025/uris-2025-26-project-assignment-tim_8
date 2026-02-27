using AnonymousRepository.Interfaces;
using AnonymousDomain.Models.Suggestion.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SuggestionService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionCommentController : Controller
    {
        private readonly ISuggestionCommentRepository _suggestionCommentRepository;
        private readonly IMapper _mapper;

        public SuggestionCommentController(ISuggestionCommentRepository suggestionCommentRepository, IMapper mapper)
        {
            _suggestionCommentRepository = suggestionCommentRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SuggestionCommentDTO>> GetAllSuggestionComments()
        {
            var result = _suggestionCommentRepository.GetAll();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<SuggestionCommentDTO> GetSuggestionCommentById(Guid id)
        {
            var result = _suggestionCommentRepository.GetById(id);
            return Ok(result);
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<SuggestionCommentDTO>> GetSuggestionCommentsBySuggestionId(Guid id)
        {
            var result = _suggestionCommentRepository.GetBySuggestionId(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<SuggestionCommentCreationDTO> CreateSuggestionComment([FromBody] SuggestionCommentCreationDTO suggestionComment)
        {
            var result = _suggestionCommentRepository.Create(suggestionComment);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<SuggestionCommentUpdateDTO> UpdateSuggestionComment([FromBody] SuggestionCommentUpdateDTO suggestionComment)
        {
            var result = _suggestionCommentRepository.Update(suggestionComment);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteSuggestionComment(Guid id)
        {
            _suggestionCommentRepository.Delete(id);
            return NoContent();
        }
    }
}