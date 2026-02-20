using AnonymousDomain.Models.Attachment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.FileType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.Url)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(a => a.UploadedAt)
                .IsRequired();

            builder.Property(a => a.ProblemId);

            builder.Property(a => a.SuggestionId);
        }
    }
}
