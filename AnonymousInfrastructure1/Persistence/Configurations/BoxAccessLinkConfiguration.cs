using AnonymousDomain.Models.AnonymousUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    internal class BoxAccessLinkConfiguration : IEntityTypeConfiguration<BoxAccessLink>
    {
        public void Configure(EntityTypeBuilder<BoxAccessLink> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.AccessToken)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(b => b.IsActive)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.ExpiresAt)
                .IsRequired();
        }
    }
}
