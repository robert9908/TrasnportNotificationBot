namespace TransportBot.WebApi.DTOs
{
    public record UpdateUserRequest(string? UserName, double? Latitude, double? Longitude);
}
