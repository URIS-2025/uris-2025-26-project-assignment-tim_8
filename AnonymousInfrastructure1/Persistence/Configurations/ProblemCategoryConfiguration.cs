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
    public class ProblemCategoryConfiguration : IEntityTypeConfiguration<ProblemCategory>
    {
        public void Configure(EntityTypeBuilder<ProblemCategory> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.HasData(
                new ProblemCategory { Id = Guid.Parse("c3d4e5f6-0003-0003-0003-000000000001"), Title = "Technical", Description = "Technical issues and bugs" },
                new ProblemCategory { Id = Guid.Parse("c3d4e5f6-0003-0003-0003-000000000002"), Title = "HR", Description = "Human resources related problems" },
                new ProblemCategory { Id = Guid.Parse("c3d4e5f6-0003-0003-0003-000000000003"), Title = "Operations", Description = "Operational and process problems" }
            );
        }
    }
}
