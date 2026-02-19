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
    public class SuggestionCategoryConfiguration : IEntityTypeConfiguration<SuggestionCategory>
    {
        public void Configure(EntityTypeBuilder<SuggestionCategory> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.HasData(
                new SuggestionCategory { Id = Guid.Parse("d4e5f6a7-0004-0004-0004-000000000001"), Title = "Product", Description = "Product improvement suggestions" },
                new SuggestionCategory { Id = Guid.Parse("d4e5f6a7-0004-0004-0004-000000000002"), Title = "Process", Description = "Process improvement suggestions" },
                new SuggestionCategory { Id = Guid.Parse("d4e5f6a7-0004-0004-0004-000000000003"), Title = "Workplace", Description = "Workplace environment suggestions" }
            );
        }
    }
}
