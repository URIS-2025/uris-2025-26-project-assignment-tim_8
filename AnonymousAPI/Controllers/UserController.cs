using AnonymousDomain.Models.Organization;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {

        //private readonly IUserService _userService;
        //private readonly IMapper _mapper;
        public UserController(/* IUserService userService, IMapper mapper*/)
        {
            // _userService = userService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAllUsers() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _userService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<User> GetUserById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _userService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<User> CreateUser([FromBody] User user ) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _userService.Create(user);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<User> UpdateUser([FromBody] User user ) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _userService.Update(user);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(Guid id)
        {
            // _userService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
