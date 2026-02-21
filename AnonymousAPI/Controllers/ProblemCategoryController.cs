using AnonymousDomain.Models.Problem;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemCategoryController : Controller
    {

        //private readonly IProblemCategoryService _problemCategoryService;
        //private readonly IMapper _mapper;
        public ProblemCategoryController(/* IProblemCategoryService problemCategoryService, IMapper mapper*/)
        {
            // _problemCategoryService = problemCategoryService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemCategory>> GetAllProblemCategories() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _problemCategoryService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemCategory> GetProblemCategoryById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _problemCategoryService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<ProblemCategory> CreateProblemCategory([FromBody] ProblemCategory problemCategory) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemCategoryService.Create(problemCategory);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<ProblemCategory> UpdateProblemCategory([FromBody] ProblemCategory problemCategory) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _problemCategoryService.Update(problemCategory);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblemCategory(Guid id)
        {
            // _problemCategoryService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
