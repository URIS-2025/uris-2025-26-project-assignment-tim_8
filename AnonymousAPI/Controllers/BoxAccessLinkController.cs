using AnonymousDomain.Models.AnonymousUser;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{

    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]

    public class BoxAccessLinkController : Controller
    {
        //private readonly IBoxAccessLinkService _boxaccesslinkService;
        //private readonly IMapper _mapper;

        public BoxAccessLinkController(/* boxaccesslinkService, mapper */)
        {
            // _boxaccesslinkService = boxaccesslinkService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BoxAccessLink>> GetAllBoxAccessLinks ()
        {
            // var result = _boxaccesslinkService.GetAll();
            // return Ok(result);
            return null; //TODO obrisati kad uradimo servise
        }

        [HttpGet("{id}")]
        public ActionResult<BoxAccessLink> GetBoxAccessLinkById (Guid id)
        {
            // var result = _boxaccesslinkService.GetById(id);
            // return Ok(result)
            return null; 
        }

        [HttpPost]
        public ActionResult<BoxAccessLink> CreateBoxAccessLink([FromBody] BoxAccessLink boxaccesslink)
        {
            // var result = _boxaccesslinkService.Create(boxaccesslink);
            // return Created("", result);
            return null; 
        }

        [HttpPut]
        public ActionResult<BoxAccessLink> UpdateBoxAccessLink([FromBody] BoxAccessLink boxaccesslink)
        {
            // var result = _boxaccesslinkService.Update(boxaccesslink);
            // return Updated("",result);
            return null;
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBoxAccessLink(Guid id)
        {
            // _boxaccesslinkService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
