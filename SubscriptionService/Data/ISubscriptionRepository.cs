using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public interface ISubscriptionRepository
    {
        IEnumerable<SubscriptionDTO> GetAllSubscriptions();

        SubscriptionDTO GetSubscriptionById(Guid id);

        IEnumerable<SubscriptionDTO> GetSubscriptionsByPlanId(Guid planId);

        SubscriptionCreatedDTO CreateSubscription(SubscriptionCreationDTO subscription);

        SubscriptionCreatedDTO UpdateSubscription(SubscriptionDTO subscription);

        void DeleteSubscription(Guid id);
    }
}