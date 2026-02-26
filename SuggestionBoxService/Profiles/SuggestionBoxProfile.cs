using AutoMapper;
using SuggestionBoxService.Models;
using SuggestionBoxService.Models.DTOs;

namespace SuggestionBoxService.Profiles
{
    public class SuggestionBoxProfile : Profile
    {
        public SuggestionBoxProfile()
        {
            CreateMap<SuggestionBoxCreateDTO, SuggestionBox>()
                .ReverseMap();

            CreateMap<SuggestionBoxUpdateDTO, SuggestionBox>()
                .ReverseMap();

            CreateMap<SuggestionBox, SuggestionBoxDTO>()
                .ReverseMap();
        }
    }
}