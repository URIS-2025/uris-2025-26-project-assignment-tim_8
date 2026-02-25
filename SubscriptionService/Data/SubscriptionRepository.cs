using SubscriptionService.Models;
using AutoMapper;
using SubscriptionService.Context;
using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly SubscriptionContext _context;
        private readonly IMapper _mapper;

        public SubscriptionRepository(SubscriptionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<SubscriptionDTO> GetAllSubscriptions()
        {
            var subscriptions = _context.Subscriptions.ToList();
            return _mapper.Map<IEnumerable<SubscriptionDTO>>(subscriptions);
        }

        public SubscriptionDTO GetSubscriptionById(Guid id)
        {
            var subscription = _context.Subscriptions.FirstOrDefault(s => s.Id == id);

            if (subscription == null)
                return null;

            return _mapper.Map<SubscriptionDTO>(subscription);
        }

        public SubscriptionCreatedDTO CreateSubscription(SubscriptionCreationDTO dto)
        {
            var subscription = _mapper.Map<Subscription>(dto);

            subscription.Id = Guid.NewGuid();

            _context.Subscriptions.Add(subscription);
            SaveChanges();

            return _mapper.Map<SubscriptionCreatedDTO>(subscription);
        }

        public SubscriptionCreatedDTO UpdateSubscription(SubscriptionDTO dto)
        {
            var subscription = _context.Subscriptions.FirstOrDefault(s => s.Id == dto.Id);

            if (subscription == null)
                return null;

            _mapper.Map(dto, subscription);

            SaveChanges();

            return _mapper.Map<SubscriptionCreatedDTO>(subscription);
        }

        public void DeleteSubscription(Guid id)
        {
            var subscription = _context.Subscriptions.FirstOrDefault(s => s.Id == id);

            if (subscription == null)
                return;

            _context.Subscriptions.Remove(subscription);
            SaveChanges();
        }
    }
}