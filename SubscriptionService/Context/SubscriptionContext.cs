using SubscriptionService.Models;
using SubscriptionService.Enums;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionService.Context
{
    public class SubscriptionContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public SubscriptionContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer(
                    _configuration.GetConnectionString("SubscriptionDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =============================
            // SubscriptionPlan
            // =============================
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(sp => sp.Id);

                entity.Property(sp => sp.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(sp => sp.Description)
                      .HasMaxLength(500);
            });

            // =============================
            // Subscription
            // =============================
            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.StartDate)
                      .IsRequired();

                entity.Property(s => s.EndDate)
                      .IsRequired();

                entity.HasOne<SubscriptionPlan>()
                      .WithMany()
                      .HasForeignKey(s => s.SubscriptionPlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================
            // Payment
            // =============================
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Total)
                      .IsRequired();

                entity.Property(p => p.CreatedAt)
                      .IsRequired();

                entity.HasOne<Subscription>()
                      .WithMany()
                      .HasForeignKey(p => p.SubscriptionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Enum kao string
                entity.Property(p => p.Status)
                      .HasConversion<string>()
                      .IsRequired();

                entity.Property(p => p.Currency)
                      .HasConversion<string>()
                      .IsRequired();

                entity.Property(p => p.PaymentMethod)
                      .HasConversion<string>()
                      .IsRequired();
            });
        }
    }
}