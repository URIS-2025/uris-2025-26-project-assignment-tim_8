using BillingNotificationService.Models.DTOs.BillingNotificationDTO;

namespace BillingNotificationService.Data
{
    public interface IBillingNotificationRepository
    {
        IEnumerable<BillingNotificationDTO> GetAllBillingNotifications();

        BillingNotificationDTO GetBillingNotificationById(Guid id);

        BillingNotificationCreatedDTO CreateBillingNotification(BillingNotificationCreationDTO billingNotification);

        BillingNotificationDTO UpdateBillingNotification(BillingNotificationUpdateDTO billingNotification);

        void DeleteBillingNotification(Guid id);



    }
}
