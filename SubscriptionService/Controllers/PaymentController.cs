using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _repository;

        public PaymentController(IPaymentRepository repository)
        {
            _repository = repository;
        }

        // GET: api/payment
        [HttpGet]
        public ActionResult<IEnumerable<PaymentDTO>> GetAllPayments()
        {
            return Ok(_repository.GetAllPayments());
        }

        // GET: api/payment/{id}
        [HttpGet("{id}")]
        public ActionResult<PaymentDTO> GetPaymentById(Guid id)
        {
            var result = _repository.GetPaymentById(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // GET: api/payment/bySubscription/{subscriptionId}
        [HttpGet("bySubscription/{subscriptionId}")]
        public ActionResult<IEnumerable<PaymentDTO>> GetPaymentsBySubscriptionId(Guid subscriptionId)
        {
            return Ok(_repository.GetPaymentsBySubscriptionId(subscriptionId));
        }

        // POST: api/payment
        [HttpPost]
        public ActionResult<PaymentCreatedDTO> CreatePayment(
            [FromBody] PaymentCreationDTO dto)
        {
            var result = _repository.CreatePayment(dto);

            return CreatedAtAction(
                nameof(GetPaymentById),
                new { id = result.Id },
                result);
        }

        // PUT: api/payment
        [HttpPut]
        public ActionResult<PaymentCreatedDTO> UpdatePayment(
            [FromBody] PaymentDTO dto)
        {
            var result = _repository.UpdatePayment(dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // DELETE: api/payment/{id}
        [HttpDelete("{id}")]
        public IActionResult DeletePayment(Guid id)
        {
            _repository.DeletePayment(id);
            return NoContent();
        }
    }
}