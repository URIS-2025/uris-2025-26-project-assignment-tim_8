using ProblemService.Models.Problem;
using ProblemService.Models.DTOs;

namespace ProblemService.Data
{
        public interface IProblemRepository
        {
            IEnumerable<ProblemDTO> GetAllProblems();
            ProblemDTO GetProblemById(Guid id);
            IEnumerable<ProblemDTO> GetProblemsByProblemBoxId(Guid problemBoxId);
            ProblemCreatedDTO CreateProblem(ProblemCreationDTO problem);
            ProblemDTO UpdateProblem(ProblemUpdateDTO problem);
            void DeleteProblem(Guid id);
        }

    
}
