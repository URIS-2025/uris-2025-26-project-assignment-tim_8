using AnonymousUserService.Context;
using AnonymousUserService.Models.DTOs.BoxAccessLink;
using AutoMapper;

namespace AnonymousUserService.Data
{
    public class BoxAccessLinkRepository : IBoxAccessLinkRepository
    {
        private readonly AnonymousUserContext _context;
        private readonly IMapper _mapper;

        public BoxAccessLinkRepository(AnonymousUserContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<BoxAccessLinkDTO> GetAllBoxAccessLinks()
        {
            var boxAccessLinks = _context.BoxAccessLinks.ToList();
            var boxAccessLinkResult = new List<BoxAccessLinkDTO>();

            foreach (var boxAccessLink in boxAccessLinks)
            {
                var dto = _mapper.Map<BoxAccessLinkDTO>(boxAccessLink);
                boxAccessLinkResult.Add(dto);
            }

            return boxAccessLinkResult;
        }

        public BoxAccessLinkDTO GetBoxAccessLinkById(Guid id)
        {
            var boxAccessLink = _context.BoxAccessLinks.Find(id);
            if (boxAccessLink == null)
            {
                return null;
            }

            return _mapper.Map<BoxAccessLinkDTO>(boxAccessLink);
        }
    }
}
