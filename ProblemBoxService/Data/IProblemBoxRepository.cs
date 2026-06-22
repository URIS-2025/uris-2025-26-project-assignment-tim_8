using ProblemBoxService.Enums;
using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.Data
{
    public interface IProblemBoxRepository
    {
        bool SaveChanges();
        IEnumerable<ProblemBoxDTO> GetAll();
        IEnumerable<ProblemBoxDTO> GetProblemBoxByOrganizationId(Guid organizationId);
        ProblemBoxDTO GetProblemBoxById(Guid id);
        ProblemBoxCreatedDTO CreateProblemBox(ProblemBoxCreationDTO problemBox);
        ProblemBoxDTO UpdateProblemBox(ProblemBoxUpdateDTO problemBox);
        ProblemBoxDTO SetStatus(Guid id, ProblemSuggestionStatus status);
        ProblemBoxDTO SetPassword(Guid id, string password);
        bool VerifyPassword(Guid id, string password);
        void DeleteProblemBox(Guid id);
    }
}