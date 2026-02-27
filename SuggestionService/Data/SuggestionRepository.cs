using AnonymousDomain.Models.Suggestion;
using AutoMapper;
using AnonymousRepository.Interfaces;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Repositories
{
    public class SuggestionRepository : ISuggestionRepository
    {
        private readonly SuggestionContext _context;
        private readonly IMapper _mapper;

        public SuggestionRepository(SuggestionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }
        
        public IEnumerable<SuggestionDTO> GetAll()
        {
            var suggestions = _context.Suggestions.ToList();
            return _mapper.Map<IEnumerable<SuggestionDTO>>(suggestions);
        }

        public SuggestionDTO GetById(Guid id)
        {
            var suggestion = _context.Suggestions.FirstOrDefault(s => s.Id == id);
            return _mapper.Map<SuggestionDTO>(suggestion);
        }

        public IEnumerable<SuggestionDTO> GetByUserId(Guid userId)
        {
            var suggestions = _context.Suggestions
                                      .Where(s => s.AnonymousUserId == userId)
                                      .ToList();
            return _mapper.Map<IEnumerable<SuggestionDTO>>(suggestions);
        }

        public SuggestionCreatedDTO Create(SuggestionCreationDTO suggestionDto)
        {
            var suggestion = _mapper.Map<Suggestion>(suggestionDto);
            _context.Suggestions.Add(suggestion);
            SaveChanges();
            return _mapper.Map<SuggestionCreatedDTO>(suggestion);
        }

        public SuggestionCreatedDTO Update(SuggestionUpdateDTO suggestionDto)
        {
            var suggestion = _context.Suggestions.FirstOrDefault(s => s.Id == suggestionDto.Id);
            _mapper.Map(suggestionDto, suggestion);
            SaveChanges();
            return _mapper.Map<SuggestionCreatedDTO>(suggestion);
        }

        public void Delete(Guid id)
        {
            var suggestion = _context.Suggestions.FirstOrDefault(s => s.Id == id);
            if (suggestion != null)
            {
                _context.Suggestions.Remove(suggestion);
                SaveChanges();
            }
        }
    }
}