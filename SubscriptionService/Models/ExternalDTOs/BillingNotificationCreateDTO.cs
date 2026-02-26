using System;
using SubscriptionService.Enums;

namespace SubscriptionService.Models.ExternalDTOs
{
    public class BillingNotificationCreateDTO
    {
        public string Text { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid PaymentId { get; set; }
        public TypeSubject Type { get; set; }
    }
}
