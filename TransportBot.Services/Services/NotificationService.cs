using Microsoft.Extensions.Logging;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;

namespace TransportBot.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ITelegramBotService _telegramBotService;
        private readonly ITransportApiService _transportApiService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ISubscriptionRepository subscriptionRepository,
            ITelegramBotService telegramBotService,
            ITransportApiService transportApiService,
            ILogger<NotificationService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _telegramBotService = telegramBotService;
            _transportApiService = transportApiService;
            _logger = logger;
        }

        public async Task CheckAndSendNotificationsAsync()
        {
            try
            {
                var activeSubscriptions = await _subscriptionRepository.GetActiveSubscriptionsAsync();
                
                foreach (var subscription in activeSubscriptions)
                {
                    var timeSinceLastNotification = DateTime.UtcNow - subscription.LastNotifiedAt;
                    if (timeSinceLastNotification.TotalMinutes < 1)
                        continue;
                    int minutesUntilArrival = -1;
                    if (!string.IsNullOrWhiteSpace(subscription.ExternalRouteNumber) && !string.IsNullOrWhiteSpace(subscription.ExternalStopCode))
                    {
                        minutesUntilArrival = await _transportApiService.GetMinutesUntilArrivalAsync(
                            subscription.ExternalRouteNumber!, subscription.ExternalStopCode!);
                    }
                    else if (subscription.RouteId.HasValue && subscription.StopId.HasValue)
                    {
                        minutesUntilArrival = await _transportApiService.GetMinutesUntilArrivalAsync(
                            subscription.RouteId.Value, subscription.StopId.Value);
                    }
                    
                    if (minutesUntilArrival == subscription.NotifyBeforeMinutes)
                    {
                        await SendNotificationAsync(subscription, minutesUntilArrival);
                        
                        subscription.LastNotifiedAt = DateTime.UtcNow;
                        await _subscriptionRepository.UpdateAsync(subscription);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and sending notifications");
            }
        }

        public async Task SendNotificationAsync(Subscription subscription, int minutesUntilArrival)
        {
            try
            {
                var message = $"🚌 Уведомление о транспорте!\n\n" +
                             $"📍 Остановка: {subscription.Stop?.Name}\n" +
                             $"🚌 Маршрут: {subscription.Route?.Name}\n" +
                             $"⏰ Прибытие через: {minutesUntilArrival} мин\n\n" +
                             $"Готовьтесь к поездке! 🎯";

                await _telegramBotService.SendMessageAsync(subscription.User.TelegramId, message);
                
                _logger.LogInformation($"Notification sent to user {subscription.User.TelegramId} for route {subscription.Route?.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification to user {subscription.User?.TelegramId}");
            }
        }

    }
}
