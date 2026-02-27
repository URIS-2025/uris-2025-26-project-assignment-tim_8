using ProblemService.Models.Problem;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemService.Data;
using AutoMapper;
using ProblemService.Models.DTOs;

namespace ProblemService.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemCategoryController : Controller
    {

        private readonly IProblemCategoryRepository _problemCategoryRepository;
        private readonly IMapper _mapper;
        public ProblemCategoryController(IProblemCategoryRepository problemCategoryRepository, IMapper mapper)
        {
            _problemCategoryRepository = problemCategoryRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemCategoryDTO>> GetAllProblemCategories()
        {
            var result = _problemCategoryRepository.GetAllProblemCategories();
            return Ok(result);
            
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemCategoryDTO> GetProblemCategoryById(Guid id)
        {
            var result = _problemCategoryRepository.GetProblemCategoryById(id);
            return Ok(result);
           
        }

        [HttpPost]
        public ActionResult<ProblemCategoryCreatedDTO> CreateProblemCategory([FromBody] ProblemCategoryCreationDTO problemCategory) 
        {
            var result = _problemCategoryRepository.CreateProblemCategory(problemCategory);
            return Created("", result);
            
        }

        [HttpPut]
        public ActionResult<ProblemCategoryDTO> UpdateProblemCategory([FromBody] ProblemCategoryUpdateDTO problemCategory)
        {
            var result = _problemCategoryRepository.UpdateProblemCategory(problemCategory);
            return Ok(result);
            
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblemCategory(Guid id)
        {
            _problemCategoryRepository.DeleteProblemCategory(id);
            return NoContent();
            
        }
    }
}
