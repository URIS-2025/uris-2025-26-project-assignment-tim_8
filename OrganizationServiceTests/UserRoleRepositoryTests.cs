using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OrganizationService.Context;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using Microsoft.Extensions.DependencyInjection;


namespace OrganizationService.Tests.Repositories
{
    public class UserRoleRepositoryTests
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
                cfg.CreateMap<UserRole, UserRoleDTO>();
                cfg.CreateMap<UserRole, UserRoleCreatedDTO>();
                cfg.CreateMap<UserRoleDTO, UserRole>();
                cfg.CreateMap<UserRoleCreationDTO, UserRole>();
            });
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IMapper>();
        }

        private UserRole SeedUserRole(OrganizationContext context, string title = "Admin", string description = "Puni pristup")
        {
            var role = new UserRole
            {
                Id          = Guid.NewGuid(),
                Title       = title,
                Description = description
            };
            context.UserRoles.Add(role);
            context.SaveChanges();
            return role;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllUserRoles_ReturnsAllRoles()
        {
            using var context = CreateInMemoryContext();
            SeedUserRole(context, "Admin",   "Puni pristup");
            SeedUserRole(context, "Viewer",  "Samo citanje");
            SeedUserRole(context, "Manager", "Upravljanje");
            var repo = new UserRoleRepository(context, CreateMapper());

            var result = repo.GetAllUserRoles();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void GetAllUserRoles_ReturnsEmpty_WhenNoneExist()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRoleRepository(context, CreateMapper());

            var result = repo.GetAllUserRoles();

            Assert.Empty(result);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetUserRoleById_ReturnsRole_WhenFound()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedUserRole(context, "Admin", "Puni pristup");
            var repo = new UserRoleRepository(context, CreateMapper());

            var result = repo.GetUserRoleById(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal("Admin", result.Title);
            Assert.Equal("Puni pristup", result.Description);
        }

        [Fact]
        public void GetUserRoleById_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRoleRepository(context, CreateMapper());

            Assert.Throws<KeyNotFoundException>(() => repo.GetUserRoleById(Guid.NewGuid()));
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public void CreateUserRole_SavesAndReturnsCreatedDTO()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRoleRepository(context, CreateMapper());
            var dto = new UserRoleCreationDTO { Title = "Manager", Description = "Upravljanje timom" };

            var result = repo.CreateUserRole(dto);

            Assert.NotNull(result);
            Assert.Equal("Manager", result.Title);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(1, context.UserRoles.Count());
        }

       
        [Fact]
        public void CreateUserRole_SetsIdAutomatically()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRoleRepository(context, CreateMapper());
            var dto = new UserRoleCreationDTO { Title = "Manager", Description = "Opis" };

            repo.CreateUserRole(dto);

            var saved = context.UserRoles.First();
            Assert.NotEqual(Guid.Empty, saved.Id);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────────

        [Fact]
        public void UpdateUserRole_UpdatesAndReturnsDTO()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedUserRole(context, "Admin", "Stari opis");
            var repo = new UserRoleRepository(context, CreateMapper());
            var updateDto = new UserRoleDTO { Id = seeded.Id, Title = "Super Admin", Description = "Novi opis" };

            var result = repo.UpdateUserRole(updateDto);

            Assert.Equal("Super Admin", result.Title);
            Assert.Equal("Super Admin", context.UserRoles.Find(seeded.Id)!.Title);
        }

        [Fact]
        public void UpdateUserRole_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRoleRepository(context, CreateMapper());
            var updateDto = new UserRoleDTO { Id = Guid.NewGuid(), Title = "Nesto", Description = "Opis" };

            Assert.Throws<KeyNotFoundException>(() => repo.UpdateUserRole(updateDto));
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteUserRole_RemovesRole()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedUserRole(context);
            var repo = new UserRoleRepository(context, CreateMapper());

            repo.DeleteUserRole(seeded.Id);

            Assert.Equal(0, context.UserRoles.Count());
        }

        [Fact]
        public void DeleteUserRole_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRoleRepository(context, CreateMapper());

            Assert.Throws<KeyNotFoundException>(() => repo.DeleteUserRole(Guid.NewGuid()));
        }
    }
}
