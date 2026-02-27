namespace SubscriptionService.Models.DTOs
{
    public class PaymentDTO
    {
        public Guid Id { get; set; }
        public double Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid SubscriptionId { get; set; }
        public string Status { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
    }
}