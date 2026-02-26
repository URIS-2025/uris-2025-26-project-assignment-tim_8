using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AutoMapper;

namespace AnonymousUserService.Profiles
{
    public class AnonymousUserProfile : Profile
    {
        public AnonymousUserProfile() 
        {
            CreateMap<AnonymousUserCreationDTO, AnonymousUser>()
                .ReverseMap();
            CreateMap<AnonymousUser, AnonymousUserDTO>()
                .ReverseMap();
            CreateMap<AnonymousUser, AnonymousUserCreatedDTO>();
        }
    }
}
