using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ProblemBoxService.Data;
using ProblemBoxService.Models;
using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemBoxController : Controller
    {

        private readonly IProblemBoxRepository _problemBoxRepository;
        private readonly IMapper _mapper;
        public ProblemBoxController(IProblemBoxRepository problemBoxRepository, IMapper mapper)
        {
            _problemBoxRepository = problemBoxRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ProblemBoxDTO>> GetProblemBoxes()
        {
            return Ok(new List<ProblemBoxDTO>());
        }

        [HttpGet("organization/{id}")]
        public ActionResult<IEnumerable<ProblemBoxDTO>> GetProblemBoxByOrganizationId(Guid id)
        {
            var result = _problemBoxRepository.GetProblemBoxByOrganizationId(id);
            return Ok(result);
            
        }

        [HttpGet("{id}")]
        public ActionResult<ProblemBoxDTO> GetProblemBoxById(Guid id)
        {
            var result = _problemBoxRepository.GetProblemBoxById(id);
            return Ok(result);
            
        }

        [HttpGet("boxaccesslink/{boxAccessLinkId}")]
        public ActionResult<ProblemBoxDTO> GetProblemBoxByAccessLinkId(Guid boxAccessLinkId)
        {
            var result = _problemBoxRepository.GetProblemBoxByAccessLinkId(boxAccessLinkId);
            return Ok(result);
           
        }

        [HttpPost]
        public ActionResult<ProblemBoxCreatedDTO> CreateProblemBox([FromBody] ProblemBoxCreationDTO problemBox)   
        {
            var result = _problemBoxRepository.CreateProblemBox(problemBox);
            return Created("", result);
           
        }

        [HttpPut]
        public ActionResult<ProblemBoxDTO> UpdateProblemBox([FromBody] ProblemBoxUpdateDTO problemBox)
        {
            var result = _problemBoxRepository.UpdateProblemBox(problemBox);
            return Ok(result);
            
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProblemBox(Guid id)
        {
            _problemBoxRepository.DeleteProblemBox(id);
             return NoContent();
           
        }
    }
}
