using AutoMapper;
using ProblemBoxService.Models;
using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.Profiles
{
    public class ProblemBoxProfile : Profile
    {
        public ProblemBoxProfile()
        {
            CreateMap<ProblemBox, ProblemBoxDTO>().ReverseMap();
            CreateMap<ProblemBox, ProblemBoxCreatedDTO>().ReverseMap();
            CreateMap<ProblemBox, ProblemBoxCreationDTO>().ReverseMap();
            CreateMap<ProblemBox, ProblemBoxUpdateDTO>().ReverseMap();
        }
    }
}