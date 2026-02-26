using AnonymousDomain.Models.BillingNotification;
using AutoMapper;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;

namespace BillingNotificationService.Profiles
{
    public class BillingNotificationProfile : Profile
    {
        public BillingNotificationProfile()
        {
            CreateMap<BillingNotificationCreationDTO, BillingNotification>()
                .ReverseMap();
            CreateMap<BillingNotification, BillingNotificationDTO>()
                .ReverseMap();
            CreateMap<BillingNotification, BillingNotificationCreatedDTO>()
                .ReverseMap();
            CreateMap<BillingNotificationUpdateDTO, BillingNotification>()
                .ReverseMap();
        }
    }
}
