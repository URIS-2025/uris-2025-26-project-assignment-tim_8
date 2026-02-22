using AnonymousDomain.Models.Subscription;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : Controller
    {
        // private readonly ISubscriptionService _subscriptionService;
        // private readonly IMapper _mapper;

        public SubscriptionController(/* ISubscriptionService subscriptionService, IMapper mapper */)
        {
            // _subscriptionService = subscriptionService;
            // _mapper = mapper;
        }

        // GET: api/subscription
        [HttpGet]
        public ActionResult<IEnumerable<Subscription>> GetAllSubscriptions()
        {
            // var result = _subscriptionService.GetAll();
            // return Ok(result);
            return null; // TODO obrisati kad uradimo servise
        }

        // GET: api/subscription/{id}
        [HttpGet("{id}")]
        public ActionResult<Subscription> GetSubscriptionById(Guid id)
        {
            // var result = _subscriptionService.GetById(id);
            // return Ok(result);
            return null;
        }

        // GET: api/subscription/byUser/{userId}
        [HttpGet("byUser/{userId}")]
        public ActionResult<IEnumerable<Subscription>> GetSubscriptionsByUserId(Guid userId)
        {
            // var result = _subscriptionService.GetByUserId(userId);
            // return Ok(result);
            return null;
        }

        // POST: api/subscription
        [HttpPost]
        public ActionResult<Subscription> CreateSubscription([FromBody] Subscription subscription)
        {
            // var result = _subscriptionService.Create(subscription);
            // return Created("", result);
            return null;
        }

        // PUT: api/subscription/{id}
        [HttpPut("{id}")]
        public ActionResult<Subscription> UpdateSubscription(Guid id, [FromBody] Subscription subscription)
        {
            // var result = _subscriptionService.Update(id, subscription);
            // return Ok(result);
            return null;
        }

        // DELETE: api/subscription/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteSubscription(Guid id)
        {
            // _subscriptionService.Delete(id);
            // return NoContent();
            return null;
        }
    }
}
