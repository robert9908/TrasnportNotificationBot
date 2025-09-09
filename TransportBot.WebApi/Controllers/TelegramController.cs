using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using TransportBot.Core.Interfaces;

namespace TransportBot.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramBotService _telegramBotService;

        public TelegramController(ITelegramBotService telegramBotService)
        {
            _telegramBotService = telegramBotService;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] Update update)
        {
            await _telegramBotService.HandleUpdateAsync(update);
            return Ok();
        }
    }
}
