using AutoMapper;
using AnonymousDomain.Models.Suggestion;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Profiles
{
    public class SuggestionProfile : Profile
    {
        public SuggestionProfile()
        {
            CreateMap<Suggestion, SuggestionDTO>().ReverseMap();
            CreateMap<SuggestionCreationDTO, Suggestion>().ReverseMap();
            CreateMap<Suggestion, SuggestionCreatedDTO>().ReverseMap();
            CreateMap<SuggestionUpdateDTO, Suggestion>().ReverseMap();
        }
    }
}