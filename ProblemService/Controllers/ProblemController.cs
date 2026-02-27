using ProblemService.Models.Problem;
using AutoMapper;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemService.Data;
using ProblemService.Models.DTOs;

namespace ProblemService.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemController : Controller
    {

        private readonly IProblemRepository _problemRepository;
        private readonly IMapper _mapper;
        public ProblemController(IProblemRepository problemRepository, IMapper mapper)
        {
            _problemRepository = problemRepository;
             _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemDTO>> GetAllProblems() 
        {
            var result = _problemRepository.GetAllProblems();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemDTO> GetProblemById(Guid id)
        {
            var result = _problemRepository.GetProblemById(id);
            return Ok(result);
        }

        [HttpGet("problembox/{problemBoxId}")]
        public ActionResult<IEnumerable<ProblemDTO>> GetProblemsByProblemBoxId(Guid problemBoxId)
        {
            var result = _problemRepository.GetProblemsByProblemBoxId(problemBoxId);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<ProblemCreatedDTO> CreateProblem([FromBody]ProblemCreationDTO problem)
        {
            var result = _problemRepository.CreateProblem(problem);
            return Created("", result);
            
        }

        [HttpPut]
        public ActionResult<ProblemDTO> UpdateProblem([FromBody] ProblemUpdateDTO problem)
        {
            var result = _problemRepository.UpdateProblem(problem);
            return Ok(result);
            
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblem(Guid id)
        {
            _problemRepository.DeleteProblem(id);
            return NoContent();
          
        }
    }
}
