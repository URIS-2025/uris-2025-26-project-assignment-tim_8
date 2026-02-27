using AnonymousDomain.Models.Suggestion;
using AutoMapper;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Repositories
{
    public class VoteRepository : IVoteRepository
    {
        private readonly SuggestionContext _context;
        private readonly IMapper _mapper;

        public VoteRepository(SuggestionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<VoteDTO> GetAll()
        {
            var votes = _context.Votes.ToList();
            return _mapper.Map<IEnumerable<VoteDTO>>(votes);
        }

        public VoteDTO GetById(Guid id)
        {
            var vote = _context.Votes.FirstOrDefault(v => v.Id == id);
            return _mapper.Map<VoteDTO>(vote);
        }

        public IEnumerable<VoteDTO> GetBySuggestionId(Guid suggestionId)
        {
            var votes = _context.Votes
                                .Where(v => v.SuggestionId == suggestionId)
                                .ToList();
            return _mapper.Map<IEnumerable<VoteDTO>>(votes);
        }

        public VoteCreationDTO Create(VoteCreationDTO voteDto)
        {
            var vote = _mapper.Map<Vote>(voteDto);
            _context.Votes.Add(vote);
            SaveChanges();
            return _mapper.Map<VoteCreationDTO>(vote);
        }

        public void Delete(Guid id)
        {
            var vote = _context.Votes.FirstOrDefault(v => v.Id == id);
            if (vote != null)
            {
                _context.Votes.Remove(vote);
                SaveChanges();
            }
        }
    }
}