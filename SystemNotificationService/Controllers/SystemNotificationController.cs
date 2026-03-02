using AnonymousDomain.Models.SystemNotification;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SystemNotificationService.Data;
using SystemNotificationService.Models.DTOs.SystemNotification;

namespace AnonymousAPI.Controllers
{

    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]

    public class SystemNotificationController : Controller
    {
        private readonly ISystemNotificationRepository _systemNotificationRepository;
        private readonly IMapper _mapper;

        public SystemNotificationController(ISystemNotificationRepository notificationRepository, IMapper mapper) 
        { 
            _systemNotificationRepository = notificationRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SystemNotificationCreatedDTO>> GetNotifications()
        {
            return Ok(new List<SystemNotificationCreatedDTO>());
        }

        [HttpPost] 
        public ActionResult<SystemNotificationCreatedDTO> CreateSystemNotification([FromBody] SystemNotificationCreationDTO notification)
        {
            var result = _systemNotificationRepository.CreateSystemNotification(notification);
            return Created("", result);
        }


        [HttpDelete("{id}")]
        public IActionResult DeleteSystemNotification(Guid id)
        {
            _systemNotificationRepository.DeleteSystemNotification(id);
            return NoContent();
        }

    }
}
