using AnonymousDomain.Models.Suggestion;
using AnonymousDomain.Models.Attachment;
using AnonymousDomain.Models.AnonymousUser;
using AnonymousDomain.Enums;
using Microsoft.EntityFrameworkCore;

public class SuggestionContext : DbContext
{
    private readonly IConfiguration _configuration;

    public SuggestionContext(
        DbContextOptions options,
        IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Suggestion> Suggestions { get; set; }
    public DbSet<SuggestionComment> SuggestionComments { get; set; }
    public DbSet<SuggestionCategory> SuggestionCategories { get; set; }
    public DbSet<SuggestionCategories> SuggestionCategoriesJoin { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Attachment> Attachments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(
            _configuration.GetConnectionString("SuggestionDB"));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SuggestionCategory>(entity =>
        {
            entity.HasKey(sc => sc.Id);

            entity.Property(sc => sc.Title)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(sc => sc.Description)
                  .HasMaxLength(500);
        });

        builder.Entity<SuggestionCategories>(entity =>
        {
            entity.HasKey(sc => new { sc.SuggestionId, sc.SuggestionCategoryId });

            entity.HasOne<Suggestion>()
                  .WithMany()
                  .HasForeignKey(sc => sc.SuggestionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<SuggestionCategory>()
                  .WithMany()
                  .HasForeignKey(sc => sc.SuggestionCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Suggestion>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(s => s.Description)
                  .HasMaxLength(2000);

            entity.Property(s => s.Status)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.Property(s => s.CreatedAt)
                  .IsRequired();

            entity.Property(s => s.AnonymousUserId)
                  .IsRequired();
        });

        builder.Entity<SuggestionComment>(entity =>
        {
            entity.HasKey(sc => sc.Id);

            entity.Property(sc => sc.Text)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(sc => sc.IsAnonymous)
                  .IsRequired();

            entity.Property(sc => sc.CreatedBy)
                  .HasMaxLength(100);

            entity.Property(sc => sc.CreatedAt)
                  .IsRequired();

            entity.HasOne<SuggestionComment>()
                  .WithMany()
                  .HasForeignKey(sc => sc.SuggestionCommentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Suggestion>()
                  .WithMany()
                  .HasForeignKey(sc => sc.SuggestionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(sc => sc.CommentAuthorId)
                  .IsRequired(false);
        });

        builder.Entity<Vote>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.CreatedAt)
                  .IsRequired();

            entity.Property(v => v.VoteAuthorId)
                  .IsRequired();

            entity.HasOne<Suggestion>()
                  .WithMany()
                  .HasForeignKey(v => v.SuggestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

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

            entity.HasOne<Suggestion>()
                  .WithMany()
                  .HasForeignKey(a => a.SuggestionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}