using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface ISubscriptionService
    {
        Task<Subscription> CreateSubscriptionAsync(Subscription subscription);
        Task<Subscription> CreateSubscriptionAsync(int userId, int stopId, int routeId, int notifyBeforeMinutes);
        Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(int userId);
        Task<bool> DeleteSubscriptionAsync(int subscriptionId);
        Task<Subscription?> GetSubscriptionAsync(int subscriptionId);
        Task<bool> ToggleSubscriptionAsync(int subscriptionId, bool isActive);
    }
}
