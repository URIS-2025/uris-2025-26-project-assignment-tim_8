using AnonymousDomain.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace OrganizationService.Context
{
    public class OrganizationContext : DbContext
    {
        

        public OrganizationContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Organization>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(o => o.CreatedAt)
                      .IsRequired();

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(o => o.AdminId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.Surname)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(u => u.Username)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.Password)
                      .IsRequired();

                entity.Property(u => u.CreatedAt)
                      .IsRequired();

                entity.HasOne<UserRole>()
                      .WithMany()
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Organization>()
                      .WithMany()
                      .HasForeignKey(u => u.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<UserRole>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Title)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(r => r.Description)
                      .HasMaxLength(500);
            });
        }
    }
}
