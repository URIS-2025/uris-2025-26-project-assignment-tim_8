using SubscriptionService.Models;
using AutoMapper;
using SubscriptionService.Context;
using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly SubscriptionContext _context;
        private readonly IMapper _mapper;

        public SubscriptionPlanRepository(SubscriptionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public IEnumerable<SubscriptionPlanDTO> GetAllSubscriptionPlans()
        {
            var plans = _context.SubscriptionPlans.ToList();
            return _mapper.Map<IEnumerable<SubscriptionPlanDTO>>(plans);
        }

        public SubscriptionPlanDTO GetSubscriptionPlanById(Guid id)
        {
            var plan = _context.SubscriptionPlans.FirstOrDefault(sp => sp.Id == id);

            if (plan == null)
                return null;

            return _mapper.Map<SubscriptionPlanDTO>(plan);
        }
    }
}