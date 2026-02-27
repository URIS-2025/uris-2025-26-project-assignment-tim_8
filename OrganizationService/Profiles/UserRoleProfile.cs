using AutoMapper;
using AnonymousDomain.Models.Organization;
using OrganizationService.Models.DTOs;


namespace OrganizationService.Profiles
{
    public class UserRoleProfile : Profile
    {
        public UserRoleProfile()
        {
            CreateMap<UserRoleCreationDTO, UserRole>()
               .ReverseMap();
            CreateMap<UserRole, UserRoleDTO>()
                .ReverseMap();
            CreateMap<UserRole, UserRoleCreatedDTO>()
                .ReverseMap();

        }
    }
}
