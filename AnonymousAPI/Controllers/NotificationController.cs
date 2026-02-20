using AnonymousDomain.Models.SystemNotification;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{

    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]

    public class NotificationController : Controller
    {
        // private readonly INotificationService _notificationService;
        // private readonly IMapper _mapper;

        public NotificationController(/* INotificationService notificationService IMapper mapper*/) 
        { 
            // _notificationService = notificationService;
            // _mapper = mapper;
        }

        [HttpPost] 
        public ActionResult<SystemNotification> CreateNotification([FromBody] SystemNotification notification)
        {
            // var result = _notificationService.Create(Notification);
            // return Created("", result);
            return null; // TODO skloniti kada se urade servisi 
        }


        [HttpDelete("{id}")]
        public IActionResult DeleteNotification(Guid id)
        {
            // _notificationService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }

    }
}
