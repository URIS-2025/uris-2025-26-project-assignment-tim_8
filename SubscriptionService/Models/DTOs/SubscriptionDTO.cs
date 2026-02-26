namespace SubscriptionService.Models.DTOs
{
    public class SubscriptionDTO
    {
        public Guid Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public Guid OrganizationId { get; set; }
    }
}