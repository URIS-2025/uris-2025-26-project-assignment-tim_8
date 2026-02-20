using AnonymousDomain.Models.Problem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class ProblemCommentConfiguration : IEntityTypeConfiguration<ProblemComment>
    {
        public void Configure(EntityTypeBuilder<ProblemComment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.CommentText)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(p => p.IsAnonymous)
                .IsRequired();

            builder.Property(p => p.ProblemId)
                .IsRequired();

            builder.Property(p => p.ProblemCommentAuthorId)
                .IsRequired();

            builder.Property(p => p.ProblemCommentId);
        }
    }
}
