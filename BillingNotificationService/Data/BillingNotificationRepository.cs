using AnonymousDomain.Models.BillingNotification;
using AutoMapper;
using BillingNotificationService.Context;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;

namespace BillingNotificationService.Data
{
    public class BillingNotificationRepository : IBillingNotificationRepository
    {

        private readonly BillingNotificationContext _context;
        private readonly IMapper _mapper;

        public BillingNotificationRepository(BillingNotificationContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public BillingNotificationCreatedDTO CreateBillingNotification(BillingNotificationCreationDTO billingNotification)
        {
            var entity = _mapper.Map<BillingNotification>(billingNotification);
            _context.BillingNotifications.Add(entity);
            SaveChanges();
            return _mapper.Map<BillingNotificationCreatedDTO>(entity);
        }

        public void DeleteBillingNotification(Guid id)
        {
            var billingNotification = _context.BillingNotifications.Find(id);
            if (billingNotification != null)
            {
                _context.Remove(billingNotification);
                _context.SaveChanges();
            }
        }

        public IEnumerable<BillingNotificationDTO> GetAllBillingNotifications()
        {
            var billingNotifications = _context.BillingNotifications.ToList();  
            var billingNotificationResult = new List<BillingNotificationDTO>(); 

            foreach( var billingNotification in billingNotifications)
            {
                var dto = _mapper.Map<BillingNotificationDTO>(billingNotification);
                billingNotificationResult.Add(dto);
            }

            return billingNotificationResult;
        }

        public BillingNotificationDTO GetBillingNotificationById(Guid id)
        {
            var billingNotification = _context.BillingNotifications.Find(id);
            if (billingNotification == null)
            {
                return null;
            }
            return _mapper.Map<BillingNotificationDTO>(billingNotification);
        }

        public BillingNotificationDTO UpdateBillingNotification(BillingNotificationUpdateDTO billingNotification)
        {
            var existingBillingNotification = _context.BillingNotifications.Find(billingNotification.Id);
            if (existingBillingNotification != null)
            {
                _mapper.Map(billingNotification, existingBillingNotification);
                _context.SaveChanges();
            }
            return _mapper.Map<BillingNotificationDTO>(existingBillingNotification);
        }
    }
}
