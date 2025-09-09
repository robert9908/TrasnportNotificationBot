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
                _logger.LogInformation("Checking {Count} active subscriptions for notifications", activeSubscriptions.Count());
                
                foreach (var subscription in activeSubscriptions)
                {
                    await ProcessSubscriptionAsync(subscription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and sending notifications");
            }
        }

        private async Task ProcessSubscriptionAsync(Subscription subscription)
        {
            try
            {
                var timeSinceLastNotification = DateTime.UtcNow - subscription.LastNotifiedAt;
                if (timeSinceLastNotification.TotalMinutes < 5)
                {
                    _logger.LogDebug("Skipping subscription {Id} - notification sent recently", subscription.Id);
                    return;
                }

                int minutesUntilArrival = -1;
                
                if (!string.IsNullOrWhiteSpace(subscription.ExternalRouteNumber) && !string.IsNullOrWhiteSpace(subscription.ExternalStopCode))
                {
                    _logger.LogDebug("Getting arrival time for route {Route} at stop {Stop}", 
                        subscription.ExternalRouteNumber, subscription.ExternalStopCode);
                    
                    minutesUntilArrival = await _transportApiService.GetMinutesUntilArrivalAsync(
                        subscription.ExternalRouteNumber!, subscription.ExternalStopCode!);
                }
                else if (subscription.RouteId.HasValue && subscription.StopId.HasValue)
                {
                    _logger.LogDebug("Getting arrival time for route ID {RouteId} at stop ID {StopId}", 
                        subscription.RouteId.Value, subscription.StopId.Value);
                    
                    minutesUntilArrival = await _transportApiService.GetMinutesUntilArrivalAsync(
                        subscription.RouteId.Value, subscription.StopId.Value);
                }
                else
                {
                    _logger.LogWarning("Subscription {Id} has no valid route/stop data", subscription.Id);
                    return;
                }

                _logger.LogDebug("Route arrives in {Minutes} minutes, notification set for {NotifyBefore} minutes", 
                    minutesUntilArrival, subscription.NotifyBeforeMinutes);
                if (ShouldSendNotification(minutesUntilArrival, subscription.NotifyBeforeMinutes))
                {
                    await SendNotificationAsync(subscription, minutesUntilArrival);
                    
                    subscription.LastNotifiedAt = DateTime.UtcNow;
                    await _subscriptionRepository.UpdateAsync(subscription);
                    
                    _logger.LogInformation("Notification sent for subscription {Id}", subscription.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription {Id}", subscription.Id);
            }
        }

        private static bool ShouldSendNotification(int minutesUntilArrival, int notifyBeforeMinutes)
        {
            return minutesUntilArrival > 0 && minutesUntilArrival <= notifyBeforeMinutes;
        }

        public async Task SendNotificationAsync(Subscription subscription, int minutesUntilArrival)
        {
            try
            {
                var routeName = GetRouteName(subscription);
                var stopName = GetStopName(subscription);
                
                var message = $"🚌 Уведомление о транспорте!\n\n" +
                             $"📍 Остановка: {stopName}\n" +
                             $"🚌 Маршрут: {routeName}\n" +
                             $"⏰ Прибытие через: {minutesUntilArrival} мин\n\n" +
                             $"Готовьтесь к поездке! 🎯";

                await _telegramBotService.SendMessageAsync(subscription.User.TelegramId, message);
                
                _logger.LogInformation("Notification sent to user {UserId} for route {Route} at stop {Stop}", 
                    subscription.User.TelegramId, routeName, stopName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", subscription.User?.TelegramId);
            }
        }

        private static string GetRouteName(Subscription subscription)
        {
            if (!string.IsNullOrWhiteSpace(subscription.ExternalRouteNumber))
                return subscription.ExternalRouteNumber;
            
            return subscription.Route?.Name ?? "Неизвестный маршрут";
        }

        private static string GetStopName(Subscription subscription)
        {
            if (!string.IsNullOrWhiteSpace(subscription.ExternalStopCode))
                return subscription.ExternalStopCode;
            
            return subscription.Stop?.Name ?? "Неизвестная остановка";
        }

    }
}
