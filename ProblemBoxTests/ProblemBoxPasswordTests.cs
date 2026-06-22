using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProblemBoxService.Clients;
using ProblemBoxService.Context;
using ProblemBoxService.Controllers;
using ProblemBoxService.Data;
using ProblemBoxService.Enums;
using ProblemBoxService.Models;
using ProblemBoxService.Models.DTOs;
using ProblemBoxService.Profiles;
using Xunit;

namespace ProblemBoxService.Tests
{
    // Repository-level tests use a real EF InMemory context + the real AutoMapper profile so
    // that the HasPassword mapping and BCrypt hashing are exercised end-to-end.
    public class ProblemBoxPasswordTests
    {
        private static ProblemBoxContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ProblemBoxContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            return new ProblemBoxContext(options, config);
        }

        private static IMapper NewMapper()
        {
            // Build the mapper through DI exactly as Program.cs does so this is
            // robust across AutoMapper versions (v16 MapperConfiguration needs an ILoggerFactory).
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<ProblemBoxProfile>());
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private static ProblemBox SeedBox(ProblemBoxContext ctx, string? password)
        {
            var box = new ProblemBox
            {
                Id = Guid.NewGuid(),
                Name = "Box",
                Description = "Desc",
                IsDarkTheme = false,
                CreatedAt = DateTime.UtcNow,
                OrganizationId = Guid.NewGuid(),
                Status = ProblemSuggestionStatus.Active,
                Password = password
            };
            ctx.ProblemBoxes.Add(box);
            ctx.SaveChanges();
            return box;
        }

        // ---- Create hashes the password ----
        [Fact]
        public void CreateProblemBox_StoresBcryptHash_NotPlaintext()
        {
            using var ctx = NewContext();
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            var dto = new ProblemBoxCreationDTO
            {
                Name = "Box",
                Description = "Desc",
                IsDarkTheme = false,
                Password = "secret123",
                CreatedBy = "tester",
                OrganizationId = Guid.NewGuid()
            };

            var result = repo.CreateProblemBox(dto);

            var stored = ctx.ProblemBoxes.Single(b => b.Id == result.Id);
            Assert.NotNull(stored.Password);
            Assert.NotEqual("secret123", stored.Password);
            Assert.StartsWith("$2", stored.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("secret123", stored.Password));
        }

        [Fact]
        public void CreateProblemBox_WithoutPassword_Succeeds_AndPasswordNull()
        {
            using var ctx = NewContext();
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            var dto = new ProblemBoxCreationDTO
            {
                Name = "Box",
                Description = "Desc",
                IsDarkTheme = false,
                Password = "",
                CreatedBy = "tester",
                OrganizationId = Guid.NewGuid()
            };

            var result = repo.CreateProblemBox(dto);

            var stored = ctx.ProblemBoxes.Single(b => b.Id == result.Id);
            Assert.Null(stored.Password);
        }

