using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public interface ISubscriptionPlanRepository
    {
        IEnumerable<SubscriptionPlanDTO> GetAllSubscriptionPlans();

        SubscriptionPlanDTO GetSubscriptionPlanById(Guid id);
        SubscriptionPlanDTO CreatePlan(SubscriptionPlanCreationDTO subscriptionPlanDTO);

        SubscriptionPlanDTO UpdatePlan(SubscriptionPlanDTO plan);
    }
}