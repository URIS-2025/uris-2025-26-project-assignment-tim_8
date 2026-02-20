using AnonymousDomain.Models.AnonymousUser;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class AnonymousUserController : Controller
    {

        //private readonly IAnonymousUserService _anonymousUserService;
        //private readonly IMapper _mapper;
        public AnonymousUserController(/* IAnonymousUserService anonymousUserService, IMapper mapper*/)
        {
            // _anonymousUserService = anonymousUserService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<AnonymousUser>> GetAllAnonymousUsers() //TODO Promeniti povratnu vrednost na DTO,
        {
            // var result = _anonymousUserService.GetAll();
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpGet("{id}")]
        public ActionResult<AnonymousUser> GetAnonymousUserById(Guid id) //TODO Promeniti povratnu vrednost na DTO
        {
            // var result = _anonymousUserService.GetById(id);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPost]
        public ActionResult<AnonymousUser> CreateAnonymousUser([FromBody] AnonymousUser anonymousUser) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _anonymousUserService.Create(anonymousUser);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpPut]
        public ActionResult<AnonymousUser> UpdateAnonymousUser([FromBody] AnonymousUser anonymousUser) //TODO Promeniti povratnu vrednost na DTO, promeniti prosledjenu vrednost na DTO
        {
            // var result = _anonymousUserService.Update(anonymousUser);
            // return Ok(result);
            return null; // TODO skloniti kada se urade servisi
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAnonymousUsere(Guid id)
        {
            // _anonymousUserService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
