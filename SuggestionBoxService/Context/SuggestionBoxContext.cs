using Microsoft.EntityFrameworkCore;
using SuggestionBoxService.Models;

namespace SuggestionBoxService.Context
{
    public class SuggestionBoxContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public SuggestionBoxContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<SuggestionBox> SuggestionBoxes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                _configuration.GetConnectionString("SuggestionBoxDB"));
            }
            }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SuggestionBox>(entity =>
            {
                entity.HasKey(sb => sb.Id);

                entity.Property(sb => sb.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(sb => sb.Description)
                      .HasMaxLength(1000);

                entity.Property(sb => sb.Password)
                      .HasMaxLength(200);

                entity.Property(sb => sb.CreatedBy)
                      .HasMaxLength(200);

                entity.Property(sb => sb.CreatedAt)
                      .IsRequired();

                entity.Property(sb => sb.IsDarkTheme)
                      .IsRequired();

                entity.Property(sb => sb.OrganizationId)
                      .IsRequired();

                entity.Property(sb => sb.BoxAccessLinkId)
                      .IsRequired();
            });
        }
    }
}