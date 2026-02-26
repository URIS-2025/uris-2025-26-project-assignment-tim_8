namespace SubscriptionService.Models.DTOs
{
    public class PaymentCreatedDTO
    {
        public Guid Id { get; set; }
        public double Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}