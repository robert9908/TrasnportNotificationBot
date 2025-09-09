using TransportBot.Core.DTOs;

namespace TransportBot.Core.Interfaces
{
    public interface ITransportApiService
    {
        Task<int> GetMinutesUntilArrivalAsync(int routeId, int stopId);
        Task<IEnumerable<TransportArrival>> GetArrivalsAsync(int stopId);
        Task<bool> IsRouteActiveAsync(int routeId);

        Task<int> GetMinutesUntilArrivalAsync(string routeNumber, string stationCode);
        Task<IEnumerable<TransportArrival>> GetArrivalsAsync(string stationCode);

        Task<IEnumerable<ExternalStation>> GetNearbyStationsAsync(double latitude, double longitude, int distanceMeters = 1000);
        Task<IEnumerable<ExternalStation>> SearchStationsAsync(string query);

        Task<(double lat, double lon)?> GeocodeAsync(string query);
    }
}
