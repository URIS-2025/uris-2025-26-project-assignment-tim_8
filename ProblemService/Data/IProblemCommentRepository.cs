using ProblemService.Models.DTOs;

namespace ProblemService.Data
{
    public interface IProblemCommentRepository
    {
        IEnumerable<ProblemCommentDTO> GetAllProblemComments();
        ProblemCommentDTO GetProblemCommentById(Guid id);
        ProblemCommentCreatedDTO CreateProblemComment(ProblemCommentCreationDTO comment);
        ProblemCommentDTO UpdateProblemComment(ProblemCommentUpdateDTO comment);
        void DeleteProblemComment(Guid id);
    }
}
