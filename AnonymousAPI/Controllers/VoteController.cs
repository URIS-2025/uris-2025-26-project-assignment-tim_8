using AnonymousDomain.Models.Suggestion;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class VoteController : Controller
    {
        //private readonly IVote _voteService;
        //private readonly IMapper _mapper;
        public VoteController(/* IVoteService voteService, IMapper mapper*/)
        {
            // _voteService = voteService;
            // _mapper = mapper;
        }

        [HttpGet("suggestion/{id}")]
        public ActionResult<IEnumerable<Vote>> GetVotesBySuggestionId(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _voteService.GetBySuggestionId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<Vote> CreateVote([FromBody] Vote vote) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _voteService.Create(vote);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteVote(Guid id)
        {
            // _voteService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
