using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Context;

namespace OrganizationService.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly OrganizationContext _context;
        private readonly IMapper _mapper;

        public UserRepository(OrganizationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }
        public User CreateUser(User user)
        {
            throw new NotImplementedException();
        }

        public void DeleteUser(Guid id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<User> GetAllUsers()
        {
            throw new NotImplementedException();
        }

        public User GetUserById(Guid id)
        {
            throw new NotImplementedException();
        }

        public User UpdateUser(User user)
        {
            throw new NotImplementedException();
        }
    }
}
