using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TransportBot.Core.Interfaces;

namespace TransportBot.Services.BackgroundServices
{
    public class TelegramBotPollingService : BackgroundService
    {
        private readonly ILogger<TelegramBotPollingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelegramBotClient _botClient;

        public TelegramBotPollingService(
            ILogger<TelegramBotPollingService> logger,
            IServiceProvider serviceProvider,
            ITelegramBotClient botClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _botClient = botClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telegram Bot Polling Service started");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
                ThrowPendingUpdates = true
            };

            try
            {
                _botClient.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    stoppingToken);

                var me = await _botClient.GetMeAsync(stoppingToken);
                _logger.LogInformation("Bot @{BotUsername} started successfully", me.Username);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Telegram Bot Polling Service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Telegram Bot Polling Service");
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var telegramBotService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
                await telegramBotService.HandleUpdateAsync(update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram Bot API error occurred");
            return Task.CompletedTask;
        }
    }
}
