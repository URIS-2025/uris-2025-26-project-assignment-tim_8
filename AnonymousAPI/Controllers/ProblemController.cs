using AnonymousDomain.Models.Problem;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemController : Controller
    {

        //private readonly IProblemService _problemService;
        //private readonly IMapper _mapper;
        public ProblemController(/* IProblemService problemService, IMapper mapper*/)
        {
            // _problemService = problemService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Problem>> GetAllProblems() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _problemService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<Problem> GetProblemById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _problemService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<Problem> CreateProblem([FromBody] Problem problem) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemService.Create(problem);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<Problem> UpdateProblem([FromBody] Problem problem) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemService.Update(problem);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblem(Guid id)
        {
            // _problemService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
