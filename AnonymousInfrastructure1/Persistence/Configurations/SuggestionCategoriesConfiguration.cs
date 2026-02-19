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
    public class SuggestionCategoriesConfiguration : IEntityTypeConfiguration<SuggestionCategories>
    {
        public void Configure(EntityTypeBuilder<SuggestionCategories> builder)
        {
            builder.HasKey(sc => new { sc.SuggestionId, sc.SuggestionCategoryId });

            builder.Property(sc => sc.SuggestionId)
                .IsRequired();

            builder.Property(sc => sc.SuggestionCategoryId)
                .IsRequired();
        }
    }
}
