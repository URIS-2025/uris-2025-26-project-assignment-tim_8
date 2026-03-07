using Microsoft.EntityFrameworkCore;
using ProblemBoxService.Models;

namespace ProblemBoxService.Context
    {
        public class ProblemBoxContext : DbContext
        {
            private readonly IConfiguration _configuration;

            public ProblemBoxContext(
                DbContextOptions options,
                IConfiguration configuration) : base(options)
            {
                _configuration = configuration;
            }

            public DbSet<ProblemBox> ProblemBoxes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    _configuration.GetConnectionString("ProblemBoxDB"));
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
            {
                base.OnModelCreating(builder);

                builder.Entity<ProblemBox>(entity =>
                {
                    entity.HasKey(pb => pb.Id);
                    entity.Property(pb => pb.Name)
                          .IsRequired()
                          .HasMaxLength(200);
                    entity.Property(pb => pb.Description)
                          .IsRequired()
                          .HasMaxLength(1000);
                    entity.Property(pb => pb.IsDarkTheme)
                          .IsRequired();
                    entity.Property(pb => pb.CreatedAt)
                          .IsRequired();
                    entity.Property(pb => pb.Password)
                          .IsRequired()
                          .HasMaxLength(200);
                    entity.Property(pb => pb.Status)
                          .IsRequired();
                    
                });
            }
        }
 }

