using TransportBot.Core.DTOs;

namespace TransportBot.Core.Interfaces
{
    public interface IGeocodingService
    {
        Task<GeocodeResult?> GeocodeAsync(string query);
    }
}
