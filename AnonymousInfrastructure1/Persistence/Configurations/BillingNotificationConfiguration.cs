using AnonymousDomain.Models.BillingNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class BillingNotificationConfiguration : IEntityTypeConfiguration<BillingNotification>
    {
        public void Configure(EntityTypeBuilder<BillingNotification> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Text)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(b => b.IsRead)
                .IsRequired();

            builder.Property(b => b.OrganizationId)
                .IsRequired();

            builder.Property(b => b.PaymentId)
                .IsRequired();


        }
    }
}
