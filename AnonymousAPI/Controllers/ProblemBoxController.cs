using AnonymousDomain.Models.ProblemBox;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemBoxController : Controller
    {

        //private readonly IProblemBoxService _problemBoxService;
        //private readonly IMapper _mapper;
        public ProblemBoxController(/* IProblemBoxService problemBoxService, IMapper mapper*/)
        {
            // _problemBoxService = problemBoxService;
            // _mapper = mapper;
        }

        [HttpGet("organization/{id}")]
        public ActionResult<IEnumerable<ProblemBox>> GetProblemBoxByOrganizationId(Guid id) //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _problemBoxService.GetByOrganizationId(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemBox> GetProblemBoxById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _problemBoxService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<ProblemBox> CreateProblemBox([FromBody] ProblemBox problemBox) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemBoxService.Create(problemBox);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<ProblemBox> UpdateProblemBox([FromBody] ProblemBox problemBox) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemBoxService.Update(problemBox);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblemBox(Guid id)
        {
            // _problemBoxService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
