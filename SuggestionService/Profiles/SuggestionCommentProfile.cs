using AutoMapper;
using AnonymousDomain.Models.Suggestion;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Profiles
{
    public class SuggestionCommentProfile : Profile
    {
        public SuggestionCommentProfile()
        {
            CreateMap<SuggestionComment, SuggestionCommentDTO>().ReverseMap();
            CreateMap<SuggestionCommentCreationDTO, SuggestionComment>().ReverseMap();
        }
    }
}