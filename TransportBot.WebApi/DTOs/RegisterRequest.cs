namespace TransportBot.WebApi.DTOs
{
    public record RegisterRequest(long TelegramId, string? UserName);
}
