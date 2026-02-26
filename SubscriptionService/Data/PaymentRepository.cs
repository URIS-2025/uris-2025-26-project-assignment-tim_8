using SubscriptionService.Models;
using SubscriptionService.Enums;
using AutoMapper;
using SubscriptionService.Context;
using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly SubscriptionContext _context;
        private readonly IMapper _mapper;

        public PaymentRepository(SubscriptionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<PaymentDTO> GetAllPayments()
        {
            var payments = _context.Payments.ToList();
            return _mapper.Map<IEnumerable<PaymentDTO>>(payments);
        }

        public PaymentDTO GetPaymentById(Guid id)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == id);

            if (payment == null)
                return null;

            return _mapper.Map<PaymentDTO>(payment);
        }

        public IEnumerable<PaymentDTO> GetPaymentsBySubscriptionId(Guid subscriptionId)
        {
            var payments = _context.Payments
                .Where(p => p.SubscriptionId == subscriptionId)
                .ToList();

            return _mapper.Map<IEnumerable<PaymentDTO>>(payments);
        }

        public PaymentCreatedDTO CreatePayment(PaymentCreationDTO dto)
        {
            var payment = _mapper.Map<Payment>(dto);

            payment.Id = Guid.NewGuid();
            payment.CreatedAt = DateTime.UtcNow;
            payment.Status = PaymentStatus.Pending;

            _context.Payments.Add(payment);
            SaveChanges();

            return _mapper.Map<PaymentCreatedDTO>(payment);
        }

        public PaymentCreatedDTO UpdatePayment(PaymentDTO dto)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == dto.Id);

            if (payment == null)
                return null;

            _mapper.Map(dto, payment);

            SaveChanges();

            return _mapper.Map<PaymentCreatedDTO>(payment);
        }

        public void DeletePayment(Guid id)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.Id == id);

            if (payment == null)
                return;

            _context.Payments.Remove(payment);
            SaveChanges();
        }
    }
}