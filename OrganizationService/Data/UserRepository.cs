using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Context;
using OrganizationService.Models.DTOs;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrganizationService.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly OrganizationContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UserRepository(OrganizationContext context, IMapper mapper, IConfiguration configuration )
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }
        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }
        public UserCreatedDTO CreateUser(UserCreationDTO user)
        {
            var entity = _mapper.Map<User>(user)!;
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(entity);
            SaveChanges();
            return _mapper.Map<UserCreatedDTO>(entity);
        }

        public void DeleteUser(Guid id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                throw new KeyNotFoundException($"User with id {id} not found.");

            _context.Users.Remove(user);
            SaveChanges();
        }

        public IEnumerable<UserDTO> GetAllUsers()
        {
            var users = _context.Users.ToList();
            var result = new List<UserDTO>();
            foreach (var user in users)
            {
                result.Add(_mapper.Map<UserDTO>(user));
            }
            return result;
        }

        public UserDTO GetUserById(Guid id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                throw new KeyNotFoundException($"User with id {id} not found.");
            return _mapper.Map<UserDTO>(user);
        }

        public UserCreatedDTO UpdateUser(UserUpdateDTO user)
        {
            var existUser = _context.Users.Find(user.Id);
            if (existUser == null)
                throw new KeyNotFoundException($"User with id {user.Id} not found.");

            _mapper.Map(user, existUser);
            SaveChanges();
            return _mapper.Map<UserCreatedDTO>(existUser);
        }

        public string Login(UserLoginDTO login)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == login.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid username or password.");

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("RoleId", user.RoleId.ToString())
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
