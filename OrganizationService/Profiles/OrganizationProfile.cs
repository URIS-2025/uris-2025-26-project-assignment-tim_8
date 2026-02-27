using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Profiles
{
    public class OrganizationProfile : Profile
    {
        public OrganizationProfile()
        {
            CreateMap<OrganizationCreationDTO, Organization>()
               .ReverseMap();
            CreateMap<Organization, OrganizationCreatedDTO>()
                .ReverseMap();
            CreateMap<OrganizationDTO, Organization>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}
