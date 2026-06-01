using AnonymousDomain.Models.AnonymousUser;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnonymousUserService.Clients;
using AnonymousUserService.Context;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AnonymousUserService.Models.DTOs.BoxAccessLink;
using Moq;
using Xunit;

namespace AnonymousUserService.Tests.Repositories
{
    public class AnonymousUserRepositoryTests
    {
        // ─── HELPERS ──────────────────────────────────────────────────────────

        private AnonymousUserContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AnonymousUserContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:AnonymousUserDB", "fake" }
                })
                .Build();

            return new AnonymousUserContext(options, config);
        }

        private IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<AnonymousUser, AnonymousUserDTO>();
                cfg.CreateMap<AnonymousUserCreationDTO, AnonymousUser>();
            });
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

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

        // Pwned client that never reports a breach (network calls are not made in unit tests).
        private static IPwnedPasswordsClient NotBreachedClient()
        {
            var mock = new Mock<IPwnedPasswordsClient>();
            mock.Setup(c => c.IsBreachedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            return mock.Object;
        }

        private AnonymousUser SeedUser(AnonymousUserContext context)
        {
            var user = new AnonymousUser
            {
                Id              = Guid.NewGuid(),
                CreatedAt       = DateTime.UtcNow,
            };
            context.AnonymousUsers.Add(user);
            context.SaveChanges();
            return user;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllAnonymousUsers_ReturnsEmpty_WhenNoneExist()
        {
            using var context = CreateInMemoryContext();
            var repo = new AnonymousUserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient());

            var result = repo.GetAllAnonymousUsers();

            Assert.Empty(result);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────
        [Fact]
        public void GetAnonymousUserById_ReturnsNull_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new AnonymousUserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient());

            var result = repo.GetAnonymousUserById(Guid.NewGuid());

            Assert.Null(result);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteAnonymousUser_DoesNotThrow_WhenNotFound()
        {
            // Za razliku od OrganizationService, ovaj repo ne baca exception — samo ignorise
            using var context = CreateInMemoryContext();
            var repo = new AnonymousUserRepository(context, CreateMapper(), CreateConfiguration(), NotBreachedClient());

            var exception = Record.Exception(() => repo.DeleteAnonymousUser(Guid.NewGuid()));

            Assert.Null(exception);
        }
    }

    public class BoxAccessLinkRepositoryTests
    {
        // ─── HELPERS ──────────────────────────────────────────────────────────

        private AnonymousUserContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AnonymousUserContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:AnonymousUserDB", "fake" }
                })
                .Build();

            return new AnonymousUserContext(options, config);
        }

        private IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<BoxAccessLink, BoxAccessLinkDTO>();
            });
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private BoxAccessLink SeedBoxAccessLink(AnonymousUserContext context,
                                                 bool isActive = true,
                                                 string token  = "test-token")
        {
            var link = new BoxAccessLink
            {
                Id          = Guid.NewGuid(),
                AccessToken = token,
                IsActive    = isActive,
                CreatedAt   = DateTime.UtcNow,
                ExpiresAt   = DateTime.UtcNow.AddDays(7)
            };
            context.BoxAccessLinks.Add(link);
            context.SaveChanges();
            return link;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllBoxAccessLinks_ReturnsAllLinks()
        {
            using var context = CreateInMemoryContext();
            SeedBoxAccessLink(context, true,  "token1");
            SeedBoxAccessLink(context, false, "token2");
            SeedBoxAccessLink(context, true,  "token3");
            var repo = new BoxAccessLinkRepository(context, CreateMapper());

            var result = repo.GetAllBoxAccessLinks();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void GetAllBoxAccessLinks_ReturnsEmpty_WhenNoneExist()
        {
            using var context = CreateInMemoryContext();
            var repo = new BoxAccessLinkRepository(context, CreateMapper());

            var result = repo.GetAllBoxAccessLinks();

            Assert.Empty(result);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetBoxAccessLinkById_ReturnsLink_WhenFound()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedBoxAccessLink(context, true, "test-token");
            var repo = new BoxAccessLinkRepository(context, CreateMapper());

            var result = repo.GetBoxAccessLinkById(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal("test-token", result.AccessToken);
            Assert.True(result.IsActive);
        }

        [Fact]
        public void GetBoxAccessLinkById_ReturnsNull_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new BoxAccessLinkRepository(context, CreateMapper());

            var result = repo.GetBoxAccessLinkById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void GetBoxAccessLinkById_ReturnsCorrectDates()
        {
            using var context = CreateInMemoryContext();
            var before = DateTime.UtcNow;
            var seeded = SeedBoxAccessLink(context);
            var repo   = new BoxAccessLinkRepository(context, CreateMapper());

            var result = repo.GetBoxAccessLinkById(seeded.Id);

            Assert.True(result.CreatedAt >= before);
            Assert.True(result.ExpiresAt > result.CreatedAt);
        }

        [Fact]
        public void GetBoxAccessLinkById_ReturnsInactiveLink_WhenIsActiveFalse()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedBoxAccessLink(context, isActive: false, token: "inactive-token");
            var repo   = new BoxAccessLinkRepository(context, CreateMapper());

            var result = repo.GetBoxAccessLinkById(seeded.Id);

            Assert.NotNull(result);
            Assert.False(result.IsActive);
        }
    }
}
