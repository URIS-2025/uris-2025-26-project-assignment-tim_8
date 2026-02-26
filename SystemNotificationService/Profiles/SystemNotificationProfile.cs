using AutoMapper;
using AnonymousDomain.Models.SystemNotification;
using SystemNotificationService.Models.DTOs.SystemNotification;



namespace SystemNotificationService.Profiles
{
    public class SystemNotificationProfile : Profile
    {
        public SystemNotificationProfile()
        {
            CreateMap<SystemNotificationCreationDTO, SystemNotification>()
                .ReverseMap();
            CreateMap<SystemNotification, SystemNotificationDTO>()
                .ReverseMap();
            CreateMap<SystemNotification, SystemNotificationCreatedDTO>()
                .ReverseMap();
            CreateMap<SystemNotificationUpdateDTO, SystemNotification>()
                .ReverseMap();
        }
    }
}
