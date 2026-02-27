using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.ServiceCalls
{
    public interface IProblemService
    {
        IEnumerable<ProblemVO> GetProblemsByProblemBoxId(Guid problemBoxId);
    }
}
