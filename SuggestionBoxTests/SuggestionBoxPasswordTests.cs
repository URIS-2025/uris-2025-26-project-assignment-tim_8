using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SuggestionBoxService.Clients;
using SuggestionBoxService.Context;
using SuggestionBoxService.Data;
using SuggestionBoxService.Enums;
using SuggestionBoxService.Models;
using SuggestionBoxService.Models.DTOs;
using SuggestionBoxService.Profiles;
using AnonymousAPI.Controllers;

namespace SuggestionBoxServiceTests
{
    // Repository-level tests use a real EF InMemory context + the real AutoMapper profile so
    // that the HasPassword mapping and BCrypt hashing are exercised end-to-end.
    public class SuggestionBoxPasswordTests
    {
        private static SuggestionBoxContext NewContext()
        {
            var options = new DbContextOptionsBuilder<SuggestionBoxContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            return new SuggestionBoxContext(options, config);
        }

        private static IMapper NewMapper()
        {
            var cfg = new MapperConfiguration(c => c.AddProfile<SuggestionBoxProfile>());
            return cfg.CreateMapper();
        }

        private static SuggestionBox SeedBox(SuggestionBoxContext ctx, string? password)
        {
            var box = new SuggestionBox
            {
                Id = Guid.NewGuid(),
                Name = "Box",
                Description = "Desc",
                IsDarkTheme = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "tester",
                OrganizationId = Guid.NewGuid(),
                Status = BoxStatus.Active,
                Password = password
            };
            ctx.SuggestionBoxes.Add(box);
            ctx.SaveChanges();
            return box;
        }

        // ---- Create hashes the password ----
        [Fact]
        public void Create_StoresBcryptHash_NotPlaintext()
        {
            using var ctx = NewContext();
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            var dto = new SuggestionBoxCreateDTO
            {
                Name = "Box",
                Description = "Desc",
                IsDarkTheme = false,
                Password = "secret123",
                CreatedBy = "tester",
                OrganizationId = Guid.NewGuid()
            };

            var result = repo.Create(dto);

            var stored = ctx.SuggestionBoxes.Single(b => b.Id == result.Id);
            Assert.NotNull(stored.Password);
            Assert.NotEqual("secret123", stored.Password);
            Assert.StartsWith("$2", stored.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("secret123", stored.Password));
            Assert.True(result.HasPassword);
        }

        [Fact]
        public void Create_WithoutPassword_LeavesPasswordNull_AndHasPasswordFalse()
        {
            using var ctx = NewContext();
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            var dto = new SuggestionBoxCreateDTO
            {
                Name = "Box",
                Description = "Desc",
                IsDarkTheme = false,
                Password = "",
                CreatedBy = "tester",
                OrganizationId = Guid.NewGuid()
            };

            var result = repo.Create(dto);

            var stored = ctx.SuggestionBoxes.Single(b => b.Id == result.Id);
            Assert.Null(stored.Password);
            Assert.False(result.HasPassword);
        }

        // ---- General Update must NOT corrupt the stored password hash ----
        [Fact]
        public void Update_DoesNotOverwritePasswordHash_WithPlaintext()
        {
            using var ctx = NewContext();
            var originalHash = BCrypt.Net.BCrypt.HashPassword("orig");
            var box = SeedBox(ctx, originalHash);
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            var result = repo.Update(new SuggestionBoxUpdateDTO
            {
                Id = box.Id,
                Name = "Renamed",
                Description = "New description",
                IsDarkTheme = true,
                Password = "plaintext-should-be-ignored"
            });

            var stored = ctx.SuggestionBoxes.Single(b => b.Id == box.Id);
            // Password is untouched by a general update: still the original BCrypt hash,
            // never the plaintext carried on the update DTO.
            Assert.Equal(originalHash, stored.Password);
            Assert.StartsWith("$2", stored.Password);
            Assert.NotEqual("plaintext-should-be-ignored", stored.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("orig", stored.Password));
            // The rest of the update still applies.
            Assert.Equal("Renamed", stored.Name);
            Assert.Equal("New description", stored.Description);
            Assert.True(result.HasPassword);
        }

        [Fact]
        public void Update_WithBlankPassword_PreservesExistingHash()
        {
            using var ctx = NewContext();
            var originalHash = BCrypt.Net.BCrypt.HashPassword("orig");
            var box = SeedBox(ctx, originalHash);
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            repo.Update(new SuggestionBoxUpdateDTO
            {
                Id = box.Id,
                Name = "Renamed",
                Description = "New description",
                IsDarkTheme = false,
                Password = ""
            });

            var stored = ctx.SuggestionBoxes.Single(b => b.Id == box.Id);
            // A blank password on a routine update must NOT wipe the stored hash.
            Assert.Equal(originalHash, stored.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("orig", stored.Password));
        }

