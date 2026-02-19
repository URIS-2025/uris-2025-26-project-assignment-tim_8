using AnonymousDomain.Models.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Description)
                .HasMaxLength(500);

            builder.HasData(
                new UserRole { Id = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001"), Title = "Admin", Description = "Administrator role with full access" },
                new UserRole { Id = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000002"), Title = "Manager", Description = "Manager role with elevated access" },
                new UserRole { Id = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000003"), Title = "Member", Description = "Standard member role" }
            );
        }
    }
}
