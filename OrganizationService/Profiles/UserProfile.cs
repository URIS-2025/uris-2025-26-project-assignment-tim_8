using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Models.DTOs;
namespace OrganizationService.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<UserCreationDTO , User>()
                .ReverseMap();
            CreateMap<User, UserDTO>() 
                .ReverseMap();
            CreateMap<User, UserCreatedDTO>()
                .ReverseMap();
            CreateMap<UserUpdateDTO, User>()
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}
