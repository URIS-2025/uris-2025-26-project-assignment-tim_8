using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public interface IPaymentRepository
    {
        IEnumerable<PaymentDTO> GetAllPayments();

        PaymentDTO GetPaymentById(Guid id);

        IEnumerable<PaymentDTO> GetPaymentsBySubscriptionId(Guid subscriptionId);

        PaymentCreatedDTO CreatePayment(PaymentCreationDTO payment);

        PaymentCreatedDTO UpdatePayment(PaymentDTO payment);

        void DeletePayment(Guid id);
    }
}