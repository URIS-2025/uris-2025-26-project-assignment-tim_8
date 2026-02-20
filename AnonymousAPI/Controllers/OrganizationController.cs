using AnonymousDomain.Models.Organization;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : Controller
    {

        //private readonly IOrganizationService _organizationService;
        //private readonly IMapper _mapper;
        public OrganizationController(/* IOrganizationService organizationService, IMapper mapper*/)
        {
            // _ogranizationService = organizationService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Organization>> GetAllOrganizations() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _organizationService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<Organization> GetOrganizationById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _organizationService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<Organization> CreateOrganization([FromBody] Organization organization) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _organizationService.Create(organization);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<Organization> UpdateOrganization([FromBody] Organization organization) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _organizationService.Update(organization);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteOrganization(Guid id)
        {
            // _organizationService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
