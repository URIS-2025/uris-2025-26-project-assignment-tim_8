using AnonymousDomain.Models.Organization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Data
{
    public interface IUserRoleRepository
    {
        IEnumerable<UserRoleDTO> GetAllUserRoles();
        UserRoleDTO GetUserRoleById(Guid id);
        UserRoleCreatedDTO CreateUserRole(UserRoleCreationDTO userRole);
        UserRoleCreatedDTO UpdateUserRole(UserRoleDTO userRole);
        void DeleteUserRole(Guid id);
    }
}
