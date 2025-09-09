using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Globalization;
using TransportBot.Core.Interfaces;
using TransportBot.Core.DTOs;
using TransportBot.Core.Configuration;

namespace TransportBot.Services.Services
{
    public class YandexApiService : IYandexApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YandexApiService> _logger;
        private readonly YandexApiConfiguration _config;

        public YandexApiService(
            HttpClient httpClient, 
            ILogger<YandexApiService> logger, 
            IOptions<YandexApiConfiguration> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;
            
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        }

        public async Task<IEnumerable<TransportArrival>> GetArrivalsAsync(string stationCode)
        {
            try
            {
                var basePart = $"schedule/?apikey={_config.ApiKey}&format=json&station={Uri.EscapeDataString(stationCode)}&lang=ru_RU";
                var urls = new List<string>
                {
                    basePart + "&transport_types=suburban&event=departure",
                    basePart + "&event=departure",
                    basePart
                };

                foreach (var url in urls)
                {
                    _logger.LogInformation("Trying URL: {Url}", url);
                    var response = await _httpClient.GetAsync(url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Request failed with status {StatusCode}. URL: {Url}\nResponse: {Response}", 
                            response.StatusCode, url, errorContent);
                        continue;
                    }
                    
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Response from {Url}: {Content}", url, content);
                    
                    try
                    {
                        var scheduleData = JsonSerializer.Deserialize<YandexScheduleBoardResponse>(
                            content, 
                            YandexScheduleBoardResponse.JsonOptions
                        );
                        
                        if (scheduleData?.Schedule?.Any() != true)
                        {
                            _logger.LogInformation("No schedule data in response from {Url}. Raw: {Raw}", 
                                url, content.Length > 500 ? content.Substring(0,500) + "..." : content);
                            continue;
                        }

                        var arrivals = scheduleData.Schedule
                            .Where(s => s.Departure.HasValue && s.Departure.Value > DateTime.Now)
                            .OrderBy(s => s.Departure)
                            .Take(10)
                            .Select(s => new TransportArrival
                            {
                                RouteId = s.Thread?.Uid?.GetHashCode() ?? 0,
                                RouteName = s.Thread?.Title ?? s.Thread?.Number ?? "Маршрут",
                                RouteType = GetTransportTypeName(s.Thread?.TransportType),
                                RouteNumber = s.Thread?.Number,
                                MinutesUntilArrival = (int)Math.Ceiling(((s.Departure!.Value) - DateTime.Now).TotalMinutes),
                                EstimatedArrival = s.Departure!.Value
                            });
                        return arrivals.ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing response from {Url}", url);
                        continue;
                    }
                }
                
                _logger.LogWarning("All URL attempts failed for station {StationCode}", stationCode);
                return Enumerable.Empty<TransportArrival>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting arrivals for station {StationCode}", stationCode);
                return Enumerable.Empty<TransportArrival>();
            }
        }

        public async Task<int> GetMinutesUntilArrivalAsync(string routeNumber, string stationCode)
        {
            try
            {
                var basePart = $"schedule/?apikey={_config.ApiKey}&format=json&station={Uri.EscapeDataString(stationCode)}&lang=ru_RU";
                var urls = new List<string>
                {
                    basePart + "&transport_types=suburban&event=departure",
                    basePart + "&event=departure",
                    basePart
                };

                foreach (var url in urls)
                {
                    _logger.LogInformation("Trying URL: {Url}", url);
                    var response = await _httpClient.GetAsync(url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Request failed with status {StatusCode}. URL: {Url}\nResponse: {Response}", 
                            response.StatusCode, url, errorContent);
                        continue;
                    }
                    
                    var content = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        var scheduleData = JsonSerializer.Deserialize<YandexScheduleBoardResponse>(
                            content, 
                            YandexScheduleBoardResponse.JsonOptions
                        );
                        
                        if (scheduleData?.Schedule?.Any() != true)
                        {
                            _logger.LogInformation("No schedule data in response from {Url}. Raw: {Raw}", 
                                url, content.Length > 500 ? content.Substring(0,500) + "..." : content);
                            continue;
                        }

                        var nextDeparture = scheduleData.Schedule
                            .Where(s => s.Thread?.Number == routeNumber)
                            .Where(s => s.Departure.HasValue && s.Departure.Value > DateTime.Now)
                            .OrderBy(s => s.Departure)
                            .FirstOrDefault();
                        if (nextDeparture != null)
                        {
                            var minutes = (int)Math.Ceiling((nextDeparture.Departure!.Value - DateTime.Now).TotalMinutes);
                            return Math.Max(0, minutes);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing response from {Url}", url);
                        continue;
                    }
                }
                
                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minutes until arrival for route {RouteNumber}, station {StationCode}", routeNumber, stationCode);
                return -1;
            }
        }

        public async Task<bool> IsRouteActiveAsync(string routeNumber, string stationCode)
        {
            try
            {
                var arrivals = await GetArrivalsAsync(stationCode);
                return arrivals.Any(a => a.RouteNumber == routeNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking route status for route {RouteNumber}, station {StationCode}", routeNumber, stationCode);
                return false;
            }
        }

        public async Task<IEnumerable<ExternalStation>> GetNearbyStationsAsync(double latitude, double longitude, int distanceMeters = 1000)
        {
            try
            {
                var latStr = latitude.ToString(CultureInfo.InvariantCulture);
                var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
                var url = $"nearest_stations/?apikey={_config.ApiKey}&format=json&lat={latStr}&lng={lonStr}&distance={Math.Max(distanceMeters, 2000)}";
                
                _logger.LogInformation("Nearest stations query: {Url}", url);
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var stationsData = JsonSerializer.Deserialize<YandexStationsResponse>(content);
                    var mapped = stationsData?.Stations?.Select(s => new ExternalStation
                    {
                        Code = s.Code ?? string.Empty,
                        Title = s.Title ?? string.Empty,
                        TransportType = s.TransportType,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        Distance = s.Distance
                    })?.ToList() ?? new List<ExternalStation>();
                    
                    _logger.LogInformation("Found {Count} nearby stations", mapped.Count);
                    return mapped;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Nearby stations request failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                }
                
                return Enumerable.Empty<ExternalStation>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby stations for coordinates {Lat}, {Lng}", latitude, longitude);
                return Enumerable.Empty<ExternalStation>();
            }
        }

        private string GetTransportTypeName(string? transportType)
        {
            return transportType?.ToLower() switch
            {
                "bus" => "Автобус",
                "trolleybus" => "Троллейбус", 
                "tram" => "Трамвай",
                "suburban" => "Электричка",
                "metro" => "Метро",
                _ => "Транспорт"
            };
        }
    }
}
