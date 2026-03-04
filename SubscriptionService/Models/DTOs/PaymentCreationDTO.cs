    namespace SubscriptionService.Models.DTOs
    {
        public class PaymentCreationDTO
        {
            public Guid SubscriptionId { get; set; }
            public double Total { get; set; }
            public string Currency { get; set; }
            public string PaymentMethod { get; set; }
        }
    }