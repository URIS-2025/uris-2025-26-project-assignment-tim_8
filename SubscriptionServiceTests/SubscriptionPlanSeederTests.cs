using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SubscriptionService.Context;
using SubscriptionService.Data;
using SubscriptionService.Models;
using Xunit;

namespace SubscriptionServiceTests
{
    public class SubscriptionPlanSeederTests
    {
        private static SubscriptionContext NewContext()
        {
            var options = new DbContextOptionsBuilder<SubscriptionContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            return new SubscriptionContext(options, config);
        }

        [Fact]
        public void Seed_AddsThreeCanonicalPlans_WhenEmpty()
        {
            using var context = NewContext();

            SubscriptionPlanSeeder.Seed(context);

            var titles = context.SubscriptionPlans.Select(p => p.Title).OrderBy(t => t).ToList();
            Assert.Equal(3, titles.Count);
            Assert.Equal(new[] { "Basic", "Premium", "Pro" }, titles);
        }

        [Fact]
        public void Seed_DoesNothing_WhenPlansAlreadyExist()
        {
            using var context = NewContext();
            context.SubscriptionPlans.Add(new SubscriptionPlan { Id = Guid.NewGuid(), Title = "Existing", Description = "x" });
            context.SaveChanges();

            SubscriptionPlanSeeder.Seed(context);

            var titles = context.SubscriptionPlans.Select(p => p.Title).ToList();
            Assert.Single(titles);
            Assert.Equal("Existing", titles[0]);
        }

        [Fact]
        public void Seed_IsIdempotent_WhenCalledTwice()
        {
            using var context = NewContext();

            SubscriptionPlanSeeder.Seed(context);
            SubscriptionPlanSeeder.Seed(context);

            Assert.Equal(3, context.SubscriptionPlans.Count());
        }
    }
}