        // ---- SetPassword ----
        [Fact]
        public void SetPassword_StoresHash_AndHasPasswordTrue()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, null);
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            var result = repo.SetPassword(box.Id, "newpass");

            var stored = ctx.SuggestionBoxes.Single(b => b.Id == box.Id);
            Assert.NotEqual("newpass", stored.Password);
            Assert.StartsWith("$2", stored.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", stored.Password));
            Assert.True(result.HasPassword);
        }

        [Fact]
        public void SetPassword_Empty_ClearsPassword_AndHasPasswordFalse()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("old"));
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            var result = repo.SetPassword(box.Id, "");

            var stored = ctx.SuggestionBoxes.Single(b => b.Id == box.Id);
            Assert.Null(stored.Password);
            Assert.False(result.HasPassword);
        }

        [Fact]
        public void SetPassword_ReturnsNull_WhenBoxMissing()
        {
            using var ctx = NewContext();
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            var result = repo.SetPassword(Guid.NewGuid(), "x");

            Assert.Null(result);
        }

        // ---- VerifyPassword ----
        [Fact]
        public void VerifyPassword_True_ForCorrectPassword()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("correct"));
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            Assert.True(repo.VerifyPassword(box.Id, "correct"));
        }

        [Fact]
        public void VerifyPassword_False_ForWrongPassword()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("correct"));
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            Assert.False(repo.VerifyPassword(box.Id, "wrong"));
        }

        [Fact]
        public void VerifyPassword_True_ForPublicBox_NoPassword()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, null);
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            Assert.True(repo.VerifyPassword(box.Id, "anything"));
        }

        [Fact]
        public void VerifyPassword_False_WhenBoxMissing()
        {
            using var ctx = NewContext();
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            Assert.False(repo.VerifyPassword(Guid.NewGuid(), "x"));
        }

        [Fact]
        public void VerifyPassword_False_ForLegacyNonHashValue()
        {
            using var ctx = NewContext();
            // Legacy plaintext that is not a BCrypt hash — Verify would throw, must be caught -> false.
            var box = SeedBox(ctx, "plaintextLegacy");
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            Assert.False(repo.VerifyPassword(box.Id, "plaintextLegacy"));
        }

        // ---- Read DTO HasPassword reflects state ----
        [Fact]
        public void GetById_HasPassword_ReflectsState()
        {
            using var ctx = NewContext();
            var withPwd = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("x"));
            var withoutPwd = SeedBox(ctx, null);
            var repo = new SuggestionBoxRepository(ctx, NewMapper());

            Assert.True(repo.GetById(withPwd.Id).HasPassword);
            Assert.False(repo.GetById(withoutPwd.Id).HasPassword);
        }

        // ---- Controller endpoints ----
        [Fact]
        public async Task UpdateSuggestionBoxPassword_ReturnsOk_WithUpdatedBox()
        {
            var mockRepo = new Mock<ISuggestionBoxRepository>();
            var logger = new Mock<LoggerServiceClient>();
            var id = Guid.NewGuid();
            var updated = new SuggestionBoxDTO { Id = id, Name = "Box", HasPassword = true };
            mockRepo.Setup(r => r.SetPassword(id, "pw")).Returns(updated);
            var controller = new SuggestionBoxController(mockRepo.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = await controller.UpdateSuggestionBoxPassword(id, new BoxPasswordDTO { Password = "pw" });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SuggestionBoxDTO>(ok.Value);
            Assert.True(returned.HasPassword);
            mockRepo.Verify(r => r.SetPassword(id, "pw"), Times.Once);
        }

        [Fact]
        public async Task UpdateSuggestionBoxPassword_ReturnsNotFound_WhenBoxMissing()
        {
            var mockRepo = new Mock<ISuggestionBoxRepository>();
            var logger = new Mock<LoggerServiceClient>();
            var id = Guid.NewGuid();
            mockRepo.Setup(r => r.SetPassword(id, It.IsAny<string>())).Returns((SuggestionBoxDTO)null);
            var controller = new SuggestionBoxController(mockRepo.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = await controller.UpdateSuggestionBoxPassword(id, new BoxPasswordDTO { Password = "pw" });

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public void VerifySuggestionBoxPassword_ReturnsOk_WithValidFlag()
        {
            var mockRepo = new Mock<ISuggestionBoxRepository>();
            var logger = new Mock<LoggerServiceClient>();
            var id = Guid.NewGuid();
            mockRepo.Setup(r => r.VerifyPassword(id, "right")).Returns(true);
            var controller = new SuggestionBoxController(mockRepo.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = controller.VerifySuggestionBoxPassword(id, new BoxPasswordDTO { Password = "right" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("{ valid = True }", ok.Value!.ToString());
            mockRepo.Verify(r => r.VerifyPassword(id, "right"), Times.Once);
        }
    }
}
