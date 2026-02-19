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
    public class SuggestionCommentConfiguration : IEntityTypeConfiguration<SuggestionComment>
    {
        public void Configure(EntityTypeBuilder<SuggestionComment> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Text)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(s => s.IsAnonymous)
                .IsRequired();

            builder.Property(s => s.CreatedBy)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.SuggestionId)
                .IsRequired();

            builder.Property(s => s.CommentAuthorId)
                .IsRequired();

            builder.Property(s => s.SuggestionCommentId);
        }
    }
}
