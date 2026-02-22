using AnonymousDomain.Models.Subscription;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {
        // private readonly IPaymentService _paymentService;
        // private readonly IMapper _mapper;

        public PaymentController(/* IPaymentService paymentService, IMapper mapper */)
        {
            // _paymentService = paymentService;
            // _mapper = mapper;
        }

        // GET: api/payment
        [HttpGet]
        public ActionResult<IEnumerable<Payment>> GetAllPayments()
        {
            // var result = _paymentService.GetAll();
            // return Ok(result);
            return null; // TODO obrisati kad se urade servisi
        }

        // GET: api/payment/{id}
        [HttpGet("{id}")]
        public ActionResult<Payment> GetPaymentById(Guid id)
        {
            // var result = _paymentService.GetById(id);
            // return Ok(result);
            return null;
        }

        // GET: api/payment/bySubscription/{subscriptionId}
        [HttpGet("bySubscription/{subscriptionId}")]
        public ActionResult<IEnumerable<Payment>> GetPaymentsBySubscriptionId(Guid subscriptionId)
        {
            // var result = _paymentService.GetBySubscriptionId(subscriptionId);
            // return Ok(result);
            return null;
        }

        // POST: api/payment
        [HttpPost]
        public ActionResult<Payment> CreatePayment([FromBody] Payment payment)
        {
            // var result = _paymentService.Create(payment);
            // return Created("", result);
            return null;
        }

        // PUT: api/payment/{id}
        [HttpPut("{id}")]
        public ActionResult<Payment> UpdatePayment(Guid id, [FromBody] Payment payment)
        {
            // var result = _paymentService.Update(id, payment);
            // return Ok(result);
            return null;
        }

        // DELETE: api/payment/{id}
        [HttpDelete("{id}")]
        public IActionResult DeletePayment(Guid id)
        {
            // _paymentService.Delete(id);
            // return NoContent();
            return null;
        }
    }
}
