using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrganizationService.Clients;
using OrganizationService.Context;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using Xunit;

namespace OrganizationService.Tests.Repositories
{
    public class UserRepositoryTests
    {
        // ─── HELPERS ──────────────────────────────────────────────────────────

        private OrganizationContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<OrganizationContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new OrganizationContext(options);
        }

        private IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<User, UserDTO>();
                cfg.CreateMap<User, UserCreatedDTO>();
                cfg.CreateMap<UserCreationDTO, User>();
                cfg.CreateMap<UserUpdateDTO, User>();
            });
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IMapper>();
        }

        // Mock IPwnedPasswordsClient koji uvijek vraća da lozinka NIJE u breach-u.
        private static IPwnedPasswordsClient NotBreachedClient()
        {
            var mock = new Mock<IPwnedPasswordsClient>();
            mock.Setup(c => c.IsBreachedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            return mock.Object;
        }

        // Mock ICaptchaVerifierClient koji uvijek prolazi (verifikacija uspješna).
        private static ICaptchaVerifierClient PassingCaptchaClient()
        {
            var mock = new Mock<ICaptchaVerifierClient>();
            mock.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            return mock.Object;
        }

        // Fake IConfiguration sa JWT postavkama potrebnim za GenerateJwtToken
        private IConfiguration CreateConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                { "Jwt:Key",      "SuperTajniKljucKojiMoraBitiDovoljnoDugacak123!" },
                { "Jwt:Issuer",   "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        // Kreira usera sa hashovanom lozinkom direktno u bazi
        private User SeedUser(OrganizationContext context,
                              string username = "markom",
                              string password = "tajnaSifra123",
                              string email    = "marko@test.com")
        {
            var user = new User
            {
                Id             = Guid.NewGuid(),
                Name           = "Marko",
                Surname        = "Markovic",
                Email          = email,
                Username       = username,
                Password       = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt      = DateTime.UtcNow,
                RoleId         = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };
            context.Users.Add(user);
            context.SaveChanges();
            return user;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllUsers_ReturnsAllUsers()
        {
            using var context = CreateInMemoryContext();
            SeedUser(context, "markom",  "sifra1", "marko@test.com");
            SeedUser(context, "anaa",    "sifra2", "ana@test.com");
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());

            var result = repo.GetAllUsers();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAllUsers_ReturnsEmpty_WhenNoneExist()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());

            var result = repo.GetAllUsers();

            Assert.Empty(result);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetUserById_ReturnsUser_WhenFound()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedUser(context);
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());

            var result = repo.GetUserById(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal("markom", result.Username);
            Assert.Equal("marko@test.com", result.Email);
        }

        [Fact]
        public void GetUserById_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());

            Assert.Throws<KeyNotFoundException>(() => repo.GetUserById(Guid.NewGuid()));
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public void CreateUser_SavesAndReturnsCreatedDTO()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var dto = new UserCreationDTO
            {
                Name           = "Marko",
                Surname        = "Markovic",
                Email          = "marko@test.com",
                Password       = "Zx9$mQ2!vK7w",
                Username       = "markom",
                RoleId         = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };

            var result = repo.CreateUser(dto);

            Assert.NotNull(result);
            Assert.Equal("markom", result.Username);
            Assert.Equal("marko@test.com", result.Email);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(1, context.Users.Count());
        }

        [Fact]
        public void CreateUser_HashesPassword()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var dto = new UserCreationDTO
            {
                Name     = "Marko",
                Surname  = "Markovic",
                Email    = "marko@test.com",
                Password = "Zx9$mQ2!vK7w",
                Username = "markom",
                RoleId   = Guid.NewGuid()
            };

            repo.CreateUser(dto);

            var saved = context.Users.First();
            // Lozinka ne smije biti sačuvana kao plaintext
            Assert.NotEqual("Zx9$mQ2!vK7w", saved.Password);
            // I mora biti validan BCrypt hash
            Assert.True(BCrypt.Net.BCrypt.Verify("Zx9$mQ2!vK7w", saved.Password));
        }

        [Fact]
        public void CreateUser_WithNullOrganizationId_SavesSuccessfully()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var dto = new UserCreationDTO
            {
                Name           = "Slobodan",
                Surname        = "Slobodic",
                Email          = "slobodan@test.com",
                Password       = "Zx9$mQ2!vK7w",
                Username       = "slobos",
                RoleId         = Guid.NewGuid(),
                OrganizationId = null
            };

            var result = repo.CreateUser(dto);

            Assert.NotNull(result);
            Assert.Null(context.Users.First().OrganizationId);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────────

        [Fact]
        public void UpdateUser_UpdatesAndReturnsDTO()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedUser(context);
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var updateDto = new UserUpdateDTO
            {
                Id             = seeded.Id,
                Name           = "Marko Updated",
                Surname        = "Markovic Updated",
                Username       = "markom_new",
                RoleId         = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid()
            };

            var result = repo.UpdateUser(updateDto);

            Assert.Equal("markom_new", result.Username);
            Assert.Equal("markom_new", context.Users.Find(seeded.Id)!.Username);
        }

        [Fact]
        public void UpdateUser_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var updateDto = new UserUpdateDTO { Id = Guid.NewGuid(), Username = "ghost" };

            Assert.Throws<KeyNotFoundException>(() => repo.UpdateUser(updateDto));
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteUser_RemovesUser()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedUser(context);
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());

            repo.DeleteUser(seeded.Id);

            Assert.Equal(0, context.Users.Count());
        }

        [Fact]
        public void DeleteUser_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());

            Assert.Throws<KeyNotFoundException>(() => repo.DeleteUser(Guid.NewGuid()));
        }

        // ─── LOGIN ────────────────────────────────────────────────────────────

        [Fact]
        public void Login_ReturnsJwtToken_WhenCredentialsValid()
        {
            using var context = CreateInMemoryContext();
            SeedUser(context, "markom", "tajnaSifra123");
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var loginDto = new UserLoginDTO { Username = "markom", Password = "tajnaSifra123" };

            var token = repo.Login(loginDto);

            Assert.NotNull(token);
            Assert.NotNull(token.AccessToken);
            Assert.NotEmpty(token.AccessToken);
            // JWT format: tri dijela odvojena tačkama
            Assert.Equal(3, token.AccessToken.Split('.').Length);
        }

        [Fact]
        public void Login_ThrowsUnauthorizedAccessException_WhenUserNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var loginDto = new UserLoginDTO { Username = "nepostoji", Password = "sifra" };

            Assert.Throws<UnauthorizedAccessException>(() => repo.Login(loginDto));
        }

        [Fact]
        public void Login_ThrowsUnauthorizedAccessException_WhenPasswordWrong()
        {
            using var context = CreateInMemoryContext();
            SeedUser(context, "markom", "ispravnaSifra");
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient(), PassingCaptchaClient());
            var loginDto = new UserLoginDTO { Username = "markom", Password = "pogresanaSifra" };

            Assert.Throws<UnauthorizedAccessException>(() => repo.Login(loginDto));
        }
    }
}
