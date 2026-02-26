using Microsoft.AspNetCore.Mvc;
using SubscriptionService.Data;
using SubscriptionService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly ISubscriptionPlanRepository _repository;

        public SubscriptionPlanController(ISubscriptionPlanRepository repository)
        {
            _repository = repository;
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
    }
}