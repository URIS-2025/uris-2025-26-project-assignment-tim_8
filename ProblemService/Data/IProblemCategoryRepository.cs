using ProblemService.Models.DTOs;

namespace ProblemService.Data
{
    public interface IProblemCategoryRepository
    {
        IEnumerable<ProblemCategoryDTO> GetAllProblemCategories();
        ProblemCategoryDTO GetProblemCategoryById(Guid id);
        ProblemCategoryCreatedDTO CreateProblemCategory(ProblemCategoryCreationDTO category);
        ProblemCategoryDTO UpdateProblemCategory(ProblemCategoryUpdateDTO category);
        void DeleteProblemCategory(Guid id);
    }
}
