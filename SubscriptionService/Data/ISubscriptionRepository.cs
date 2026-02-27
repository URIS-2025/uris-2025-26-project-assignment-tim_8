using SubscriptionService.Models.DTOs;

namespace SubscriptionService.Data
{
    public interface ISubscriptionRepository
    {
        IEnumerable<SubscriptionDTO> GetAllSubscriptions();

        SubscriptionDTO GetSubscriptionById(Guid id);

        SubscriptionCreatedDTO CreateSubscription(SubscriptionCreationDTO subscription);

        SubscriptionCreatedDTO UpdateSubscription(SubscriptionDTO subscription);

        void DeleteSubscription(Guid id);
    }
}