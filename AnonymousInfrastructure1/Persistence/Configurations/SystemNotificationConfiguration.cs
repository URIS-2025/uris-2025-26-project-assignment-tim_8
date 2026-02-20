using AnonymousDomain.Models.SystemNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class SystemNotificationConfiguration : IEntityTypeConfiguration<SystemNotification>
    {
        public void Configure(EntityTypeBuilder<SystemNotification> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Text)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(s => s.IsRead)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.OrganizationId);

            builder.Property(s => s.AnonymousUserId);

            builder.Property(s => s.ProblemCommentId);

            builder.Property(s => s.SuggestionCommentId);

            builder.Property(s => s.Type)
                .IsRequired();
        }
    }
}
