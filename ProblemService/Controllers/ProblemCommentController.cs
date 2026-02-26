using ProblemService.Models.Problem;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using ProblemService.Data;
using ProblemService.Models.DTOs;

namespace ProblemService.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemCommentController : Controller
    {

        private readonly IProblemCommentRepository _problemCommentRepository;
        private readonly IMapper _mapper;
        public ProblemCommentController(IProblemCommentRepository problemCommentRepository, IMapper mapper)
        {
            _problemCommentRepository = problemCommentRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemCommentDTO>> GetAllProblemComments()
        {
            var result = _problemCommentRepository.GetAllProblemComments();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemCommentDTO> GetProblemCommentById(Guid id)
        {
            var result = _problemCommentRepository.GetProblemCommentById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<ProblemCommentCreatedDTO> CreateProblemComment([FromBody] ProblemCommentCreationDTO problemComment)
        {
            var result = _problemCommentRepository.CreateProblemComment(problemComment);
            return Created("", result);
       
        }

        [HttpPut]
        public ActionResult<ProblemCommentDTO> UpdateProblemComment([FromBody] ProblemCommentUpdateDTO problemComment) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            var result = _problemCommentRepository.UpdateProblemComment(problemComment);
            return Ok(result);
           
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblemComment(Guid id)
        {
            _problemCommentRepository.DeleteProblemComment(id);
            return NoContent();
            
        }
    }
}
