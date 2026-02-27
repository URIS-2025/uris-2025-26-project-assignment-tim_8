using SuggestionService.Models.DTOs;

namespace SuggestionService.Data
{
    public interface IVoteRepository
    {
        bool SaveChanges();
        IEnumerable<VoteDTO> GetAll();
        VoteDTO GetById(Guid id);
        IEnumerable<VoteDTO> GetBySuggestionId(Guid suggestionId);
        VoteCreationDTO Create(VoteCreationDTO vote);
        void Delete(Guid id);
    }
}
