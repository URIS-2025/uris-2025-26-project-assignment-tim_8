using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : Controller
    {

        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper; // ne koristimo maper u controlerima da li da ga izbacimo
        public OrganizationController(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
             _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<OrganizationDTO>> GetAllOrganizations() 
        {
            var result = _organizationRepository.GetAllOrganizations();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<OrganizationDTO> GetOrganizationById(Guid id) 
        {
            var result = _organizationRepository.GetOrganizationById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<OrganizationCreatedDTO> CreateOrganization([FromBody] OrganizationCreationDTO organization)
        {
            var result = _organizationRepository.CreateOrganization(organization);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<OrganizationCreatedDTO> UpdateOrganization([FromBody] OrganizationDTO organization) 
        {
            var result = _organizationRepository.UpdateOrganization(organization);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteOrganization(Guid id)
        {
            _organizationRepository.DeleteOrganization(id);
            return NoContent();
        }
    }
}
