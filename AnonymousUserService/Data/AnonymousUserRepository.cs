using AnonymousUserService.Context;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AutoMapper;

namespace AnonymousUserService.Data
{
    public class AnonymousUserRepository : IAnonymousUserRepository
    {
        private readonly AnonymousUserContext _context;
        private readonly IMapper _mapper;

        public AnonymousUserRepository(AnonymousUserContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public void DeleteAnonymousUser(Guid id)
        {
            var user = _context.AnonymousUsers.Find(id);
            if(user != null)
            {
                _context.Remove(user);
                _context.SaveChanges();
            }
        }

        public IEnumerable<AnonymousUserDTO> GetAllAnonymousUsers()
        {
            var users = _context.AnonymousUsers.ToList();
            var userResult = new List<AnonymousUserDTO>();  

            foreach(var user in users)
            {
                var dto = _mapper.Map<AnonymousUserDTO>(user);
                userResult.Add(dto);
            }

            return userResult;
        }

        public AnonymousUserDTO GetAnonymousUserById(Guid id)
        {
            var user = _context.AnonymousUsers.Find(id);
            if(user == null)
            {
                return null;
            }

            return _mapper.Map<AnonymousUserDTO>(user);
        }
    }
}
