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
    public class UserController : Controller
    {

        private readonly IUserRepository _userRepository;
        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
          
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserDTO>> GetAllUsers() 
        {
            var result = _userRepository.GetAllUsers();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<UserDTO> GetUserById(Guid id) 
        {
            var result = _userRepository.GetUserById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<UserCreatedDTO> CreateUser([FromBody] UserCreationDTO user ) 
        {
            var result = _userRepository.CreateUser(user);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<UserCreatedDTO> UpdateUser([FromBody] UserUpdateDTO user ) 
        {
            var result = _userRepository.UpdateUser(user);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(Guid id)
        {
            _userRepository.DeleteUser(id);
            return NoContent();
        }
        [HttpPost("login")]
        public ActionResult<string> Login([FromBody] UserLoginDTO login)
        {
            var token = _userRepository.Login(login);
            return Ok(token);
        }
    }
}
