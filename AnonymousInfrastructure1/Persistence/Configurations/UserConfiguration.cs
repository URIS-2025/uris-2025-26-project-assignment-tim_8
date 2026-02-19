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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Surname)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.Password)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(u => u.Username)
                .IsUnique();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.RoleId)
                .IsRequired();

            builder.Property(u => u.OrganizationId);
        }
    }
}
