using AutoMapper;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;

namespace ProblemService.Profiles
{
    public class ProblemCommentProfile : Profile
    {
        public ProblemCommentProfile()
        {
            CreateMap<ProblemComment, ProblemCommentDTO>().ReverseMap();
            CreateMap<ProblemComment, ProblemCommentCreatedDTO>().ReverseMap();
            CreateMap<ProblemComment, ProblemCommentCreationDTO>().ReverseMap();
            CreateMap<ProblemComment, ProblemCommentUpdateDTO>().ReverseMap();
        }
    }
}
