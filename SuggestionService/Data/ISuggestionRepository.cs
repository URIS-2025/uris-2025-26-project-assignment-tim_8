
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Interfaces
{
    public interface ISuggestionRepository
    {
        bool SaveChanges();
        IEnumerable<SuggestionDTO> GetAll();
        SuggestionDTO GetById(Guid id);
        IEnumerable<SuggestionDTO> GetByUserId(Guid userId);
        SuggestionCreatedDTO Create(SuggestionCreationDTO suggestion);
        SuggestionCreatedDTO Update(SuggestionUpdateDTO suggestion);
        void Delete(Guid id);
    }
}