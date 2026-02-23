using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Context;

namespace OrganizationService.Data
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly OrganizationContext _context;
        private readonly IMapper _mapper;

        public UserRoleRepository(OrganizationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }
        public UserRole CreateUserRole(UserRole userRole)
        {
            throw new NotImplementedException();
        }

        public void DeleteUserRole(Guid id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<UserRole> GetAllUserRoles()
        {
            throw new NotImplementedException();
        }

        public UserRole GetUserRoleById(Guid id)
        {
            throw new NotImplementedException();
        }

        public UserRole UpdateUserRole(UserRole userRole)
        {
            throw new NotImplementedException();
        }
    }
}
