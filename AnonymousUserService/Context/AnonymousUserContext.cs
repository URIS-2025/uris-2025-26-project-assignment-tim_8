using AnonymousDomain.Models.AnonymousUser;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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
                entity.Property(a => a.CreatedAt)
                      .IsRequired();
                entity.Property(a => a.BoxAccessLinkId)
                      .IsRequired();
            });
        }
    }
}