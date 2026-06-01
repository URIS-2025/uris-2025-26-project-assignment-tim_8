using System;
using System.Threading;
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
    public class UserRepositoryValidationTests
    {
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
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Jwt:Key",      "SuperTajniKljucKojiMoraBitiDovoljnoDugacak123!" },
                    { "Jwt:Issuer",   "TestIssuer" },
                    { "Jwt:Audience", "TestAudience" }
                })
                .Build();
        }

        private IPwnedPasswordsClient PwnedClient(bool breached)
        {
            var mock = new Mock<IPwnedPasswordsClient>();
            mock.Setup(c => c.IsBreachedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(breached);
            return mock.Object;
        }

        private UserCreationDTO ValidDto() => new UserCreationDTO
        {
            Name           = "Marko",
            Surname        = "Markovic",
            Email          = "marko@test.com",
            Password       = "Zx9$mQ2!vK7w",
            Username       = "markom",
            RoleId         = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };

        [Fact]
        public void CreateUser_InvalidEmail_ThrowsCanonicalMessage()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), PwnedClient(false));
            var dto = ValidDto();
            dto.Email = "not-an-email";

            var ex = Assert.Throws<ArgumentException>(() => repo.CreateUser(dto));
            Assert.Equal("Please enter a valid email address.", ex.Message);
            Assert.Equal(0, context.Users.Count());
        }

        [Fact]
        public void CreateUser_WeakPassword_ThrowsPolicyMessage()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), PwnedClient(false));
            var dto = ValidDto();
            dto.Password = "alllowercase1!"; // missing uppercase

            var ex = Assert.Throws<ArgumentException>(() => repo.CreateUser(dto));
            Assert.Equal("Password must include uppercase, lowercase, a number, and a special character.", ex.Message);
            Assert.Equal(0, context.Users.Count());
        }

        [Fact]
        public void CreateUser_BreachedPassword_ThrowsBreachMessage()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), PwnedClient(true));
            var dto = ValidDto();

            var ex = Assert.Throws<ArgumentException>(() => repo.CreateUser(dto));
            Assert.Equal("This password has appeared in a data breach. Please choose another.", ex.Message);
            Assert.Equal(0, context.Users.Count());
        }

        [Fact]
        public void CreateUser_NotBreachedValidInput_Succeeds()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context, CreateMapper(), CreateConfiguration(), PwnedClient(false));

            var result = repo.CreateUser(ValidDto());

            Assert.NotNull(result);
            Assert.Equal("markom", result.Username);
            Assert.Equal("marko@test.com", result.Email);
            Assert.Equal(1, context.Users.Count());
        }
    }
}
