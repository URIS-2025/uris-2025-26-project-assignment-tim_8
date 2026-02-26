namespace SubscriptionService.Models.DTOs
{
    public class SubscriptionCreationDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public Guid OrganizationId { get; set; }
    }
}