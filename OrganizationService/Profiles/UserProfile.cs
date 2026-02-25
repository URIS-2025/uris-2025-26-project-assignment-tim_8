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
        }
    }
}
