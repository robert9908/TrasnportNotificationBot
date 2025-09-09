using Telegram.Bot.Types;

namespace TransportBot.Core.Interfaces
{
    public interface ITelegramBotService
    {
        Task HandleUpdateAsync(Update update);
        Task SendMessageAsync(long chatId, string message);
        Task SendLocationRequestAsync(long chatId, string message);
        Task SendInlineKeyboardAsync(long chatId, string message, List<List<(string text, string callbackData)>> buttons);
    }
}
