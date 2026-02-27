using AutoMapper;
using AnonymousDomain.Models.Suggestion;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Profiles
{
    public class SuggestionCategoryProfile : Profile
    {
        public SuggestionCategoryProfile()
        {
            CreateMap<SuggestionCategory, SuggestionCategoryDTO>().ReverseMap();
        }
    }
}
