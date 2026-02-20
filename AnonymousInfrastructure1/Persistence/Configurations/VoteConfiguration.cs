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
    public class VoteConfiguration : IEntityTypeConfiguration<Vote>
    {
        public void Configure(EntityTypeBuilder<Vote> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.CreatedAt)
                .IsRequired();

            builder.Property(v => v.VoteAuthorId)
                .IsRequired();

            builder.Property(v => v.SuggestionId)
                .IsRequired();
        }
    }
}
