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
        
        public UserRoleController(IUserRoleRepository userRoleRepository)
        {
            _userRoleRepository = userRoleRepository;
           
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
        public ActionResult<UserRoleCreatedDTO> CreateUserRole([FromBody] UserRoleCreationDTO userRole) 
        {
            var result = _userRoleRepository.CreateUserRole(userRole);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<UserRoleCreatedDTO> UpdateUserRole([FromBody] UserRoleDTO userRole) 
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
