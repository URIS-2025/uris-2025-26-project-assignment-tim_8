using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using Microsoft.AspNetCore.Mvc;

namespace AnonymousUserService.Data
{
    public interface IAnonymousUserRepository
    {
        IEnumerable<AnonymousUserDTO> GetAllAnonymousUsers();
        AnonymousUserDTO GetAnonymousUserById(Guid id);
        AnonymousUserDTO CreateUser(AnonymousUserCreationDTO user);
        string Login(AnonymousUserCreationDTO login);
        void DeleteAnonymousUser(Guid id);
    }
}
