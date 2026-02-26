using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;
using SubscriptionService.ServiceCalls;
using SubscriptionService.Models.ExternalDTOs;
using SubscriptionService.Enums;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionRepository _repository;
        private readonly BillingServiceCall _billingServiceCall;

        public SubscriptionController(
            ISubscriptionRepository repository,
            BillingServiceCall billingServiceCall)
        {
            _repository = repository;
            _billingServiceCall = billingServiceCall;
        }

        // GET: api/subscription
        [HttpGet]
        public ActionResult<IEnumerable<SubscriptionDTO>> GetAll()
        {
            return Ok(_repository.GetAllSubscriptions());
        }

        // GET: api/subscription/{id}
        [HttpGet("{id}")]
        public ActionResult<SubscriptionDTO> GetById(Guid id)
        {
            var result = _repository.GetSubscriptionById(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/subscription
        [HttpPost]
        public async Task<ActionResult<SubscriptionCreatedDTO>> Create(
            [FromBody] SubscriptionCreationDTO dto)
        {
            var result = _repository.CreateSubscription(dto);

            // 🔥 Poziv BillingService
            await _billingServiceCall.CreateBillingNotificationAsync(
                new BillingNotificationCreateDTO
                {
                    Text = "Subscription successfully created.",
                    OrganizationId = dto.OrganizationId,
                    PaymentId = Guid.NewGuid(), // ili pravi PaymentId ako postoji
                    Type = TypeSubject.BillingNotification
                });

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }

        // PUT: api/subscription
        [HttpPut]
        public ActionResult<SubscriptionCreatedDTO> Update(
            [FromBody] SubscriptionDTO dto)
        {
            var result = _repository.UpdateSubscription(dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // DELETE: api/subscription/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            _repository.DeleteSubscription(id);
            return NoContent();
        }
    }
}