using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public interface ISubscriptionPlanRepository
    {
        IEnumerable<SubscriptionPlanDTO> GetAllSubscriptionPlans();

        SubscriptionPlanDTO GetSubscriptionPlanById(Guid id);
    }
}