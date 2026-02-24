using AnonymousDomain.Models.BillingNotification;
using AutoMapper;
using BillingNotificationService.Context;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;
using BillingNotificationService.Validations;

namespace BillingNotificationService.Data
{
    public class BillingNotificationRepository : IBillingNotificationRepository
    {

        private readonly BillingNotificationContext _context;
        private readonly IMapper _mapper;
        private readonly NotEmptyGuidAttribute _validation;

        public BillingNotificationRepository(BillingNotificationContext context, IMapper mapper, NotEmptyGuidAttribute validation)
        {
            _mapper = mapper;
            _context = context;
            _validation = validation;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public BillingNotificationCreatedDTO CreateBillingNotification(BillingNotificationCreationDTO billingNotification)
        {
            var paymentValidation = _validation.IsValid(billingNotification.PaymentId);
            if (!paymentValidation)
                throw new ArgumentException("PaymentId must be provided.");

            var organizationValidation = _validation.IsValid(billingNotification.OrganizationId);
            if (!organizationValidation)
                throw new ArgumentException("OrganizationId must be provided.");

            var entity = _mapper.Map<BillingNotification>(billingNotification);
            _context.BillingNotifications.Add(entity);
            SaveChanges();
            return _mapper.Map<BillingNotificationCreatedDTO>(entity);
        }

        public void DeleteBillingNotification(Guid id)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public BillingNotificationDTO UpdateBillingNotification(BillingNotificationUpdateDTO billingNotification)
        {
            throw new NotImplementedException();
        }
    }
}
