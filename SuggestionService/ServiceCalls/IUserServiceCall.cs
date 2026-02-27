using SuggestionService.Models.DTOs;

namespace SuggestionService.ServiceCalls
{
    public interface IUserServiceCall
    {
        Task<UserDTO> GetUserById(Guid userId);
    }
}