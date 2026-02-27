using AnonymousDomain.Models.Suggestion.DTOs;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Interfaces
{
    public interface ISuggestionCommentRepository
    {
        bool SaveChanges();
        IEnumerable<SuggestionCommentDTO> GetAll();
        SuggestionCommentDTO GetById(Guid id);
        IEnumerable<SuggestionCommentDTO> GetBySuggestionId(Guid suggestionId);
        SuggestionCommentCreationDTO Create(SuggestionCommentCreationDTO comment);
        SuggestionCommentUpdateDTO Update(SuggestionCommentUpdateDTO comment);
        void Delete(Guid id);
    }
}