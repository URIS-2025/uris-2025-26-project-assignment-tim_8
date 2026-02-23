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
    public class UserController : Controller
    {

        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public UserController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAllUsers() //TODO Promeniti povratnu vrednost na DTO,
        {
            var result = _userRepository.GetAllUsers();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<User> GetUserById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            var result = _userRepository.GetUserById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<User> CreateUser([FromBody] User user ) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            var result = _userRepository.CreateUser(user);
            return Created("", result);
        }

        [HttpPut]
        public ActionResult<User> UpdateUser([FromBody] User user ) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
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
    }
}
