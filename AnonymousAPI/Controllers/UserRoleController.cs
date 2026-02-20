using AnonymousDomain.Models.Organization;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class UserRoleController : Controller
    {

        //private readonly IUserRoleService _userRoleService;
        //private readonly IMapper _mapper;
        public UserRoleController(/* IUserRoleService userRoleService, IMapper mapper*/)
        {
            // _userRoleService = userRoleService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserRole>> GetAllUserRoles() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _userRoleService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<UserRole> GetUserRoleById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _userRoleService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<UserRole> CreateUserRole([FromBody] UserRole userRole) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _userRoleService.Create(userRole);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<UserRole> UpdateUserRole([FromBody] UserRole userRole) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _userRoleService.Update(userRole);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUserRole(Guid id)
        {
            // _userRoleService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
