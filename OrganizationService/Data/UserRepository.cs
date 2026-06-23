using AnonymousDomain.Models.Organization;
using AutoMapper;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using OrganizationService.Clients;
using OrganizationService.Context;
using OrganizationService.Models.DTOs;
using OrganizationService.Validation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OrganizationService.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly OrganizationContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IPwnedPasswordsClient _pwned;
        private readonly ICaptchaVerifierClient _captcha;

        private static readonly Regex EmailRegex = new(
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

        public UserRepository(OrganizationContext context, IMapper mapper, IConfiguration configuration, IPwnedPasswordsClient pwned, ICaptchaVerifierClient captcha)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _pwned = pwned;
            _captcha = captcha;
        }

        public bool SaveChanges() => _context.SaveChanges() > 0;

        public UserCreatedDTO CreateUser(UserCreationDTO user)
        {
            if (!_captcha.VerifyAsync(user.CaptchaToken, CancellationToken.None).GetAwaiter().GetResult())
                throw new ArgumentException("Captcha verification failed. Please try again.");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new ArgumentException("Please enter a valid email address.");

            var normalizedEmail = user.Email.Trim().ToLower();
            var normalizedUsername = user.Username.Trim().ToLower();

            if (!EmailRegex.IsMatch(normalizedEmail))
                throw new ArgumentException("Please enter a valid email address.");

            PasswordPolicy.Validate(user.Password);

            if (_pwned.IsBreachedAsync(user.Password, CancellationToken.None).GetAwaiter().GetResult())
                throw new ArgumentException("This password has appeared in a data breach. Please choose another.");

            if (_context.Users.Any(u => u.Email == normalizedEmail))
                throw new InvalidOperationException("An account with this email already exists.");

            if (_context.Users.Any(u => u.Username == normalizedUsername))
                throw new InvalidOperationException("Username is already taken.");

            var entity = _mapper.Map<User>(user)!;
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Email = normalizedEmail;
            entity.Username = normalizedUsername;
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
            return _context.Users.ToList().Select(u => _mapper.Map<UserDTO>(u));
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

        public LoginResponseDTO Login(UserLoginDTO login)
        {
            if (!_captcha.VerifyAsync(login.CaptchaToken, CancellationToken.None).GetAwaiter().GetResult())
                throw new ArgumentException("Captcha verification failed. Please try again.");

            var normalizedUsername = login.Username.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u => u.Username == normalizedUsername);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid username or password.");

            return GenerateTokenPair(user);
        }

        public LoginResponseDTO RefreshToken(string refreshToken)
        {
            var stored = _context.RefreshTokens.FirstOrDefault(
                t => t.Token == refreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

            if (stored == null)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var user = _context.Users.Find(stored.UserId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            stored.IsRevoked = true;
            SaveChanges();

            return GenerateTokenPair(user);
        }

        private LoginResponseDTO GenerateTokenPair(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = IssueRefreshToken(user.Id);
            return new LoginResponseDTO { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        private string GenerateAccessToken(User user)
        {
            var role = _context.UserRoles.Find(user.RoleId);

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role?.Title ?? "user"),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim("OrganizationId", user.OrganizationId?.ToString() ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string IssueRefreshToken(Guid userId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes);

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });
            SaveChanges();
            return token;
        }
    }
}
