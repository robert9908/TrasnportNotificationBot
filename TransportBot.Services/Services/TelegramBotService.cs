using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using TransportBot.Core.DTOs;

namespace TransportBot.Services.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IUserService _userService;
        private readonly ITransportStopService _transportStopService;
        private readonly ITransportApiService _transportApiService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IRouteService _routeService;

        public TelegramBotService(
            IConfiguration configuration,
            ILogger<TelegramBotService> logger,
            IUserService userService,
            ITransportStopService transportStopService,
            ISubscriptionService subscriptionService,
            IRouteService routeService,
            ITransportApiService transportApiService)
        {
            var botToken = configuration["TelegramBot:Token"] 
                ?? throw new ArgumentException("Telegram bot token not found in configuration");
            
            _botClient = new TelegramBotClient(botToken);
            _logger = logger;
            _userService = userService;
            _transportStopService = transportStopService;
            _subscriptionService = subscriptionService;
            _routeService = routeService;
            _transportApiService = transportApiService;
        }

        private async Task ShowNearbyFromStoredLocationAsync(long chatId, long userId, int distance)
        {
            var user = await _userService.GetUserByTelegramIdAsync(chatId);
            if (user?.LocationLatitude is double lat && user?.LocationLongitude is double lon)
            {
                var stations = await _transportApiService.GetNearbyStationsAsync(lat, lon, distance);
                var list = stations.Take(10).ToList();
                if (list.Any())
                {
                    var buttons = new List<List<(string text, string callbackData)>>();
                    foreach (var st in list)
                    {
                        var title = $"📍 {st.Title} ({st.Distance:F0} м)";
                        buttons.Add(new List<(string, string)>{(title, $"yst_{st.Code}")});
                    }
                    await SendInlineKeyboardAsync(chatId, "🚏 Ближайшие станции (Яндекс):", buttons);
                }
                else
                {
                    await SendMessageAsync(chatId, "Рядом не найдено станций. Попробуйте другую локацию или команду /search <название>.");
                }
            }
            else
            {
                await SendMessageAsync(chatId, "Локация не задана. Задайте: /setlocation <lat> <lon>");
            }
        }

        public async Task HandleUpdateAsync(Update update)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await HandleMessageAsync(update.Message!);
                        break;
                    case UpdateType.CallbackQuery:
                        await HandleCallbackQueryAsync(update.CallbackQuery!);
                        break;
                    default:
                        _logger.LogWarning("Unhandled update type: {UpdateType}", update.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Telegram update: {UpdateType}", update.Type);
                
                try
                {
                    var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
                    if (chatId.HasValue)
                    {
                        await SendMessageAsync(chatId.Value, "Произошла ошибка. Попробуйте позже или обратитесь к администратору.");
                    }
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "Failed to send error message to user");
                }
            }
        }

        private async Task HandleMessageAsync(Message message)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id ?? 0;

            await _userService.RegisterAsync(userId, message.From?.Username);

            switch (message.Type)
            {
                case MessageType.Text:
                    await HandleTextMessageAsync(chatId, message.Text!, userId);
                    break;
                case MessageType.Location:
                    await HandleLocationAsync(chatId, message.Location!, userId);
                    break;
                default:
                    _logger.LogWarning("Unhandled message type: {MessageType} from user {UserId}", message.Type, userId);
                    break;
            }
        }

        private async Task HandleTextMessageAsync(long chatId, string text, long userId)
        {
            if (text.StartsWith("/search", StringComparison.OrdinalIgnoreCase))
            {
                var searchQuery = text.Length > 7 ? text.Substring(7).Trim() : string.Empty;
                await SearchStopsByNameAsync(chatId, searchQuery);
                return;
            }
            if (text.StartsWith("/diag_station", StringComparison.OrdinalIgnoreCase))
            {
                var code = text.Length > 13 ? text.Substring(13).Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                {
                    await SendMessageAsync(chatId, "Использование: /diag_station <код_станции_Яндекс>\nНапример: /diag_station s9602494");
                    return;
                }

                await SendMessageAsync(chatId, $"🔎 Диагностика станции {code}: загружаю данные...");
                var arrivals = await _transportApiService.GetArrivalsAsync(code);
                var list = arrivals.ToList();
                if (list.Any())
                {
                    var sample = string.Join("\n", list.Take(3).Select(a => $"• {a.RouteName} ({a.RouteNumber}) — через {a.MinutesUntilArrival} мин"));
                    await SendMessageAsync(chatId, $"✅ Найдено отправлений: {list.Count}\n{sample}");
                }
                else
                {
                    await SendMessageAsync(chatId, "❌ Табло пустое. Подробности смотрите в логах сервера (URL и Raw-ответ). Убедитесь, что код станции корректен и есть актуальные отправления.");
                }
                return;
            }
            if (text.StartsWith("/station", StringComparison.OrdinalIgnoreCase))
            {
                var code = text.Length > 8 ? text.Substring(8).Trim() : string.Empty;
                if (string.IsNullOrWhiteSpace(code))
                {
                    await SendMessageAsync(chatId, "Использование: /station <код_станции_Яндекс>\nНапример: /station s9602494\n\n💡 Коды станций можно найти на сайте: https://rasp.yandex.ru");
                    return;
                }

                await SendMessageAsync(chatId, $"📍 Станция: {code}. Загружаю ближайшие отправления...");
                var arrivals = await _transportApiService.GetArrivalsAsync(code);
                var list = arrivals.Take(8).ToList();
                if (list.Any())
                {
                    var buttons = new List<List<(string text, string callbackData)>>();
                    foreach (var a in list)
                    {
                        var title = $"{GetRouteIcon(a.RouteType)} {a.RouteName} • через {a.MinutesUntilArrival} мин";
                        var cb = $"yrt_{code}_{a.RouteNumber}";
                        buttons.Add(new List<(string, string)>{(title, cb)});
                    }
                    await SendInlineKeyboardAsync(chatId, "🕒 Ближайшие отправления:", buttons);
                }
                else
                {
                    await SendMessageAsync(chatId, "Нет ближайших отправлений по этой станции.");
                }
                return;
            }
            if (text.StartsWith("/setlocation", StringComparison.OrdinalIgnoreCase))
            {
                var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (args.Length == 3 && double.TryParse(args[1], System.Globalization.CultureInfo.InvariantCulture, out var lat)
                    && double.TryParse(args[2], System.Globalization.CultureInfo.InvariantCulture, out var lon))
                {
                    var user = await _userService.RegisterAsync(userId, null);
                    await _userService.UpdateAsync(user.Id, null, lat, lon);
                    await SendMessageAsync(chatId, $"✅ Локация обновлена: {lat:F6}, {lon:F6}. Выполняю поиск ближайших станций...");
                    await ShowNearbyFromStoredLocationAsync(chatId, userId, 1200);
                }
                else
                {
                    await SendMessageAsync(chatId, "Использование: /setlocation <lat> <lon>\nНапример: /setlocation 55.7558 37.6176");
                }
                return;
            }
            if (string.Equals(text, "/nearby", StringComparison.OrdinalIgnoreCase))
            {
                await ShowNearbyFromStoredLocationAsync(chatId, userId, 1200);
                return;
            }

            switch (text.ToLower())
            {
                case "/start":
                    await SendWelcomeMessageAsync(chatId);
                    break;
                case "/help":
                    await SendHelpMessageAsync(chatId);
                    break;
                case "/nearby":
                    await ShowNearbyFromStoredLocationAsync(chatId, userId, 1200);
                    break;
                case "/subscriptions":
                    await ShowUserSubscriptionsAsync(chatId, userId);
                    break;
                case "/stops":
                    await ShowAllStopsAsync(chatId);
                    break;
                case "/moscow":
                    await ShowMoscowStopsAsync(chatId);
                    break;
                default:
                    await SendMessageAsync(chatId, "Извините, я не понимаю эту команду. Используйте /help для получения списка команд.");
                    break;
            }
        }

        private async Task HandleLocationAsync(long chatId, Location location, long userId)
        {
            try
            {
                _logger.LogInformation("Received location from user {UserId}: Lat={Latitude}, Lon={Longitude}", 
                    userId, location.Latitude, location.Longitude);

                var user = await _userService.RegisterAsync(userId, null);
                await _userService.UpdateAsync(user.Id, null, location.Latitude, location.Longitude);

                await SendMessageAsync(chatId, $"✅ Локация получена!\n📍 Широта: {location.Latitude:F6}\n📍 Долгота: {location.Longitude:F6}");
                
                await SendMessageAsync(chatId, "🔍 Ищу ближайшие остановки...");
                
                var stations = await _transportApiService.GetNearbyStationsAsync(location.Latitude, location.Longitude, 1200);
                var stationsList = stations.Take(10).ToList();

                _logger.LogInformation("Found {Count} nearby Yandex stations for user {UserId}", stationsList.Count, userId);

                if (stationsList.Any())
                {
                    var buttons = new List<List<(string text, string callbackData)>>();
                    
                    foreach (var st in stationsList)
                    {
                        var title = $"📍 {st.Title} ({st.Distance:F0} м)";
                        buttons.Add(new List<(string, string)>{(title, $"yst_{st.Code}")});
                    }

                    await SendInlineKeyboardAsync(chatId, "🚏 Выберите остановку (данные Яндекс):", buttons);
                }
                else
                {
                    await SendMessageAsync(chatId, "😔 Рядом не найдено станций в радиусе 1.2 км. Попробуйте другую локацию или команду /search <название>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling location from user {UserId}", userId);
                await SendMessageAsync(chatId, "❌ Произошла ошибка при обработке локации. Попробуйте еще раз или обратитесь к администратору.");
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            var data = callbackQuery.Data!;
            var userId = callbackQuery.From.Id;

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

            if (data == "moscow_stops")
            {
                await ShowMoscowStopsAsync(chatId);
            }
            else if (data == "all_stops")
            {
                await ShowAllStopsAsync(chatId);
            }
            else if (data.StartsWith("yst_"))
            {
                var stationCode = data.Substring(4);
                await SendMessageAsync(chatId, $"📍 Станция: {stationCode}. Загружаю ближайшие отправления...");

                var arrivals = await _transportApiService.GetArrivalsAsync(stationCode);
                var list = arrivals.Take(8).ToList();

                if (list.Any())
                {
                    var buttons = new List<List<(string text, string callbackData)>>();
                    foreach (var a in list)
                    {
                        var title = $"{GetRouteIcon(a.RouteType)} {a.RouteName} • через {a.MinutesUntilArrival} мин";
                        var cb = $"yrt_{stationCode}_{a.RouteNumber}";
                        buttons.Add(new List<(string, string)>{(title, cb)});
                    }

                    await SendInlineKeyboardAsync(chatId, "🕒 Ближайшие отправления (Яндекс):", buttons);
                }
                else
                {
                    await SendMessageAsync(chatId, "Нет ближайших отправлений по этой станции.");
                }
            }
            else if (data.StartsWith("yrt_"))
            {
                var parts = data.Split('_');
                if (parts.Length >= 3)
                {
                    var stationCode = parts[1];
                    var routeNumber = string.Join('_', parts.Skip(2));

                    var minutes = await _transportApiService.GetMinutesUntilArrivalAsync(routeNumber, stationCode);
                    if (minutes >= 0)
                    {
                        await SendMessageAsync(chatId, $"✅ Маршрут №{routeNumber}\n📍 Станция: {stationCode}\n⏰ Прибытие через {minutes} мин");
                    }
                    else
                    {
                        await SendMessageAsync(chatId, $"Для маршрута №{routeNumber} нет данных о прибытии.");
                    }

                    var buttons = new List<List<(string text, string callbackData)>>
                    {
                        new() { ("⏰ За 5 минут", $"ynotify_{stationCode}_{routeNumber}_5"), ("⏰ За 10 минут", $"ynotify_{stationCode}_{routeNumber}_10") },
                        new() { ("⏰ За 15 минут", $"ynotify_{stationCode}_{routeNumber}_15"), ("⏰ За 20 минут", $"ynotify_{stationCode}_{routeNumber}_20") }
                    };

                    await SendInlineKeyboardAsync(chatId, "⏰ За сколько минут до прибытия уведомлять?", buttons);
                }
            }
            else if (data.StartsWith("ynotify_"))
            {
                var parts = data.Split('_');
                if (parts.Length >= 4)
                {
                    var stationCode = parts[1];
                    var routeNumber = parts[2];
                    var minutes = int.Parse(parts[3]);

                    var user = await _userService.GetUserByTelegramIdAsync(chatId);
                    if (user != null)
                    {
                        var subscription = new Subscription
                        {
                            UserId = user.Id,
                            ExternalStopCode = stationCode,
                            ExternalRouteNumber = routeNumber,
                            NotifyBeforeMinutes = minutes,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _subscriptionService.CreateSubscriptionAsync(subscription);
                        await SendMessageAsync(chatId, $"✅ Подписка создана!\n📍 Станция: {stationCode}\n🚌 Маршрут: {routeNumber}\n⏰ За {minutes} минут до прибытия");
                    }
                }
            }
            else if (data.StartsWith("stop_"))
            {
                var stopId = int.Parse(data.Replace("stop_", ""));
                var stop = await _transportStopService.GetStopAsync(stopId);
                
                if (stop != null)
                {
                    await SendMessageAsync(chatId, $"📍 Вы выбрали остановку: {stop.Name}");
                    
                    var routes = await _routeService.GetRoutesByStopAsync(stopId);
                    var routesList = routes.ToList();

                    if (routesList.Any())
                    {
                        var buttons = new List<List<(string text, string callbackData)>>();
                        
                        foreach (var route in routesList.Take(8))
                        {
                            var routeIcon = GetRouteIcon(route.Type);
                            buttons.Add(new List<(string, string)> 
                            { 
                                ($"{routeIcon} {route.Name}", $"route_{stopId}_{route.Id}") 
                            });
                        }

                        await SendInlineKeyboardAsync(chatId, "🚌 Выберите маршрут:", buttons);
                    }
                    else
                    {
                        await SendMessageAsync(chatId, "😔 На этой остановке пока нет доступных маршрутов.");
                    }
                }
            }
            else if (data.StartsWith("route_"))
            {
                var parts = data.Split('_');
                var stopId = int.Parse(parts[1]);
                var routeId = int.Parse(parts[2]);
                
                var stop = await _transportStopService.GetStopAsync(stopId);
                var route = await _routeService.GetRouteAsync(routeId);
                
                if (stop != null && route != null)
                {
                    var routeIcon = GetRouteIcon(route.Type);
                    await SendMessageAsync(chatId, $"✅ Выбрано:\n📍 {stop.Name}\n{routeIcon} {route.Name}");
                    
                    var buttons = new List<List<(string text, string callbackData)>>
                    {
                        new() { ("⏰ За 5 минут", $"notify_{stopId}_{routeId}_5"), ("⏰ За 10 минут", $"notify_{stopId}_{routeId}_10") },
                        new() { ("⏰ За 15 минут", $"notify_{stopId}_{routeId}_15"), ("⏰ За 20 минут", $"notify_{stopId}_{routeId}_20") }
                    };

                    await SendInlineKeyboardAsync(chatId, "⏰ За сколько минут до прибытия уведомлять?", buttons);
                }
            }
            else if (data.StartsWith("notify_"))
            {
                var parts = data.Split('_');
                var stopId = int.Parse(parts[1]);
                var routeId = int.Parse(parts[2]);
                var minutes = int.Parse(parts[3]);

                var user = await _userService.GetUserByTelegramIdAsync(chatId);
                if (user != null)
                {
                    var subscription = new Subscription
                    {
                        UserId = user.Id,
                        StopId = stopId,
                        RouteId = routeId,
                        NotifyBeforeMinutes = minutes,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _subscriptionService.CreateSubscriptionAsync(subscription);
                    
                    var stop = await _transportStopService.GetStopAsync(stopId);
                    var route = await _routeService.GetRouteAsync(routeId);
                    var routeIcon = GetRouteIcon(route?.Type ?? "");
                    
                    await SendMessageAsync(chatId, 
                        $"✅ Подписка создана!\n" +
                        $"📍 Остановка: {stop?.Name}\n" +
                        $"{routeIcon} Маршрут: {route?.Name}\n" +
                        $"⏰ Уведомления за {minutes} минут до прибытия");
                }
            }
            else if (data == "my_subscriptions")
            {
                var user = await _userService.RegisterAsync(userId, null);
                var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(user.Id);
                
                if (subscriptions.Any())
                {
                    var message = "📋 Ваши подписки:\n\n";
                    foreach (var sub in subscriptions)
                    {
                        var status = sub.IsActive ? "✅" : "❌";
                        var routeIcon = GetRouteIcon(sub.Route?.Type ?? "");
                        message += $"{status} {sub.Stop?.Name} - {routeIcon} {sub.Route?.Name} - за {sub.NotifyBeforeMinutes} мин\n";
                    }
                    
                    await SendMessageAsync(chatId, message);
                }
                else
                {
                    await SendMessageAsync(chatId, "У вас пока нет подписок. Поделитесь локацией, чтобы создать первую подписку!");
                }
            }
            else if (data == "new_location")
            {
                await SendLocationRequestAsync(chatId, "Поделитесь новой локацией для поиска остановок:");
            }
            else if (data.StartsWith("delete_"))
            {
                var subscriptionId = int.Parse(data.Replace("delete_", ""));
                var success = await _subscriptionService.DeleteSubscriptionAsync(subscriptionId);
                
                if (success)
                {
                    await SendMessageAsync(chatId, "✅ Подписка удалена!");
                }
                else
                {
                    await SendMessageAsync(chatId, "❌ Ошибка при удалении подписки.");
                }
            }
            else if (data.StartsWith("toggle_"))
            {
                var subscriptionId = int.Parse(data.Replace("toggle_", ""));
                var subscription = await _subscriptionService.GetSubscriptionAsync(subscriptionId);
                
                if (subscription != null)
                {
                    var success = await _subscriptionService.ToggleSubscriptionAsync(subscriptionId, !subscription.IsActive);
                    var status = !subscription.IsActive ? "активирована" : "приостановлена";
                    
                    if (success)
                    {
                        await SendMessageAsync(chatId, $"✅ Подписка {status}!");
                    }
                    else
                    {
                        await SendMessageAsync(chatId, "❌ Ошибка при изменении статуса подписки.");
                    }
                }
            }
        }

        private async Task SendWelcomeMessageAsync(long chatId)
        {
            var message = "🚌 Добро пожаловать в бот уведомлений о транспорте!\n\n" +
                         "Я помогу вам получать уведомления о прибытии общественного транспорта.\n\n" +
                         "🔍 Способы поиска остановок:\n" +
                         "📍 Поделитесь геолокацией для поиска ближайших остановок\n" +
                         "🚉 /station <код_станции> - поиск по коду станции (например: /station s9602494)\n\n" +
                         "⚠️ Поиск по названию (/search) временно не работает";

            await SendLocationRequestAsync(chatId, message);
        }

        private async Task ShowUserSubscriptionsAsync(long chatId, long userId)
        {
            var user = await _userService.GetUserByTelegramIdAsync(chatId);
            if (user == null)
            {
                await SendMessageAsync(chatId, "Сначала зарегистрируйтесь, отправив команду /start");
                return;
            }

            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(user.Id);
            
            if (subscriptions.Any())
            {
                var message = "📋 Ваши подписки:\n\n";
                foreach (var sub in subscriptions)
                {
                    var status = sub.IsActive ? "✅" : "❌";
                    var routeIcon = GetRouteIcon(sub.Route?.Type ?? "");
                    message += $"{status} {sub.Stop?.Name} - {routeIcon} {sub.Route?.Name} - за {sub.NotifyBeforeMinutes} мин\n";
                }
                
                var buttons = new List<List<(string text, string callbackData)>>
                {
                    new() { ("📍 Новая локация", "new_location") }
                };

                foreach (var sub in subscriptions.Take(5))
                {
                    var toggleText = sub.IsActive ? "⏸️ Приостановить" : "▶️ Активировать";
                    buttons.Add(new List<(string, string)> 
                    { 
                        (toggleText, $"toggle_{sub.Id}"),
                        ("🗑️ Удалить", $"delete_{sub.Id}")
                    });
                }

                await SendInlineKeyboardAsync(chatId, message, buttons);
            }
            else
            {
                await SendMessageAsync(chatId, "У вас пока нет подписок. Поделитесь локацией, чтобы создать первую подписку!");
            }
        }

        private async Task SendHelpMessageAsync(long chatId)
        {
            var helpMessage = @"📋 Список команд:

/start - Начать работу с ботом
/location - Поделиться локацией
/station <код> - Расписание по коду станции
/subscriptions - Показать мои подписки
/help - Показать эту справку

🔔 Как получать уведомления:
1. Поделитесь локацией
2. Выберите ближайшую остановку
3. Выберите маршрут транспорта
4. Настройте время уведомлений (5, 10, 15, 20 минут)
5. Получайте уведомления о прибытии транспорта!

🚉 Коды станций можно найти на: https://rasp.yandex.ru

💡 Используйте кнопки внизу экрана для быстрого доступа к функциям.";

            await SendMessageAsync(chatId, helpMessage);
        }

        private string GetTransportTypeName(string routeType)
        {
            return routeType?.ToLower() switch
            {
                "bus" => "🚌 Автобусы",
                "tram" => "🚋 Трамваи",
                "metro" => "🚇 Метро",
                "minibus" => "🚐 Маршрутки",
                _ => "Неизвестный тип"
            };
        }

        private async Task ShowMoscowStopsAsync(long chatId)
        {
            await SendMessageAsync(chatId, "🔍 Поделитесь геолокацией для поиска ближайших остановок или используйте /search <название>");
        }

        private async Task ShowAllStopsAsync(long chatId)
        {
            await SendMessageAsync(chatId, "🔍 Поделитесь геолокацией для поиска ближайших остановок или используйте /search <название>");
        }

        private async Task SearchStopsByNameAsync(long chatId, string searchQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    await SendMessageAsync(chatId, "❌ Введите название остановки для поиска.\nПример: /search Красная площадь");
                    return;
                }

                await SendMessageAsync(chatId, $"🔍 Ищу станции по запросу \"{searchQuery}\"...");
                
                _logger.LogInformation("Searching for stations with query: {Query}", searchQuery);
                var stations = await _transportApiService.SearchStationsAsync(searchQuery);
                var list = stations.Take(10).ToList();
                
                _logger.LogInformation("Found {Count} stations for query: {Query}", list.Count, searchQuery);

                if (list.Any())
                {
                    var buttons = new List<List<(string text, string callbackData)>>();
                    
                    foreach (var st in list)
                    {
                        var title = $"📍 {st.Title}";
                        buttons.Add(new List<(string, string)>{(title, $"yst_{st.Code}")});
                    }

                    await SendInlineKeyboardAsync(chatId, $"🔍 Найденные станции по запросу \"{searchQuery}\":", buttons);
                }
                else
                {
                    await SendMessageAsync(chatId, $"❌ Станции по запросу \"{searchQuery}\" не найдены.\n\nПопробуйте:\n• /search Москва {searchQuery}\n• /search метро {searchQuery}\n• /station s9602494 (прямой код)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stops by name: {SearchQuery}", searchQuery);
                await SendMessageAsync(chatId, "❌ Ошибка при поиске станций. Попробуйте /station с прямым кодом станции.");
            }
        }

        public async Task SendMessageAsync(long chatId, string message)
        {
            try
            {
                await _botClient.SendTextMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to chat {ChatId}", chatId);
            }
        }

        public async Task SendLocationRequestAsync(long chatId, string message)
        {
            try
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("📍 Поделиться локацией") { RequestLocation = true }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location request to chat {ChatId}", chatId);
            }
        }

        public async Task SendInlineKeyboardAsync(long chatId, string message, List<List<(string text, string callbackData)>> buttons)
        {
            try
            {
                var keyboard = new InlineKeyboardMarkup(
                    buttons.Select(row =>
                        row.Select(button => InlineKeyboardButton.WithCallbackData(button.text, button.callbackData))
                    )
                );

                await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send inline keyboard to chat {ChatId}", chatId);
            }
        }

        private string GetRouteIcon(string routeType)
        {
            return routeType?.ToLower() switch
            {
                "bus" => "🚌",
                "trolleybus" => "🚎",
                "tram" => "🚋",
                "metro" => "🚇",
                "train" => "🚆",
                _ => "🚌"
            };
        }
    }
}
