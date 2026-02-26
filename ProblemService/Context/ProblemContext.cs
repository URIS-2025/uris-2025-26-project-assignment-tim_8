
using ProblemService.Models.Problem;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;


namespace ProblemService.Context
{
    public class ProblemContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public ProblemContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Problem> Problems { get; set; }
        public DbSet<ProblemCategory> ProblemCategories { get; set; }
        public DbSet<ProblemComment> ProblemComments { get; set; }
        //public DbSet<ProblemBox> ProblemBoxes { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _configuration.GetConnectionString("ProblemDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Problem>(entity =>
{
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title)
               .IsRequired()
               .HasMaxLength(200);
            entity.Property(p => p.Description)
              .IsRequired()
              .HasMaxLength(1000);
            entity.Property(p => p.CreatedAt)
              .IsRequired();
            entity.Property(p => p.Status)
              .IsRequired();
            entity.Property(p => p.Priority)
              .IsRequired();
          /*  entity.HasOne<ProblemBox>()
              .WithMany()
              .HasForeignKey(p => p.ProblemBoxId)
              .OnDelete(DeleteBehavior.Cascade); */
            });

            builder.Entity<ProblemCategory>(entity =>
            {
                entity.HasKey(pc => pc.Id);
                entity.Property(pc => pc.Title)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(pc => pc.Description)
                      .HasMaxLength(500);
            });

            builder.Entity<ProblemComment>(entity =>
            {
                entity.HasKey(pc => pc.Id);
                entity.Property(pc => pc.CommentText)
                      .IsRequired()
                      .HasMaxLength(1000);
                entity.Property(pc => pc.IsAnonymous)
                      .IsRequired();
                entity.HasOne<Problem>()
                      .WithMany()
                      .HasForeignKey(pc => pc.ProblemId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<ProblemComment>()
                      .WithMany()
                      .HasForeignKey(pc => pc.ProblemCommentId)
                      .OnDelete(DeleteBehavior.NoAction);
              /*  entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(pc => pc.ProblemCommentAuthorId)
                      .OnDelete(DeleteBehavior.Restrict);   */
            });
        }
    }
}
