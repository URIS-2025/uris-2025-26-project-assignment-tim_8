namespace BillingNotificationService.Models.DTOs.BillingNotificationDTO
{
    public class BillingNotificationUpdateDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid PaymentId { get; set; }
    }
}
