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
    public class VoteController : Controller
    {
        private readonly IVoteRepository _voteRepository;
        private readonly IMapper _mapper;

        public VoteController(IVoteRepository voteRepository, IMapper mapper)
        {
            _voteRepository = voteRepository;
            _mapper = mapper;
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<VoteDTO>> GetVotesBySuggestionId(Guid id)
        {
            var result = _voteRepository.GetBySuggestionId(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<VoteCreationDTO> CreateVote([FromBody] VoteCreationDTO vote)
        {
            var result = _voteRepository.Create(vote);
            return Created("", result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteVote(Guid id)
        {
            _voteRepository.Delete(id);
            return NoContent();
        }
    }
}