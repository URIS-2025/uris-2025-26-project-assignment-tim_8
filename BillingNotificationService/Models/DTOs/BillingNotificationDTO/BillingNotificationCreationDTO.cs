using System.ComponentModel.DataAnnotations;

namespace BillingNotificationService.Models.DTOs.BillingNotificationDTO
{
    public class BillingNotificationCreationDTO
    {

      
        public string Text { get; set; }

    
        public Guid OrganizationId { get; set; }

        public Guid PaymentId { get; set; }
    }
}
