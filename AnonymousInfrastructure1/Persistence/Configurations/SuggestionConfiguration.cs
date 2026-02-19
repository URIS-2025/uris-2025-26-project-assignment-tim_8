using AnonymousDomain.Models.Suggestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
    {
        public void Configure(EntityTypeBuilder<Suggestion> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.SuggestionBoxId)
                .IsRequired();

            builder.Property(s => s.AnonymousUserId)
                .IsRequired();

            builder.Property(s => s.Status)
                .IsRequired();
        }
    }
}
