namespace SubscriptionService.Models.DTOs
{
    public class SubscriptionCreatedDTO
    {
        public Guid Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}