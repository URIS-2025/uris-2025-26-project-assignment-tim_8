using AutoMapper;
using SubscriptionService.Models;
using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Profiles
{
    public class SubscriptionProfile : Profile
    {
        public SubscriptionProfile()
        {
            CreateMap<SubscriptionCreationDTO, Subscription>().ReverseMap();
            CreateMap<Subscription, SubscriptionDTO>().ReverseMap();
            CreateMap<Subscription, SubscriptionCreatedDTO>().ReverseMap();
        }
    }
}