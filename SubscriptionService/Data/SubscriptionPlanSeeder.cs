using SubscriptionService.Context;
using SubscriptionService.Models;

namespace SubscriptionService.Data
{
    /// <summary>
    /// Idempotent seeding of the canonical subscription plans. Runs only when the
    /// SubscriptionPlans table is empty, so existing databases are left untouched.
    /// Titles match the price inference in the admin frontend (Basic/Pro/Premium).
    /// </summary>
    public static class SubscriptionPlanSeeder
    {
        public static readonly IReadOnlyList<SubscriptionPlan> CanonicalPlans = new[]
        {
            new SubscriptionPlan { Title = "Basic",   Description = "Basic plan for small teams." },
            new SubscriptionPlan { Title = "Pro",     Description = "Pro plan for growing organizations." },
            new SubscriptionPlan { Title = "Premium", Description = "Premium plan with all features." },
        };

        public static void Seed(SubscriptionContext context)
        {
            if (context.SubscriptionPlans.Any())
                return;

            foreach (var plan in CanonicalPlans)
            {
                context.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Title = plan.Title,
                    Description = plan.Description
                });
            }

            context.SaveChanges();
        }
    }
}
