using SystemNotificationService.Models.DTOs.SystemNotification;

namespace SystemNotificationService.Data
{
    public interface ISystemNotificationRepository
    {
        IEnumerable<SystemNotificationCreatedDTO> GetAllSystemNotifications();
        SystemNotificationCreatedDTO CreateSystemNotification(SystemNotificationCreationDTO systemNotification);
        void DeleteSystemNotification(Guid id);
    }
}
