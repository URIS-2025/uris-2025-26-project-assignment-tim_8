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

            // Password is a BCrypt hash managed solely by SetPassword (PUT /{id}/password).
            // Ignore it on the general update so a routine name/description edit can't overwrite
            // the stored hash with the plaintext value carried on the update DTO.
            CreateMap<SuggestionBoxUpdateDTO, SuggestionBox>()
                .ForMember(d => d.Password, o => o.Ignore())
                .ReverseMap();

            CreateMap<SuggestionBox, SuggestionBoxDTO>()
                .ForMember(d => d.HasPassword, o => o.MapFrom(s => !string.IsNullOrWhiteSpace(s.Password)));

            CreateMap<SuggestionBoxDTO, SuggestionBox>();
        }
    }
}