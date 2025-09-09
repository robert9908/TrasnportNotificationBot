using Microsoft.Extensions.Logging;
using TransportBot.Core.Interfaces;
using TransportBot.Core.DTOs;

namespace TransportBot.Services.Services
{
    public class RefactoredTransportApiService : ITransportApiService
    {
        private readonly IYandexApiService _yandexApiService;
        private readonly IGeocodingService _geocodingService;
        private readonly ILogger<RefactoredTransportApiService> _logger;

        public RefactoredTransportApiService(
            IYandexApiService yandexApiService,
            IGeocodingService geocodingService,
            ILogger<RefactoredTransportApiService> logger)
        {
            _yandexApiService = yandexApiService;
            _geocodingService = geocodingService;
            _logger = logger;
        }

        public async Task<int> GetMinutesUntilArrivalAsync(int routeId, int stopId)
        {
            return await _yandexApiService.GetMinutesUntilArrivalAsync(routeId.ToString(), stopId.ToString());
        }

        public async Task<IEnumerable<TransportArrival>> GetArrivalsAsync(int stopId)
        {
            return await _yandexApiService.GetArrivalsAsync(stopId.ToString());
        }

        public async Task<bool> IsRouteActiveAsync(int routeId)
        {
            return await _yandexApiService.IsRouteActiveAsync(routeId.ToString(), "default");
        }

        public async Task<int> GetMinutesUntilArrivalAsync(string routeNumber, string stationCode)
        {
            return await _yandexApiService.GetMinutesUntilArrivalAsync(routeNumber, stationCode);
        }

        public async Task<IEnumerable<TransportArrival>> GetArrivalsAsync(string stationCode)
        {
            return await _yandexApiService.GetArrivalsAsync(stationCode);
        }

        public async Task<IEnumerable<ExternalStation>> GetNearbyStationsAsync(double latitude, double longitude, int distanceMeters = 1000)
        {
            return await _yandexApiService.GetNearbyStationsAsync(latitude, longitude, distanceMeters);
        }

        public async Task<IEnumerable<ExternalStation>> SearchStationsAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching stations for query: {Query}", query);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var geocodeResult = await _geocodingService.GeocodeAsync(query);
                    if (geocodeResult == null)
                    {
                        geocodeResult = await _geocodingService.GeocodeAsync($"Москва {query}");
                    }
                    
                    if (geocodeResult != null)
                    {
                        _logger.LogInformation("Geocoded '{Query}' => lat={Lat}, lon={Lon}", 
                            query, geocodeResult.Latitude, geocodeResult.Longitude);
                        var nearby = await GetNearbyStationsAsync(geocodeResult.Latitude, geocodeResult.Longitude, 5000);
                        if (nearby.Any()) 
                            return nearby;
                    }
                }

                _logger.LogWarning("No stations found for query: {Query}", query);
                return Enumerable.Empty<ExternalStation>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stations with query {Query}", query);
                return Enumerable.Empty<ExternalStation>();
            }
        }

        public async Task<(double lat, double lon)?> GeocodeAsync(string query)
        {
            var result = await _geocodingService.GeocodeAsync(query);
            return result != null ? (result.Latitude, result.Longitude) : null;
        }
    }
}
