using SystemNotificationService.Models.DTOs.SystemNotification;

namespace SystemNotificationService.Data
{
    public interface ISystemNotificationRepository
    {
        SystemNotificationCreatedDTO CreateSystemNotification(SystemNotificationCreationDTO systemNotification);
        void DeleteSystemNotification(Guid id);
    }
}
