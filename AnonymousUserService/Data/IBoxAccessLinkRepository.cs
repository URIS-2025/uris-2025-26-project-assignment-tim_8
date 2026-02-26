using AnonymousUserService.Models.DTOs.BoxAccessLink;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousUserService.Data
{
    public interface IBoxAccessLinkRepository
    {
        IEnumerable<BoxAccessLinkDTO> GetAllBoxAccessLinks();
        BoxAccessLinkDTO GetBoxAccessLinkById(Guid id);
    }
}
