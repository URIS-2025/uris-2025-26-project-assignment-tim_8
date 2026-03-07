using AutoMapper;
using SubscriptionService.Models;
using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Profiles
{
    public class SubscriptionPlanProfile : Profile
    {
        public SubscriptionPlanProfile()
        {
            CreateMap<SubscriptionPlan, SubscriptionPlanDTO>().ReverseMap();
            CreateMap<SubscriptionPlan, SubscriptionCreatedDTO>().ReverseMap();
        }
    }
}