namespace BillingNotificationService.Models.DTOs.Organization
{
    public class OrganizationDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid AdminId { get; set; }
    }
}
