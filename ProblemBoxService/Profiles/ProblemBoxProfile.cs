using AutoMapper;
using ProblemBoxService.Models;
using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.Profiles
{
    public class ProblemBoxProfile : Profile
    {
        public ProblemBoxProfile()
        {
            CreateMap<ProblemBox, ProblemBoxDTO>()
                .ForMember(d => d.HasPassword, o => o.MapFrom(s => !string.IsNullOrWhiteSpace(s.Password)));
            CreateMap<ProblemBoxDTO, ProblemBox>();
            CreateMap<ProblemBox, ProblemBoxCreatedDTO>().ReverseMap();
            CreateMap<ProblemBox, ProblemBoxCreationDTO>().ReverseMap();
            CreateMap<ProblemBox, ProblemBoxUpdateDTO>().ReverseMap();
        }
    }
}