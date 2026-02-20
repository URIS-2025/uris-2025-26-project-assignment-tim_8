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
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            builder.Property(o => o.AdminId)
                .IsRequired();
        }
    }
}
