using TransportBot.Core.DTOs;

namespace TransportBot.Core.Interfaces
{
    public interface IYandexApiService
    {
        Task<IEnumerable<TransportArrival>> GetArrivalsAsync(string stationCode);
        Task<int> GetMinutesUntilArrivalAsync(string routeNumber, string stationCode);
        Task<bool> IsRouteActiveAsync(string routeNumber, string stationCode);
        Task<IEnumerable<ExternalStation>> GetNearbyStationsAsync(double latitude, double longitude, int distanceMeters = 1000);
    }
}
