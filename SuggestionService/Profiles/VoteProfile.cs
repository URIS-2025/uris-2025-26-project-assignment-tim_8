using AutoMapper;
using AnonymousDomain.Models.Suggestion;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Profiles
{
    public class VoteProfile : Profile
    {
        public VoteProfile()
        {
            CreateMap<Vote, VoteDTO>().ReverseMap();
            CreateMap<VoteCreationDTO, Vote>().ReverseMap();
        }
    }
}