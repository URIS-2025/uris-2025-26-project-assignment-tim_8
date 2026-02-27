using AutoMapper;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;

namespace ProblemService.Profiles
{
    public class ProblemCategoryProfile : Profile
    {
        public ProblemCategoryProfile()
        {
            CreateMap<ProblemCategory, ProblemCategoryDTO>().ReverseMap();
            CreateMap<ProblemCategory, ProblemCategoryCreatedDTO>().ReverseMap();
            CreateMap<ProblemCategory, ProblemCategoryCreationDTO>().ReverseMap();
            CreateMap<ProblemCategory, ProblemCategoryUpdateDTO>().ReverseMap();
        }
    }
}
