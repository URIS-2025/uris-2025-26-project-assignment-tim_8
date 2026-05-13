using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Models.DTOs.AnonymousUser;

namespace AnonymousUserService.Data
{
    public interface IAnonymousUserRepository
    {
        IEnumerable<AnonymousUserDTO> GetAllAnonymousUsers();
        AnonymousUserDTO GetAnonymousUserById(Guid id);
        AnonymousUserDTO CreateUser(AnonymousUserCreationDTO user);
        AnonymousLoginResponseDTO Login(AnonymousUserLoginDTO login);
        AnonymousLoginResponseDTO RefreshToken(string refreshToken);
        void DeleteAnonymousUser(Guid id);
    }
}
