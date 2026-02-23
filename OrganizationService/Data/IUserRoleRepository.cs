using AnonymousDomain.Models.Organization;
using Microsoft.AspNetCore.Mvc;

namespace OrganizationService.Data
{
    public interface IUserRoleRepository
    {
        IEnumerable<UserRole> GetAllUserRoles();
        UserRole GetUserRoleById(Guid id);
        UserRole CreateUserRole(UserRole userRole);
        UserRole UpdateUserRole(UserRole userRole);
        void DeleteUserRole(Guid id);
    }
}
