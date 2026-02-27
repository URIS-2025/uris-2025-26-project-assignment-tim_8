using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Context;
using OrganizationService.Models.DTOs;

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
        public UserRoleCreatedDTO CreateUserRole(UserRoleCreationDTO userRole)
        {
            var entity = _mapper.Map<UserRole>(userRole)!;
            entity.Id = Guid.NewGuid();
            _context.UserRoles.Add(entity);
            SaveChanges();
            return _mapper.Map<UserRoleCreatedDTO>(entity);
        }

        public void DeleteUserRole(Guid id)
        {
            var userRole = _context.UserRoles.Find(id);
            if (userRole == null)
                throw new KeyNotFoundException($"UserRole with id {id} not found.");

            _context.UserRoles.Remove(userRole);
            SaveChanges();
        }

        public IEnumerable<UserRoleDTO> GetAllUserRoles()
        {

            var userRoles = _context.UserRoles.ToList();
            var userRoleResult = new List<UserRoleDTO>();

            foreach (var userRole in userRoles)
            {
                var dto = _mapper.Map<UserRoleDTO>(userRole);
                userRoleResult.Add(dto);
            }

            return userRoleResult;
        }

        public UserRoleDTO GetUserRoleById(Guid id)
        {
            var userRole = _context.UserRoles.Find(id);
            if (userRole == null)
                throw new KeyNotFoundException($"UserRole with id {id} not found.");

            return _mapper.Map<UserRoleDTO>(userRole);
        }

        public UserRoleCreatedDTO UpdateUserRole(UserRoleDTO userRole)
        {
            var existRole = _context.UserRoles.Find(userRole.Id);
            if (existRole == null)
                throw new KeyNotFoundException($"UserRole with id {userRole.Id} not found.");

            _mapper.Map(userRole, existRole);
            SaveChanges();
            return _mapper.Map<UserRoleCreatedDTO>(existRole);
        }
    }
}
