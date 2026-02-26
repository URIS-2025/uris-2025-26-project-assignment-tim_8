using AutoMapper;
using SubscriptionService.Models;
using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Profiles
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<PaymentCreationDTO, Payment>().ReverseMap();
            CreateMap<Payment, PaymentDTO>().ReverseMap();
            CreateMap<Payment, PaymentCreatedDTO>().ReverseMap();
        }
    }
}