using AnonymousDomain.Models.Subscription;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionPlanController : Controller
    {
        // private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlanController(/* ISubscriptionPlanService subscriptionPlanService */)
        {
            // _subscriptionPlanService = subscriptionPlanService;
        }

        // GET: api/subscriptionplan
        [HttpGet]
        public ActionResult<IEnumerable<SubscriptionPlan>> GetAllSubscriptionPlans()
        {
            // var result = _subscriptionPlanService.GetAll();
            // return Ok(result);
            return null; // TODO ukloniti kad se urade servisi
        }

        // GET: api/subscriptionplan/{id}
        [HttpGet("{id}")]
        public ActionResult<SubscriptionPlan> GetSubscriptionPlanById(Guid id)
        {
            // var result = _subscriptionPlanService.GetById(id);
            // return Ok(result);
            return null;
        }
    }
}
