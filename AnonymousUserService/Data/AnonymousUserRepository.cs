using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Clients;
using AnonymousUserService.Context;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AnonymousUserService.Validation;
using AutoMapper;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AnonymousUserService.Data
{
    public class AnonymousUserRepository : IAnonymousUserRepository
    {
        private readonly AnonymousUserContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IPwnedPasswordsClient _pwned;
        private readonly ICaptchaVerifierClient _captcha;

        private static readonly Regex UsernameRegex = new(
            @"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

        public AnonymousUserRepository(AnonymousUserContext context, IMapper mapper, IConfiguration configuration, IPwnedPasswordsClient pwned, ICaptchaVerifierClient captcha)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _pwned = pwned;
            _captcha = captcha;
        }

        public bool SaveChanges() => _context.SaveChanges() > 0;

        public void DeleteAnonymousUser(Guid id)
        {
            var user = _context.AnonymousUsers.Find(id);
            if (user == null)
                throw new KeyNotFoundException($"Anonymous user with id {id} not found.");

            _context.Remove(user);
            SaveChanges();
        }

        public IEnumerable<AnonymousUserDTO> GetAllAnonymousUsers()
        {
            return _context.AnonymousUsers.ToList().Select(u => _mapper.Map<AnonymousUserDTO>(u));
        }

        public AnonymousUserDTO GetAnonymousUserById(Guid id)
        {
            var user = _context.AnonymousUsers.Find(id);
            if (user == null)
                return null;
            return _mapper.Map<AnonymousUserDTO>(user);
        }

        public AnonymousUserDTO CreateUser(AnonymousUserCreationDTO user)
        {
            if (!_captcha.VerifyAsync(user.CaptchaToken, CancellationToken.None).GetAwaiter().GetResult())
                throw new ArgumentException("Captcha verification failed. Please try again.");

            if (string.IsNullOrWhiteSpace(user.Username))
                throw new ArgumentException("Username may only contain letters, digits, and underscores (3–30 chars).");

            var normalizedUsername = user.Username.Trim().ToLower();

            if (!UsernameRegex.IsMatch(normalizedUsername) || normalizedUsername.Length < 3 || normalizedUsername.Length > 30)
                throw new ArgumentException("Username may only contain letters, digits, and underscores (3–30 chars).");

            PasswordPolicy.Validate(user.Password);

            if (_pwned.IsBreachedAsync(user.Password, CancellationToken.None).GetAwaiter().GetResult())
                throw new ArgumentException("This password has appeared in a data breach. Please choose another.");

            if (_context.AnonymousUsers.Any(u => u.Username == normalizedUsername))
                throw new InvalidOperationException("Username is already taken.");

            var entity = _mapper.Map<AnonymousUser>(user)!;
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.Username = normalizedUsername;
            entity.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.AnonymousUsers.Add(entity);
            SaveChanges();
            return _mapper.Map<AnonymousUserDTO>(entity);
        }

        public AnonymousLoginResponseDTO Login(AnonymousUserLoginDTO login)
        {
            if (!_captcha.VerifyAsync(login.CaptchaToken, CancellationToken.None).GetAwaiter().GetResult())
                throw new ArgumentException("Captcha verification failed. Please try again.");

            var normalizedUsername = login.Username.Trim().ToLower();
            var user = _context.AnonymousUsers.FirstOrDefault(u => u.Username == normalizedUsername);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid username or password.");

            return GenerateTokenPair(user);
        }

        public AnonymousLoginResponseDTO RefreshToken(string refreshToken)
        {
            var stored = _context.AnonymousRefreshTokens.FirstOrDefault(
                t => t.Token == refreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

            if (stored == null)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var user = _context.AnonymousUsers.Find(stored.UserId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            stored.IsRevoked = true;
            SaveChanges();

            return GenerateTokenPair(user);
        }

        private AnonymousLoginResponseDTO GenerateTokenPair(AnonymousUser user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = IssueRefreshToken(user.Id);
            return new AnonymousLoginResponseDTO { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        private string GenerateAccessToken(AnonymousUser user)
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
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string IssueRefreshToken(Guid userId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes);

            _context.AnonymousRefreshTokens.Add(new AnonymousRefreshToken
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
