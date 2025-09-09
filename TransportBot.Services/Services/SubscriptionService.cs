using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;

namespace TransportBot.Services.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionService(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
        {
            await _subscriptionRepository.AddAsync(subscription);
            return subscription;
        }

        public async Task<Subscription> CreateSubscriptionAsync(int userId, int stopId, int routeId, int notifyBeforeMinutes)
        {
            var subscription = new Subscription
            {
                UserId = userId,
                StopId = stopId,
                RouteId = routeId,
                NotifyBeforeMinutes = notifyBeforeMinutes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastNotifiedAt = DateTime.MinValue
            };

            await _subscriptionRepository.AddAsync(subscription);
            return subscription;
        }

        public async Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(int userId)
        {
            return await _subscriptionRepository.GetByUserIdAsync(userId);
        }

        public async Task<bool> DeleteSubscriptionAsync(int subscriptionId)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null)
                return false;

            await _subscriptionRepository.DeleteAsync(subscriptionId);
            return true;
        }

        public async Task<Subscription?> GetSubscriptionAsync(int subscriptionId)
        {
            return await _subscriptionRepository.GetByIdAsync(subscriptionId);
        }

        public async Task<bool> ToggleSubscriptionAsync(int subscriptionId, bool isActive)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null)
                return false;

            subscription.IsActive = isActive;
            await _subscriptionRepository.UpdateAsync(subscription);
            return true;
        }
    }
}
