using AnonymousDomain.Models.BillingNotification;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BillingNotificationService.Context;
using BillingNotificationService.Data;
using BillingNotificationService.Models.DTOs.BillingNotificationDTO;
using Xunit;

namespace BillingNotificationService.Tests.Repositories
{
    public class BillingNotificationRepositoryTests
    {
        // ─── HELPERS ──────────────────────────────────────────────────────────

        private BillingNotificationContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<BillingNotificationContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:BillingNotificationDB", "fake" }
                })
                .Build();

            return new BillingNotificationContext(options, config);
        }

        private IMapper CreateMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<BillingNotificationCreationDTO, BillingNotification>();
                cfg.CreateMap<BillingNotification, BillingNotificationDTO>();
                cfg.CreateMap<BillingNotification, BillingNotificationCreatedDTO>();
                cfg.CreateMap<BillingNotificationUpdateDTO, BillingNotification>();
            });
            return services.BuildServiceProvider().GetRequiredService<IMapper>();
        }

        private BillingNotification SeedNotification(BillingNotificationContext context,
                                                      string text    = "Test notifikacija",
                                                      bool   isRead  = false)
        {
            var notification = new BillingNotification
            {
                Id             = Guid.NewGuid(),
                Text           = text,
                IsRead         = isRead,
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };
            context.BillingNotifications.Add(notification);
            context.SaveChanges();
            return notification;
        }

        // ─── GET ALL ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAllBillingNotifications_ReturnsAll()
        {
            using var context = CreateInMemoryContext();
            SeedNotification(context, "Notif 1", false);
            SeedNotification(context, "Notif 2", true);
            SeedNotification(context, "Notif 3", false);
            var repo = new BillingNotificationRepository(context, CreateMapper());

            var result = repo.GetAllBillingNotifications();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void GetAllBillingNotifications_ReturnsEmpty_WhenNoneExist()
        {
            using var context = CreateInMemoryContext();
            var repo = new BillingNotificationRepository(context, CreateMapper());

            var result = repo.GetAllBillingNotifications();

            Assert.Empty(result);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────

        [Fact]
        public void GetBillingNotificationById_ReturnsNotification_WhenFound()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedNotification(context, "Test notifikacija", false);
            var repo = new BillingNotificationRepository(context, CreateMapper());

            var result = repo.GetBillingNotificationById(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal("Test notifikacija", result.Text);
            Assert.False(result.IsRead);
        }

        [Fact]
        public void GetBillingNotificationById_ReturnsNull_WhenNotFound()
        {
            using var context = CreateInMemoryContext();
            var repo = new BillingNotificationRepository(context, CreateMapper());

            var result = repo.GetBillingNotificationById(Guid.NewGuid());

            Assert.Null(result);
        }

        // ─── CREATE ───────────────────────────────────────────────────────────

        [Fact]
        public void CreateBillingNotification_SavesAndReturnsCreatedDTO()
        {
            using var context = CreateInMemoryContext();
            var repo = new BillingNotificationRepository(context, CreateMapper());
            var dto = new BillingNotificationCreationDTO
            {
                Text           = "Nova notifikacija",
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };

            var result = repo.CreateBillingNotification(dto);

            Assert.NotNull(result);
            Assert.Equal("Nova notifikacija", result.Text);
            Assert.Equal(1, context.BillingNotifications.Count());
        }

        [Fact]
        public void CreateBillingNotification_SetsOrganizationIdAndPaymentId()
        {
            using var context = CreateInMemoryContext();
            var repo = new BillingNotificationRepository(context, CreateMapper());
            var orgId     = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var dto = new BillingNotificationCreationDTO
            {
                Text           = "Test",
                OrganizationId = orgId,
                PaymentId      = paymentId
            };

            var result = repo.CreateBillingNotification(dto);

            Assert.Equal(orgId,     result.OrganizationId);
            Assert.Equal(paymentId, result.PaymentId);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────────

        [Fact]
        public void UpdateBillingNotification_UpdatesAndReturnsDTO()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedNotification(context, "Stari tekst");
            var repo = new BillingNotificationRepository(context, CreateMapper());
            var updateDto = new BillingNotificationUpdateDTO
            {
                Id             = seeded.Id,
                Text           = "Novi tekst",
                OrganizationId = seeded.OrganizationId,
                PaymentId      = seeded.PaymentId
            };

            var result = repo.UpdateBillingNotification(updateDto);

            Assert.Equal("Novi tekst", result.Text);
            Assert.Equal("Novi tekst", context.BillingNotifications.Find(seeded.Id)!.Text);
        }

        [Fact]
        public void UpdateBillingNotification_ReturnsNull_WhenNotFound()
        {
            // Repo ne baca exception nego vraca null kad nije pronadjen
            using var context = CreateInMemoryContext();
            var repo = new BillingNotificationRepository(context, CreateMapper());
            var updateDto = new BillingNotificationUpdateDTO
            {
                Id             = Guid.NewGuid(),
                Text           = "Nesto",
                OrganizationId = Guid.NewGuid(),
                PaymentId      = Guid.NewGuid()
            };

            var result = repo.UpdateBillingNotification(updateDto);

            Assert.Null(result);
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        [Fact]
        public void DeleteBillingNotification_RemovesNotification_WhenFound()
        {
            using var context = CreateInMemoryContext();
            var seeded = SeedNotification(context);
            var repo = new BillingNotificationRepository(context, CreateMapper());

            repo.DeleteBillingNotification(seeded.Id);

            Assert.Equal(0, context.BillingNotifications.Count());
        }

        [Fact]
        public void DeleteBillingNotification_DoesNotThrow_WhenNotFound()
        {
            // Repo samo ignorise ako nije pronadjen
            using var context = CreateInMemoryContext();
            var repo = new BillingNotificationRepository(context, CreateMapper());

            var exception = Record.Exception(() => repo.DeleteBillingNotification(Guid.NewGuid()));

            Assert.Null(exception);
        }
    }
}
