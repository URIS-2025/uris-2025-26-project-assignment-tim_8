using AnonymousDomain.Models.BillingNotification;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{

    //[Authorize] 
    [ApiController]
    [Route("api/[controller]")]

    public class BillingNotificationController : Controller
    {
        // private readonly IBillingNotificationService _billingnotificationService;
        // private readonly IMapper _mapper;

        public BillingNotificationController(/* billingnotificationService, mapper*/)
        {
            // _billingnotificationService = billingnotificationService;
            // _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BillingNotification>> GetAllBillingNotifications()
        {
            // var result = _billingnotificationService.GetAll();
            // return Ok("",result);
            return null;
        }

        [HttpGet("{id}")]
        public ActionResult<BillingNotification> GetBillingNotificationById(Guid id)
        {
            // var result = _billingnotificationService.GetById(id);
            // return Ok("",result);
            return null;
        }

        [HttpPost]
        public ActionResult<BillingNotification> CreateBillingNotification([FromBody]BillingNotification billingNotification)
        {
            // var result = _billingnotificationService.Create(billingNotification);
            // return Created("",result);
            return null;
        }

        [HttpPut]
        public ActionResult<BillingNotification> UpdateBillingNotification([FromBody] BillingNotification billingNotification)
        {
            // var result = _billingnotificationService.Update(billingNotification);
            // return Updated("",result);
            return null;
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBillingNotification(Guid id)
        {
            // _billingnotificationService.Delete(id);
            // return NoContent();
            return null; // TODO skloniti kada se urade servisi 
        }
    }
}
