using AnonymousDomain.Models.AnonymousUser;
using AnonymousDomain.Models.Attachment;
using AnonymousDomain.Models.BillingNotification;
using AnonymousDomain.Models.Organization;
using AnonymousDomain.Models.Problem;
using AnonymousDomain.Models.ProblemBox;
using AnonymousDomain.Models.Subscription;
using AnonymousDomain.Models.Suggestion;
using AnonymousDomain.Models.SuggestionBox;
using AnonymousDomain.Models.SystemNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace AnonymousInfrastructure.Persistence
{
    public class AnonDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AnonDbContext(
            DbContextOptions options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<AnonymousUser> AnonymousUsers { get; set; }
        public DbSet<BoxAccessLink> BoxAccessLinks { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<BillingNotification> BillingNotifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<ProblemCategory> ProblemCategories { get; set; }
        public DbSet<ProblemComment> ProblemComments { get; set; }
        public DbSet<ProblemBox> ProblemBoxes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<SuggestionCategories> SuggestionCategories { get; set; }
        public DbSet<SuggestionCategory> SuggestionCategoryList { get; set; }
        public DbSet<SuggestionComment> SuggestionComments { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<SuggestionBox> SuggestionBoxes { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("AppDB"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
