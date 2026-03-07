using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Context;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AutoMapper;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AnonymousUserService.Data
{
    public class AnonymousUserRepository : IAnonymousUserRepository
    {
        private readonly AnonymousUserContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AnonymousUserRepository(AnonymousUserContext context, IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
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

        public AnonymousUserDTO CreateUser(AnonymousUserCreationDTO user)
        {
            var entity = _mapper.Map<AnonymousUser>(user)!;
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.AnonymousUsers.Add(entity);
            SaveChanges();
            return _mapper.Map<AnonymousUserDTO>(entity);
        }

        public string Login(AnonymousUserCreationDTO login)
        {
            var user = _context.AnonymousUsers.FirstOrDefault(u => u.Username == login.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid username or password.");

            return GenerateJwtToken(user);
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

        private string GenerateJwtToken(AnonymousUser user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
