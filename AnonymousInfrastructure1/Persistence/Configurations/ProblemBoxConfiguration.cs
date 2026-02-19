using AnonymousDomain.Models.ProblemBox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class ProblemBoxConfiguration : IEntityTypeConfiguration<ProblemBox>
    {
        public void Configure(EntityTypeBuilder<ProblemBox> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.IsDarkTheme)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.Password)
                .HasMaxLength(500);

            builder.Property(p => p.OrganizationId)
                .IsRequired();

            builder.Property(p => p.BoxAccessLinkId)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired();
        }
    }
}
