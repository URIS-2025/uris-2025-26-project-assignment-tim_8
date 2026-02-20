using AnonymousDomain.Models.SuggestionBox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class SuggestionBoxConfiguration : IEntityTypeConfiguration<SuggestionBox>
    {
        public void Configure(EntityTypeBuilder<SuggestionBox> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .HasMaxLength(1000);

            builder.Property(s => s.IsDarkTheme)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.Password)
                .HasMaxLength(500);

            builder.Property(s => s.CreatedBy)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.OrganizationId)
                .IsRequired();

            builder.Property(s => s.BoxAccessLinkId)
                .IsRequired();
        }
    }
}
