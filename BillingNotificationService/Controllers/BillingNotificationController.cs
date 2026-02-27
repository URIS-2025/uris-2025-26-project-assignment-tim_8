using AnonymousDomain.Models.BillingNotification;
using Microsoft.AspNetCore.Mvc;
using BillingNotificationService.Data;
using AutoMapper;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;


namespace AnonymousAPI.Controllers
{

    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]

    public class BillingNotificationController : Controller
    {
        private readonly IBillingNotificationRepository _billingNotificationRepository;
        private readonly IMapper _mapper;

        public BillingNotificationController(IBillingNotificationRepository billingNotificationRepository, IMapper mapper)
        {
             _billingNotificationRepository = billingNotificationRepository;
             _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BillingNotificationDTO>> GetAllBillingNotifications()
        {
            var result = _billingNotificationRepository.GetAllBillingNotifications();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<BillingNotificationDTO> GetBillingNotificationById(Guid id)
        {
            var result = _billingNotificationRepository.GetBillingNotificationById(id);
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<BillingNotificationCreatedDTO> CreateBillingNotification([FromBody]BillingNotificationCreationDTO billingNotification)
        {
            var result = _billingNotificationRepository.CreateBillingNotification(billingNotification);
            return Created("",result);
        }

        [HttpPut]
        public ActionResult<BillingNotificationCreatedDTO> UpdateBillingNotification([FromBody] BillingNotificationUpdateDTO billingNotification)
        {
            var result = _billingNotificationRepository.UpdateBillingNotification(billingNotification);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBillingNotification(Guid id)
        {
            _billingNotificationRepository.DeleteBillingNotification(id);
            return NoContent();
        }
    }
}
