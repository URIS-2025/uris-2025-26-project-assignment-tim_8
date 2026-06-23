using SuggestionBoxService.Enums;
using SuggestionBoxService.Models.DTOs;

namespace SuggestionBoxService.Data
{
    public interface ISuggestionBoxRepository
    {
        bool SaveChanges();

        IEnumerable<SuggestionBoxDTO> GetAll();

        SuggestionBoxDTO GetById(Guid id);

        IEnumerable<SuggestionBoxDTO> GetByOrganizationId(Guid organizationId);

        SuggestionBoxDTO Create(SuggestionBoxCreateDTO dto);

        SuggestionBoxDTO Update(SuggestionBoxUpdateDTO dto);

        SuggestionBoxDTO SetStatus(Guid id, BoxStatus status);

        SuggestionBoxDTO SetPassword(Guid id, string password);

        bool VerifyPassword(Guid id, string password);

        void Delete(Guid id);

        void DeleteByOrganizationId(Guid organizationId);
    }
}