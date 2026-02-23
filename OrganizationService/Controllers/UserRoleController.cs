using AnonymousDomain.Models.Organization;
using AutoMapper;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Data;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class UserRoleController : Controller
    {

        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IMapper _mapper;
        public UserRoleController(IUserRoleRepository userRoleRepository, IMapper mapper)
        {
            _userRoleRepository = userRoleRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserRole>> GetAllUserRoles() //TODO Promeniti povratnu vrednost na DTO,
        {
            var result = _userRoleRepository.GetAllUserRoles();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<UserRole> GetUserRoleById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            var result = _userRoleRepository.GetUserRoleById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<UserRole> CreateUserRole([FromBody] UserRole userRole) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            var result = _userRoleRepository.CreateUserRole(userRole);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<UserRole> UpdateUserRole([FromBody] UserRole userRole) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            var result = _userRoleRepository.UpdateUserRole(userRole);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUserRole(Guid id)
        {
            _userRoleRepository.DeleteUserRole(id);
            return NoContent();
        }
    }
}
