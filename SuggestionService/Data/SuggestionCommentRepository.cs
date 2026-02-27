using AnonymousDomain.Models.Suggestion;
using AutoMapper;
using AnonymousRepository.Interfaces;
using SuggestionService.Models.DTOs;
using AnonymousDomain.Models.Suggestion.DTOs;

namespace AnonymousRepository.Repositories
{
    public class SuggestionCommentRepository : ISuggestionCommentRepository
    {
        private readonly SuggestionContext _context;
        private readonly IMapper _mapper;

        public SuggestionCommentRepository(SuggestionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<SuggestionCommentDTO> GetAll()
        {
            var comments = _context.SuggestionComments.ToList();
            return _mapper.Map<IEnumerable<SuggestionCommentDTO>>(comments);
        }

        public SuggestionCommentDTO GetById(Guid id)
        {
            var comment = _context.SuggestionComments.FirstOrDefault(c => c.Id == id);
            return _mapper.Map<SuggestionCommentDTO>(comment);
        }

        public IEnumerable<SuggestionCommentDTO> GetBySuggestionId(Guid suggestionId)
        {
            var comments = _context.SuggestionComments
                                   .Where(c => c.SuggestionId == suggestionId)
                                   .ToList();
            return _mapper.Map<IEnumerable<SuggestionCommentDTO>>(comments);
        }

        public SuggestionCommentCreationDTO Create(SuggestionCommentCreationDTO commentDto)
        {
            var comment = _mapper.Map<SuggestionComment>(commentDto);
            _context.SuggestionComments.Add(comment);
            SaveChanges();
            return _mapper.Map<SuggestionCommentCreationDTO>(comment);
        }

        public SuggestionCommentUpdateDTO Update(SuggestionCommentUpdateDTO commentDto)
        {
            var comment = _context.SuggestionComments.FirstOrDefault(c => c.Id == commentDto.Id);
            _mapper.Map(commentDto, comment);
            SaveChanges();
            return _mapper.Map<SuggestionCommentUpdateDTO>(comment);
        }

        public void Delete(Guid id)
        {
            var comment = _context.SuggestionComments.FirstOrDefault(c => c.Id == id);
            if (comment != null)
            {
                _context.SuggestionComments.Remove(comment);
                SaveChanges();
            }
        }
    }
}