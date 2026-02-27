using ProblemService.Models.DTOs;

namespace ProblemService.ServiceCalls
{
    public interface IAttachmentService
    {
        IEnumerable<AttachmentVO> GetAttachmentsByProblemId(Guid problemId);
    }
}
