namespace BillingNotificationService.Models.DTOs.BillingNotificationDTO
{
    public class BillingNotificationDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsRead { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid PaymentId { get; set; }

    }
}