        // ---- SetPassword ----
        [Fact]
        public void SetPassword_StoresHash_AndHasPasswordTrue()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, null);
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            var result = repo.SetPassword(box.Id, "newpass");

            var stored = ctx.ProblemBoxes.Single(b => b.Id == box.Id);
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
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            var result = repo.SetPassword(box.Id, "");

            var stored = ctx.ProblemBoxes.Single(b => b.Id == box.Id);
            Assert.Null(stored.Password);
            Assert.False(result.HasPassword);
        }

        [Fact]
        public void SetPassword_Throws_WhenBoxMissing()
        {
            using var ctx = NewContext();
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.Throws<ArgumentException>(() => repo.SetPassword(Guid.NewGuid(), "x"));
        }

        // ---- VerifyPassword ----
        [Fact]
        public void VerifyPassword_True_ForCorrectPassword()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("correct"));
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.True(repo.VerifyPassword(box.Id, "correct"));
        }

        [Fact]
        public void VerifyPassword_False_ForWrongPassword()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("correct"));
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.False(repo.VerifyPassword(box.Id, "wrong"));
        }

        [Fact]
        public void VerifyPassword_True_ForPublicBox_NoPassword()
        {
            using var ctx = NewContext();
            var box = SeedBox(ctx, null);
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.True(repo.VerifyPassword(box.Id, "anything"));
        }

        [Fact]
        public void VerifyPassword_False_WhenBoxMissing()
        {
            using var ctx = NewContext();
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.False(repo.VerifyPassword(Guid.NewGuid(), "x"));
        }

        [Fact]
        public void VerifyPassword_False_ForLegacyNonHashValue()
        {
            using var ctx = NewContext();
            // Legacy plaintext that is not a BCrypt hash — Verify would throw, must be caught -> false.
            var box = SeedBox(ctx, "plaintextLegacy");
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.False(repo.VerifyPassword(box.Id, "plaintextLegacy"));
        }

        // ---- Read DTO HasPassword reflects state ----
        [Fact]
        public void GetProblemBoxById_HasPassword_ReflectsState()
        {
            using var ctx = NewContext();
            var withPwd = SeedBox(ctx, BCrypt.Net.BCrypt.HashPassword("x"));
            var withoutPwd = SeedBox(ctx, null);
            var repo = new ProblemBoxRepository(ctx, NewMapper());

            Assert.True(repo.GetProblemBoxById(withPwd.Id).HasPassword);
            Assert.False(repo.GetProblemBoxById(withoutPwd.Id).HasPassword);
        }

        // ---- Controller endpoints ----
        [Fact]
        public async Task UpdateProblemBoxPassword_ReturnsOk_WithUpdatedBox()
        {
            var mockRepo = new Mock<IProblemBoxRepository>();
            var mockMapper = new Mock<IMapper>();
            var logger = new Mock<LoggerServiceClient>();
            var id = Guid.NewGuid();
            var updated = new ProblemBoxDTO { Id = id, Name = "Box", HasPassword = true };
            mockRepo.Setup(r => r.SetPassword(id, "pw")).Returns(updated);
            var controller = new ProblemBoxController(mockRepo.Object, mockMapper.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = await controller.UpdateProblemBoxPassword(id, new BoxPasswordDTO { Password = "pw" });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<ProblemBoxDTO>(ok.Value);
            Assert.True(returned.HasPassword);
            mockRepo.Verify(r => r.SetPassword(id, "pw"), Times.Once);
        }

        [Fact]
        public async Task UpdateProblemBoxPassword_ReturnsBadRequest_WhenBoxMissing()
        {
            var mockRepo = new Mock<IProblemBoxRepository>();
            var mockMapper = new Mock<IMapper>();
            var logger = new Mock<LoggerServiceClient>();
            var id = Guid.NewGuid();
            mockRepo.Setup(r => r.SetPassword(id, It.IsAny<string>()))
                    .Throws(new ArgumentException("ProblemBox with that Id does not exist."));
            var controller = new ProblemBoxController(mockRepo.Object, mockMapper.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = await controller.UpdateProblemBoxPassword(id, new BoxPasswordDTO { Password = "pw" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public void VerifyProblemBoxPassword_ReturnsOk_WithValidFlag()
        {
            var mockRepo = new Mock<IProblemBoxRepository>();
            var mockMapper = new Mock<IMapper>();
            var logger = new Mock<LoggerServiceClient>();
            var id = Guid.NewGuid();
            mockRepo.Setup(r => r.VerifyPassword(id, "right")).Returns(true);
            var controller = new ProblemBoxController(mockRepo.Object, mockMapper.Object, logger.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = controller.VerifyProblemBoxPassword(id, new BoxPasswordDTO { Password = "right" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("{ valid = True }", ok.Value!.ToString());
            mockRepo.Verify(r => r.VerifyPassword(id, "right"), Times.Once);
        }
    }
}
