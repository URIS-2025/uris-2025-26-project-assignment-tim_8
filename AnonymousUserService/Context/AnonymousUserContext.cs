using AnonymousDomain.Models.AnonymousUser;
using Microsoft.EntityFrameworkCore;

namespace AnonymousUserService.Context
{
    public class AnonymousUserContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AnonymousUserContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<AnonymousUser> AnonymousUsers { get; set; }
        public DbSet<BoxAccessLink> BoxAccessLinks { get; set; }
        public DbSet<AnonymousRefreshToken> AnonymousRefreshTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer(
                    _configuration.GetConnectionString("AnonymousUserDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AnonymousUser>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.CreatedAt).IsRequired();
                entity.Property(a => a.Username).IsRequired().HasMaxLength(30);
                entity.Property(a => a.Password).IsRequired();
                entity.HasIndex(a => a.Username).IsUnique();
            });

            builder.Entity<AnonymousRefreshToken>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Token).IsRequired().HasMaxLength(512);
                entity.Property(r => r.ExpiresAt).IsRequired();
                entity.Property(r => r.CreatedAt).IsRequired();
                entity.HasIndex(r => r.Token);
                entity.HasOne<AnonymousUser>()
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
