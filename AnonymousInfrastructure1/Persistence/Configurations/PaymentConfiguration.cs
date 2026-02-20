using AnonymousDomain.Models.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousInfrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Total)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.SubscriptionId)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.Currency)
                .IsRequired();

            builder.Property(p => p.PaymentMethod)
                .IsRequired();
        }
    }
}
