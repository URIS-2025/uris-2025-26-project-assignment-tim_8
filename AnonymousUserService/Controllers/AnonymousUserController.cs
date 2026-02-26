using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AutoMapper;
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

        private readonly IAnonymousUserRepository _anonymousUserRepository;
        private readonly IMapper _mapper;

        public AnonymousUserController(IAnonymousUserRepository anonymousUserRepository, IMapper mapper)
        {
             _anonymousUserRepository = anonymousUserRepository;
             _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<AnonymousUserDTO>> GetAllAnonymousUsers()
        {
            var result = _anonymousUserRepository.GetAllAnonymousUsers();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<AnonymousUserDTO> GetAnonymousUserById(Guid id)
        {
            var result = _anonymousUserRepository.GetAnonymousUserById(id);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAnonymousUser(Guid id)
        {
            _anonymousUserRepository.DeleteAnonymousUser(id);
            return NoContent();
        }
    }
}
