using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Enums;
using SubscriptionService.Models.DTOs;
using SubscriptionService.Models.ExternalDTOs;
using SubscriptionService.ServiceCalls;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly ISubscriptionPlanRepository _repository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly BillingServiceCall _billingServiceCall;

        public SubscriptionPlanController(ISubscriptionPlanRepository repository,
            ISubscriptionRepository subscriptionRepository, BillingServiceCall billingServiceCall)
        {
            _repository = repository;
            _subscriptionRepository = subscriptionRepository;
            _billingServiceCall = billingServiceCall;
        }

        // GET: api/subscriptionplan
        [HttpGet]
        public ActionResult<IEnumerable<SubscriptionPlanDTO>> GetAll()
        {
            return Ok(_repository.GetAllSubscriptionPlans());
        }

        // GET: api/subscriptionplan/{id}
        [HttpGet("{id}")]
        public ActionResult<SubscriptionPlanDTO> GetById(Guid id)
        {
            var result = _repository.GetSubscriptionPlanById(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public ActionResult<SubscriptionPlanDTO> Create([FromBody] SubscriptionPlanCreationDTO plan)
        {
            var result = _repository.CreatePlan(plan);
            return Created("", result);
        }

        // PUT: api/subscriptionplan
        // On a plan change, fans out one (non-fatal) billing notification per org subscribed to the plan.
        [HttpPut]
        public async Task<ActionResult<SubscriptionPlanDTO>> Update([FromBody] SubscriptionPlanDTO plan)
        {
            var result = _repository.UpdatePlan(plan);
            if (result == null)
                return NotFound();

            await FanOutPlanChangeAsync(result);

            return Ok(result);
        }

        // Non-fatal fan-out: one billing notification per org that has a subscription to this plan.
        // Any failure (no subscriptions, billing down) is swallowed so the plan update still succeeds.
        private async Task FanOutPlanChangeAsync(SubscriptionPlanDTO plan)
        {
            try
            {
                var bearer = Request.Headers["Authorization"];
                var ct = HttpContext.RequestAborted;

                var subscriptions = _subscriptionRepository.GetSubscriptionsByPlanId(plan.Id);
                if (subscriptions == null)
                    return;

                foreach (var subscription in subscriptions)
                {
                    await _billingServiceCall.CreateBillingNotificationAsync(new BillingNotificationCreateDTO
                    {
                        Text = $"Your subscription plan '{plan.Title}' has changed.",
                        OrganizationId = subscription.OrganizationId,
                        PaymentId = Guid.NewGuid(),
                        Type = TypeSubject.BillingNotification
                    }, bearer, ct);
                }
            }
            catch { }
        }
    }
}