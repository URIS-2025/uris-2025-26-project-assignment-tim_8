using AnonymousDomain.Models.BillingNotification;
using BillingNotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingNotificationService.Context
{
    public class BillingNotificationContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public BillingNotificationContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<BillingNotification> BillingNotifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer(
                _configuration.GetConnectionString("BillingNotificationDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<BillingNotification>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.Property(b => b.Text)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(b => b.IsRead)
                      .IsRequired();

                entity.Property(b => b.OrganizationId)
                      .IsRequired();

                entity.Property(b => b.PaymentId)
                      .IsRequired();
            });
        }
    }
}
