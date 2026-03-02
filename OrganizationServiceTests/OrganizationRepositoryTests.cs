using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrganizationService.Context;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Tests.Repositories
{
    public class OrganizationRepositoryTests
    {
        // ─── HELPERS ──────────────────────────────────────────────────────────

        private OrganizationContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<OrganizationContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // svaki test dobija svježu bazu
                .Options;
            return new OrganizationContext(options);
        }


        private IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<Organization, OrganizationDTO>();
                cfg.CreateMap<Organization, OrganizationCreatedDTO>();
                cfg.CreateMap<OrganizationDTO, Organization>();
                cfg.CreateMap<OrganizationCreationDTO, Organization>();
            });
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IMapper>();
        }

        private Organization SeedOrganization(OrganizationContext context, string name = "Test Org")
        {
            var org = new Organization
            {
                Id        = Guid.NewGuid(),
                Name      = name,
                AdminId   = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            context.Organizations.Add(org);
            context.SaveChanges();
            return org;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllOrganizations_ReturnsAllOrganizations()
        {
            using var context = CreateInMemoryContext();
            SeedOrganization(context, "Org A");
            SeedOrganization(context, "Org B");
            var repo = new OrganizationRepository(context, CreateMapper());

            var result = repo.GetAllOrganizations();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAllOrganizations_ReturnsEmpty_WhenNoneExist()
        {
            using var context = CreateInMemoryContext();
            var repo = new OrganizationRepository(context, CreateMapper());

            var result = repo.GetAllOrganizations();

            Assert.Empty(result);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetOrganizationById_ReturnsOrganization_WhenFound()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedOrganization(context, "Test Org");
            var repo = new OrganizationRepository(context, CreateMapper());

            var result = repo.GetOrganizationById(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal("Test Org", result.Name);
        }

        [Fact]
        public void GetOrganizationById_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new OrganizationRepository(context, CreateMapper());

            Assert.Throws<KeyNotFoundException>(() => repo.GetOrganizationById(Guid.NewGuid()));
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public void CreateOrganization_SavesAndReturnsCreatedDTO()
        {
            using var context = CreateInMemoryContext();
            var repo = new OrganizationRepository(context, CreateMapper());
            var dto = new OrganizationCreationDTO { Name = "Nova Org", AdminId = Guid.NewGuid() };

            var result = repo.CreateOrganization(dto);

            Assert.NotNull(result);
            Assert.Equal("Nova Org", result.Name);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(1, context.Organizations.Count());
        }

        [Fact]
        public void CreateOrganization_SetsIdAndCreatedAt_Automatically()
        {
            using var context = CreateInMemoryContext();
            var repo = new OrganizationRepository(context, CreateMapper());
            var dto = new OrganizationCreationDTO { Name = "Nova Org", AdminId = Guid.NewGuid() };

            repo.CreateOrganization(dto);

            var saved = context.Organizations.First();
            Assert.NotEqual(Guid.Empty, saved.Id);
            Assert.True(saved.CreatedAt > DateTime.MinValue);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────────

        [Fact]
        public void UpdateOrganization_UpdatesAndReturnsDTO()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedOrganization(context, "Stari Naziv");
            var repo = new OrganizationRepository(context, CreateMapper());
            var updateDto = new OrganizationDTO { Id = seeded.Id, Name = "Novi Naziv", AdminId = seeded.AdminId };

            var result = repo.UpdateOrganization(updateDto);

            Assert.Equal("Novi Naziv", result.Name);
            Assert.Equal("Novi Naziv", context.Organizations.Find(seeded.Id)!.Name);
        }

        [Fact]
        public void UpdateOrganization_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new OrganizationRepository(context, CreateMapper());
            var updateDto = new OrganizationDTO { Id = Guid.NewGuid(), Name = "Nesto", AdminId = Guid.NewGuid() };

            Assert.Throws<KeyNotFoundException>(() => repo.UpdateOrganization(updateDto));
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteOrganization_RemovesOrganization()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedOrganization(context);
            var repo = new OrganizationRepository(context, CreateMapper());

            repo.DeleteOrganization(seeded.Id);

            Assert.Equal(0, context.Organizations.Count());
        }

        [Fact]
        public void DeleteOrganization_ThrowsKeyNotFoundException_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new OrganizationRepository(context, CreateMapper());

            Assert.Throws<KeyNotFoundException>(() => repo.DeleteOrganization(Guid.NewGuid()));
        }
    }
}
