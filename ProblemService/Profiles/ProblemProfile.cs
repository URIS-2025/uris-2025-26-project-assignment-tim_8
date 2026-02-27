using AutoMapper;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;

namespace ProblemService.Profiles
{
    public class ProblemProfile : Profile
    {
        public ProblemProfile()
        {
            CreateMap<Problem, ProblemDTO>().ReverseMap();
            CreateMap<Problem, ProblemCreatedDTO>().ReverseMap();
            CreateMap<Problem, ProblemCreationDTO>().ReverseMap();
            CreateMap<Problem, ProblemUpdateDTO>().ReverseMap();
        }
    }
}
