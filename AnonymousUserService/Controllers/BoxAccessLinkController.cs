using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.BoxAccessLink;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{

    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]

    public class BoxAccessLinkController : Controller
    {
        private readonly IBoxAccessLinkRepository _boxAccessLinkRepository;
        private readonly IMapper _mapper;

        public BoxAccessLinkController(IBoxAccessLinkRepository boxAccessLinkRepository, IMapper mapper)
        {
            _boxAccessLinkRepository = boxAccessLinkRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BoxAccessLinkDTO>> GetAllBoxAccessLinks ()
        {
            var result = _boxAccessLinkRepository.GetAllBoxAccessLinks();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<BoxAccessLinkDTO> GetBoxAccessLinkById(Guid id)
        {
            var result = _boxAccessLinkRepository.GetBoxAccessLinkById(id);
            return Ok(result);
        }
    }
}
