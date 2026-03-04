using AnonymousDomain.Models.Attachment;
using Microsoft.EntityFrameworkCore;

namespace AttachmentService.Context
{
    public class AttachmentContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AttachmentContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Attachment> Attachments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer(
                    _configuration.GetConnectionString("AttachmentDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Attachment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.FileName)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(a => a.FileType)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.Url)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(a => a.UploadedAt)
                      .IsRequired();

                // TODO: Add validation that at least one of SuggestionId or ProblemId must be provided
                // An attachment must belong to either a Suggestion or a Problem, never neither
                entity.Property(a => a.SuggestionId)
                      .IsRequired(false);

                entity.Property(a => a.ProblemId)
                      .IsRequired(false);
            });
        }
    }
}