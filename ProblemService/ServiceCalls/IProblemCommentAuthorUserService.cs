using ProblemService.Models.DTOs;

namespace ProblemService.ServiceCalls
{
    public interface IProblemCommentAuthorUserService
    {
        ProblemCommentAuthorUserVO GetUserById(Guid id);
    }
}
