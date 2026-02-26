using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.Data
{
    public interface IProblemBoxRepository
    {
        bool SaveChanges();
        IEnumerable<ProblemBoxDTO> GetProblemBoxByOrganizationId(Guid organizationId);
        ProblemBoxDTO GetProblemBoxById(Guid id);
        ProblemBoxDTO GetProblemBoxByAccessLinkId(Guid boxAccessLinkId);
        ProblemBoxCreatedDTO CreateProblemBox(ProblemBoxCreationDTO problemBox);
        ProblemBoxDTO UpdateProblemBox(ProblemBoxUpdateDTO problemBox);
        void DeleteProblemBox(Guid id);
    }
}