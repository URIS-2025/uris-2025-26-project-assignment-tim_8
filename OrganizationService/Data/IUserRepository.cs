using AnonymousDomain.Models.Organization;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Data
{
    public interface IUserRepository
    {
        IEnumerable<UserDTO> GetAllUsers();
        UserDTO GetUserById(Guid id);
        UserCreatedDTO CreateUser(UserCreationDTO user);
        UserCreatedDTO UpdateUser(UserUpdateDTO user);
        void DeleteUser(Guid id);
        LoginResponseDTO Login(UserLoginDTO login);
        LoginResponseDTO RefreshToken(string refreshToken);
    }
}
