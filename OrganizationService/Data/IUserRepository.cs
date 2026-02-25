using AnonymousDomain.Models.Organization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Data
{
    public interface IUserRepository
    {
        IEnumerable<UserDTO> GetAllUsers();
        UserDTO GetUserById(Guid id);
        UserCreatedDTO CreateUser(UserCreationDTO user);
        UserCreatedDTO UpdateUser(UserDTO user);
        void DeleteUser(Guid id);
        string Login (UserLoginDTO login);
    }
}
