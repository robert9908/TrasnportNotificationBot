using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface INotificationService
    {
        Task CheckAndSendNotificationsAsync();
        Task SendNotificationAsync(Subscription subscription, int minutesUntilArrival);
    }
}
