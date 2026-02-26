using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AnonymousUserService.Models.DTOs.BoxAccessLink;
using AutoMapper;

namespace AnonymousUserService.Profiles
{
    public class BoxAccessLinkProfile : Profile

    {
        public BoxAccessLinkProfile()
        {
            CreateMap<BoxAccessLink, BoxAccessLinkDTO>()
                .ReverseMap();
        }
    }
}
