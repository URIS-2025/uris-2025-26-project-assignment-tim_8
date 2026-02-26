using AnonymousDomain.Models.SystemNotification;
using Microsoft.EntityFrameworkCore;

namespace SystemNotificationService.Context
{
    public class SystemNotificationContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public SystemNotificationContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<SystemNotification> SystemNotifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _configuration.GetConnectionString("SystemNotificationDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SystemNotification>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Text)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(s => s.IsRead)
                      .IsRequired();

                entity.Property(s => s.CreatedAt)
                      .IsRequired();

                entity.Property(s => s.OrganizationId)
                      .IsRequired();

                entity.Property(s => s.AnonymousUserId)
                      .IsRequired();

                entity.Property(s => s.ProblemCommentId)
                      .IsRequired();

                entity.Property(s => s.SuggestionCommentId)
                      .IsRequired();
            });
        }
    }
}