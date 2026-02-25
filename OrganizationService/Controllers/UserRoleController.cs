using AnonymousDomain.Models.Organization;
using AutoMapper;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class UserRoleController : Controller
    {

        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IMapper _mapper; // ne koristimo maper u controlerima da li da ga izbacimo
        public UserRoleController(IUserRoleRepository userRoleRepository, IMapper mapper)
        {
            _userRoleRepository = userRoleRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserRoleDTO>> GetAllUserRoles() 
        {
            var result = _userRoleRepository.GetAllUserRoles();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<UserRoleDTO> GetUserRoleById(Guid id) 
        {
            var result = _userRoleRepository.GetUserRoleById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<UserRoleCreatedDTO> CreateUserRole([FromBody] UserRoleCreationDTO userRole) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            var result = _userRoleRepository.CreateUserRole(userRole);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<UserRoleCreatedDTO> UpdateUserRole([FromBody] UserRoleDTO userRole) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
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
