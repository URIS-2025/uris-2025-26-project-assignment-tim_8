using SuggestionService.Models.DTOs;

namespace SuggestionService.Data
{
    public interface ISuggestionCategoryRepository
    {
        bool SaveChanges();
        IEnumerable<SuggestionCategoryDTO> GetAll();
        SuggestionCategoryDTO GetById(Guid id);
        SuggestionCategoryDTO Create(SuggestionCategoryDTO category);
        SuggestionCategoryDTO Update(SuggestionCategoryDTO category);
        void Delete(Guid id);
    }
}
