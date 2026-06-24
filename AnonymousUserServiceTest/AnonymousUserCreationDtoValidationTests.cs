using System;
using System.Collections.Generic;
using AnonymousDomain.Models.AnonymousUser;
using AnonymousUserService.Clients;
using AnonymousUserService.Context;
using AnonymousUserService.Data;
using AnonymousUserService.Models.DTOs.AnonymousUser;
using AnonymousUserService.Validation;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AnonymousUserService.Tests.Validation
{
    // Repo-layer validation tests. The repo (not DataAnnotations) is the enforcement point:
    // PasswordPolicy + username regex + HIBP gate all run inside AnonymousUserRepository.CreateUser.
    public class AnonymousUserValidationTests
    {
        // ─── PasswordPolicy (canonical messages) ──────────────────────────────

        [Fact]
        public void Password_TooShort_ThrowsWithCanonicalMessage()
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate("Ab1!"));
            Assert.Equal("Password must be at least 8 characters.", ex.Message);
        }

        [Fact]
        public void Password_TooLong_ThrowsWithCanonicalMessage()
        {
            // 65 chars, otherwise valid composition.
            var pwd = "Aa1!" + new string('a', 61);
            Assert.Equal(65, pwd.Length);
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate(pwd));
            Assert.Equal("Password must be at most 64 characters.", ex.Message);
        }

        [Theory]
        [InlineData("aaaaaaaa")]   // no upper, digit, special
        [InlineData("Aaaaaaaa")]   // no digit, special
        [InlineData("Aaaaaaa1")]   // no special
        [InlineData("aaaaaaa1!")]  // no uppercase
        public void Password_MissingCharacterClass_ThrowsComplexityMessage(string password)
        {
            var ex = Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate(password));
            Assert.Equal(
                "Password must include uppercase, lowercase, a number, and a special character.",
                ex.Message);
        }

        [Fact]
        public void Password_MeetingPolicy_DoesNotThrow()
        {
            var ex = Record.Exception(() => PasswordPolicy.Validate("Abcdef1!"));
            Assert.Null(ex);
        }

        // ─── Username regex / length (via CreateUser) ─────────────────────────

        [Theory]
        [InlineData("ab")]            // too short (2)
        [InlineData("bad name!")]     // bad chars (space + !)
        [InlineData("nope$")]         // bad char
        public void CreateUser_InvalidUsername_ThrowsUsernameMessage(string username)
        {
            var repo = CreateRepo(out _);
            var dto = new AnonymousUserCreationDTO { Username = username, Password = "Abcdef1!" };

            var ex = Assert.Throws<ArgumentException>(() => repo.CreateUser(dto));
            Assert.Equal(
                "Username may only contain letters, digits, and underscores (3–30 chars).",
                ex.Message);
        }

        [Fact]
        public void CreateUser_UsernameTooLong_ThrowsUsernameMessage()
        {
            var repo = CreateRepo(out _);
            var dto = new AnonymousUserCreationDTO { Username = new string('a', 31), Password = "Abcdef1!" };

            var ex = Assert.Throws<ArgumentException>(() => repo.CreateUser(dto));
            Assert.Equal(
                "Username may only contain letters, digits, and underscores (3–30 chars).",
                ex.Message);
        }

        [Fact]
        public void CreateUser_ValidUsernameWithUnderscoresAndDigits_Succeeds()
        {
            var repo = CreateRepo(out _);
            var dto = new AnonymousUserCreationDTO { Username = "valid_user_123", Password = "Abcdef1!" };

            var result = repo.CreateUser(dto);

            Assert.NotNull(result);
        }

        // ─── HIBP gate ────────────────────────────────────────────────────────

        [Fact]
        public void CreateUser_BreachedPassword_ThrowsBreachMessage()
        {
            var repo = CreateRepo(out _, breached: true);
            var dto = new AnonymousUserCreationDTO { Username = "validuser", Password = "Abcdef1!" };

            var ex = Assert.Throws<ArgumentException>(() => repo.CreateUser(dto));
            Assert.Equal(
                "This password has appeared in a data breach. Please choose another.",
                ex.Message);
        }

        [Fact]
        public void CreateUser_NotBreachedValidInput_Succeeds()
        {
            var repo = CreateRepo(out var context, breached: false);
            var dto = new AnonymousUserCreationDTO { Username = "validuser", Password = "Abcdef1!" };

            var result = repo.CreateUser(dto);

            Assert.NotNull(result);
            Assert.Single(context.AnonymousUsers);
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────

        private static AnonymousUserRepository CreateRepo(out AnonymousUserContext context, bool breached = false)
        {
            context = CreateInMemoryContext();
            var pwned = new Mock<IPwnedPasswordsClient>();
            pwned.Setup(c => c.IsBreachedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(breached);
            var captcha = new Mock<ICaptchaVerifierClient>();
            captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);
            return new AnonymousUserRepository(context, CreateMapper(), CreateConfiguration(), pwned.Object, captcha.Object);
        }

        private static AnonymousUserContext CreateInMemoryContext()
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

        private static IMapper CreateMapper()
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

        private static IConfiguration CreateConfiguration()
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
    }
}
