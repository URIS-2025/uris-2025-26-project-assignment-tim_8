using AnonymousDomain.Models.AnonymousUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class AnonymousUserConfiguration : IEntityTypeConfiguration<AnonymousUser>
    {
        public void Configure(EntityTypeBuilder<AnonymousUser> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.BoxAccessLinkId)
                .IsRequired();
        }
    }
}
