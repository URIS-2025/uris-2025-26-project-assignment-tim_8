using AnonymousDomain.Models.Problem;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemCommentController : Controller
    {

        //private readonly IProblemCommentService _problemCommentService;
        //private readonly IMapper _mapper;
        public ProblemCommentController(/* IProblemCommentService problemCommentService, IMapper mapper*/)
        {
            // _problemCommentService = problemCommentService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemComment>> GetAllProblemComments() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _problemCommentService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemComment> GetProblemCommentById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _problemCommentService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<ProblemComment> CreateProblemComment([FromBody] ProblemComment problemComment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemCommentService.Create(problemComment);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<ProblemComment> UpdateProblemComment([FromBody] ProblemComment problemComment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemCommentService.Update(problemComment);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblemComment(Guid id)
        {
            // _problemCommentService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
