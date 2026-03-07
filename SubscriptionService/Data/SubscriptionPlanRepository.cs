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

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
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
        public SubscriptionPlanDTO CreatePlan(SubscriptionPlanCreationDTO plan)
        {
            var entity = _mapper.Map<SubscriptionPlan>(plan)!;
            entity.Id = Guid.NewGuid();
            entity.Title = plan.Title;
            entity.Description = plan.Description;

            _context.SubscriptionPlans.Add(entity);
            SaveChanges();
            return _mapper.Map<SubscriptionPlanDTO>(entity);
        }
    }
}