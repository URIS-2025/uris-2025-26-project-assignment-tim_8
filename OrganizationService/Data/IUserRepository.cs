using AnonymousDomain.Models.Organization;
using Microsoft.AspNetCore.Mvc;

namespace OrganizationService.Data
{
    public interface IUserRepository
    {
        IEnumerable<User> GetAllUsers();
        User GetUserById(Guid id);
        User CreateUser(User user);
        User UpdateUser(User user);
        void DeleteUser(Guid id);
    }
}
