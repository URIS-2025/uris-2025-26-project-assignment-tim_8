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
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.HasData(
                new SubscriptionPlan { Id = Guid.Parse("b2c3d4e5-0002-0002-0002-000000000001"), Title = "Free", Description = "Free tier with basic features" },
                new SubscriptionPlan { Id = Guid.Parse("b2c3d4e5-0002-0002-0002-000000000002"), Title = "Pro", Description = "Professional plan with advanced features" },
                new SubscriptionPlan { Id = Guid.Parse("b2c3d4e5-0002-0002-0002-000000000003"), Title = "Enterprise", Description = "Enterprise plan with full feature access" }
            );
        }
    }
}
