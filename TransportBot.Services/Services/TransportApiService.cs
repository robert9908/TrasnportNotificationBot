using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransportBot.Core.Interfaces;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Net.Http;
using TransportBot.Core.DTOs;

namespace TransportBot.Services.Services
{
    public class TransportApiService : ITransportApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TransportApiService> _logger;
        private readonly string _apiKey;
        private readonly string _mosgorpassBaseUrl;

        public TransportApiService(HttpClient httpClient, ILogger<TransportApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["YandexSchedulesApiKey"] ?? "";
            _mosgorpassBaseUrl = configuration["Mosgorpass:BaseUrl"] ?? "https://api.mosgorpass.ru/";
            
            _httpClient.BaseAddress = new Uri("https://api.rasp.yandex.net/v3.0/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TransportNotificationBot/1.0");
        }

        public async Task<string> GetArrivalTimeAsync(string stopId, string routeId)
        {
            try
            {
                var url = $"schedule/?apikey={_apiKey}&format=json&station={Uri.EscapeDataString(stopId)}&transport_types=suburban,bus,trolleybus,tram&event=departure&lang=ru_RU";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var scheduleData = JsonSerializer.Deserialize<YandexScheduleBoardResponse>(
                        content, 
                        YandexScheduleBoardResponse.JsonOptions
                    );
                    
                    if (scheduleData?.Schedule?.Any() == true)
                    {
                        var nextDeparture = scheduleData.Schedule
                            .Where(s => s.Thread?.Number == routeId)
                            .Where(s => s.Departure.HasValue && s.Departure.Value > DateTime.Now)
                            .OrderBy(s => s.Departure)
                            .FirstOrDefault();
                            
                        if (nextDeparture != null)
                        {
                            var minutes = (int)Math.Ceiling((nextDeparture.Departure!.Value - DateTime.Now).TotalMinutes);
                            return minutes > 0 ? $"Прибытие через {minutes} мин" : "Прибывает сейчас";
                        }
                    }
                }
                return "Нет данных о прибытии транспорта";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting arrival time for stop {StopId}, route {RouteId}", stopId, routeId);
                return "Нет данных о прибытии";
            }
        }

        public async Task<int> GetMinutesUntilArrivalAsync(string routeNumber, string stationCode)
        {
            try
            {
                var basePart = $"schedule/?apikey={_apiKey}&format=json&station={Uri.EscapeDataString(stationCode)}&lang=ru_RU";
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
                if (stationCode.StartsWith("mgs:"))
                {
                    var arrivals = await TryGetMosgorpassArrivalsAsync(stationCode);
                    var match = arrivals.FirstOrDefault(a => string.Equals(a.RouteNumber, routeNumber, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        return match.MinutesUntilArrival;
                    return -1;
                }
                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minutes until arrival for route {RouteNumber}, station {StationCode}", routeNumber, stationCode);
                return -1;
            }
        }

        public async Task<int> GetMinutesUntilArrivalAsync(int routeId, int stopId)
        {
            try
            {
                var url = $"schedule/?apikey={_apiKey}&format=json&station={stopId}&transport_types=metro&event=departure&lang=ru_RU";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var scheduleData = JsonSerializer.Deserialize<YandexScheduleBoardResponse>(
                        content, 
                        YandexScheduleBoardResponse.JsonOptions
                    );
                    
                    if (scheduleData?.Schedule?.Any() == true)
                    {
                        var nextDeparture = scheduleData.Schedule
                            .Where(s => s.Thread?.Number == routeId.ToString())
                            .Where(s => s.Departure.HasValue && s.Departure.Value > DateTime.Now)
                            .OrderBy(s => s.Departure)
                            .FirstOrDefault();
                            
                        if (nextDeparture != null)
                        {
                            var minutes = (int)Math.Ceiling((nextDeparture.Departure!.Value - DateTime.Now).TotalMinutes);
                            return Math.Max(0, minutes);
                        }
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minutes until arrival for route {RouteId}, stop {StopId}", routeId, stopId);
                return -1;
            }
        }

        public async Task<IEnumerable<TransportArrival>> GetArrivalsAsync(string stationCode)
        {
            try
            {
                var basePart = $"schedule/?apikey={_apiKey}&format=json&station={Uri.EscapeDataString(stationCode)}&lang=ru_RU";
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

                return Enumerable.Empty<TransportArrival>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting arrivals for station {StationCode}", stationCode);
                return Enumerable.Empty<TransportArrival>();
            }
        }

        public async Task<IEnumerable<TransportArrival>> GetArrivalsAsync(int stopId)
        {
            return await GetArrivalsAsync(stopId.ToString());
        }

        public async Task<bool> IsRouteActiveAsync(int routeId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"search/?apikey={_apiKey}&format=json&from=c213&to=c213&transport_types=suburban,bus,trolleybus,tram&limit=50");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var scheduleData = JsonSerializer.Deserialize<YandexSearchResponse>(content);
                    
                    var hasActiveSegments = scheduleData?.Segments?.Any(s => 
                        (s.Thread?.Number == routeId.ToString()) &&
                        s.Departure > DateTime.Now && 
                        s.Departure < DateTime.Now.AddHours(2)) ?? false;
                        
                    return hasActiveSegments;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking route status for route {RouteId}", routeId);
                return false;
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

        public async Task<IEnumerable<ExternalStation>> GetNearbyStationsAsync(double latitude, double longitude, int distance = 1000)
        {
            try
            {
                var latStr = latitude.ToString(CultureInfo.InvariantCulture);
                var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
                var url = $"nearest_stations/?apikey={_apiKey}&format=json&lat={latStr}&lng={lonStr}&distance={Math.Max(distance, 2000)}";
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
                    }).ToList() ?? new List<ExternalStation>();
                    _logger.LogInformation("Nearest stations found: {Count}", mapped.Count);
                    if (mapped.Any()) return mapped;
                }

                return Enumerable.Empty<ExternalStation>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby stations for coordinates {Lat}, {Lng}", latitude, longitude);
                return Enumerable.Empty<ExternalStation>();
            }
        }

        public async Task<IEnumerable<ExternalStation>> SearchStationsAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching stations for query: {Query}", query);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    _logger.LogInformation("Starting geocoding for: {Query}", query);
                    var geo = await GeocodeAsync(query);
                    
                    if (!geo.HasValue)
                    {
                        _logger.LogInformation("First geocoding failed, trying with 'Москва {Query}'", query);
                        geo = await GeocodeAsync($"Москва {query}");
                    }
                    
                    if (!geo.HasValue)
                    {
                        _logger.LogInformation("Second geocoding failed, trying with 'станция {Query}'", query);
                        geo = await GeocodeAsync($"станция {query}");
                    }
                    
                    if (!geo.HasValue)
                    {
                        _logger.LogInformation("Third geocoding failed, trying with 'остановка {Query}'", query);
                        geo = await GeocodeAsync($"остановка {query}");
                    }
                    
                    if (geo.HasValue)
                    {
                        _logger.LogInformation("Geocode '{Query}' => lat={Lat}, lon={Lon}", query, geo.Value.lat, geo.Value.lon);
                        var nearby = await GetNearbyStationsAsync(geo.Value.lat, geo.Value.lon, 10000);
                        var nearbyList = nearby.ToList();
                        _logger.LogInformation("Found {Count} nearby stations", nearbyList.Count);
                        if (nearbyList.Any()) return nearbyList;
                    }
                    else
                    {
                        _logger.LogWarning("All geocoding attempts failed for query: {Query}", query);
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


        private async Task<List<ExternalStation>> TryGetMosgorpassNearbyAsync(double latitude, double longitude, int radiusMeters)
        {
            try
            {
                var latStr = latitude.ToString(CultureInfo.InvariantCulture);
                var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
                var urls = new[]
                {
                    $"{_mosgorpassBaseUrl.TrimEnd('/')}/v8.2/nearby?latitude={latStr}&longitude={lonStr}&radius={radiusMeters}&types=bus,trolleybus,tram",
                    $"{_mosgorpassBaseUrl.TrimEnd('/')}/widgets-api/nearby?latitude={latStr}&longitude={lonStr}&radius={radiusMeters}&types=bus,trolleybus,tram"
                };

                foreach (var url in urls)
                {
                    try
                    {
                        _logger.LogInformation("Mosgorpass nearby: {Url}", url);
                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.Add("User-Agent", "TransportNotificationBot/1.0");
                        var resp = await _httpClient.SendAsync(req);
                        if (!resp.IsSuccessStatusCode) continue;
                        var json = await resp.Content.ReadAsStringAsync();
                        var stations = ParseMosgorpassNearby(json);
                        if (stations.Any()) return stations;
                    }
                    catch (Exception exu)
                    {
                        _logger.LogWarning(exu, "Mosgorpass nearby attempt failed: {Url}", url);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mosgorpass nearby error");
            }
            return new List<ExternalStation>();
        }

        private List<ExternalStation> ParseMosgorpassNearby(string json)
        {
            var list = new List<ExternalStation>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in root.EnumerateArray())
                    {
                        var id = el.TryGetProperty("id", out var idEl) ? idEl.GetRawText().Trim('"') : null;
                        var name = el.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                        var lat = el.TryGetProperty("latitude", out var laEl) ? laEl.GetDouble() : (double?)null;
                        var lon = el.TryGetProperty("longitude", out var loEl) ? loEl.GetDouble() : (double?)null;
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name) && lat.HasValue && lon.HasValue)
                        {
                            list.Add(new ExternalStation
                            {
                                Code = $"mgs:{id.Trim('"')}",
                                Title = name!,
                                TransportType = "bus",
                                Latitude = lat.Value,
                                Longitude = lon.Value,
                                Distance = 0
                            });
                        }
                    }
                }
                else if (root.TryGetProperty("stops", out var stops) && stops.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in stops.EnumerateArray())
                    {
                        var id = el.TryGetProperty("id", out var idEl) ? idEl.GetRawText().Trim('"') : null;
                        var name = el.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                        var lat = el.TryGetProperty("lat", out var laEl) ? laEl.GetDouble() : (double?)null;
                        var lon = el.TryGetProperty("lon", out var loEl) ? loEl.GetDouble() : (double?)null;
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name) && lat.HasValue && lon.HasValue)
                        {
                            list.Add(new ExternalStation
                            {
                                Code = $"mgs:{id.Trim('"')}",
                                Title = name!,
                                TransportType = "bus",
                                Latitude = lat.Value,
                                Longitude = lon.Value,
                                Distance = 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Mosgorpass nearby response");
            }
            return list;
        }

        private async Task<List<TransportArrival>> TryGetMosgorpassArrivalsAsync(string stationCode)
        {
            var result = new List<TransportArrival>();
            try
            {
                if (!stationCode.StartsWith("mgs:")) return result;
                var id = stationCode.Substring(4);
                var urls = new[]
                {
                    $"{_mosgorpassBaseUrl.TrimEnd('/')}/v8.2/stop?id={Uri.EscapeDataString(id)}",
                    $"{_mosgorpassBaseUrl.TrimEnd('/')}/widgets-api/stop?id={Uri.EscapeDataString(id)}"
                };
                foreach (var url in urls)
                {
                    try
                    {
                        _logger.LogInformation("Mosgorpass stop arrivals: {Url}", url);
                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.Add("User-Agent", "TransportNotificationBot/1.0");
                        var resp = await _httpClient.SendAsync(req);
                        if (!resp.IsSuccessStatusCode) continue;
                        var json = await resp.Content.ReadAsStringAsync();
                        var list = ParseMosgorpassArrivals(json);
                        if (list.Any()) return list;
                    }
                    catch (Exception exu)
                    {
                        _logger.LogWarning(exu, "Mosgorpass arrivals attempt failed: {Url}", url);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mosgorpass arrivals error");
            }
            return result;
        }

        private List<TransportArrival> ParseMosgorpassArrivals(string json)
        {
            var list = new List<TransportArrival>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("routes", out var routes) && routes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in routes.EnumerateArray())
                    {
                        var number = r.TryGetProperty("number", out var nEl) ? nEl.GetString() : null;
                        if (string.IsNullOrWhiteSpace(number)) continue;
                        DateTime? nearest = null;
                        if (r.TryGetProperty("arrivals", out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var a in arr.EnumerateArray())
                            {
                                if (a.ValueKind == JsonValueKind.String && DateTime.TryParse(a.GetString(), out var dt))
                                {
                                    if (!nearest.HasValue || dt < nearest) nearest = dt;
                                }
                                else if (a.TryGetProperty("time", out var tEl) && DateTime.TryParse(tEl.GetString(), out var dt2))
                                {
                                    if (!nearest.HasValue || dt2 < nearest) nearest = dt2;
                                }
                            }
                        }
                        else if (r.TryGetProperty("times", out var times) && times.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var t in times.EnumerateArray())
                            {
                                if (t.ValueKind == JsonValueKind.String && DateTime.TryParse(t.GetString(), out var dt))
                                {
                                    if (!nearest.HasValue || dt < nearest) nearest = dt;
                                }
                            }
                        }

                        if (nearest.HasValue)
                        {
                            var minutes = (int)Math.Ceiling((nearest.Value.ToLocalTime() - DateTime.Now).TotalMinutes);
                            list.Add(new TransportArrival
                            {
                                RouteId = number!.GetHashCode(),
                                RouteName = $"Автобус №{number}",
                                RouteType = "bus",
                                RouteNumber = number,
                                MinutesUntilArrival = Math.Max(0, minutes),
                                EstimatedArrival = DateTime.Now.AddMinutes(Math.Max(0, minutes))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Mosgorpass arrivals response");
            }
            return list.OrderBy(a => a.MinutesUntilArrival).ToList();
        }

        public async Task<(double lat, double lon)?> GeocodeAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query)) return null;

                var result = await TryGeocodeWithYandex(query);
                if (result.HasValue) return result;

                var alternativeQueries = new[]
                {
                    $"{query}, Москва",
                    $"станция {query}",
                    $"метро {query}",
                    query.Replace("станция", "").Replace("метро", "").Trim()
                };

                foreach (var altQuery in alternativeQueries)
                {
                    if (altQuery != query)
                    {
                        result = await TryGeocodeWithYandex(altQuery);
                        if (result.HasValue) return result;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geocoding request failed for query: {Query}", query);
                return null;
            }
        }

        private async Task<(double lat, double lon)?> TryGeocodeWithYandex(string query)
        {
            try
            {
                await Task.Delay(500);

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var url = $"https://geocode-maps.yandex.ru/1.x/?apikey={_apiKey}&geocode={Uri.EscapeDataString(query)}&format=json&results=1&lang=ru_RU";
                
                _logger.LogInformation("Yandex geocoding query: {Query}", query);
                var resp = await httpClient.GetAsync(url);

                if (!resp.IsSuccessStatusCode) 
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Yandex geocoding request failed: {StatusCode}, Response: {Response}", resp.StatusCode, errorContent);
                    return null;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<YandexGeocoderResponse>(json);
                
                var geoObject = response?.Response?.GeoObjectCollection?.FeatureMember?.FirstOrDefault()?.GeoObject;
                if (geoObject?.Point?.Pos == null)
                {
                    _logger.LogInformation("No Yandex geocoding results for query: {Query}", query);
                    return null;
                }

                var coords = geoObject.Point.Pos.Split(' ');
                if (coords.Length == 2 && 
                    double.TryParse(coords[0], System.Globalization.CultureInfo.InvariantCulture, out var lon) &&
                    double.TryParse(coords[1], System.Globalization.CultureInfo.InvariantCulture, out var lat))
                {
                    _logger.LogInformation("Yandex geocoded '{Query}' to lat={Lat}, lon={Lon}", query, lat, lon);
                    return (lat, lon);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yandex geocoding failed for query: {Query}", query);
                return null;
            }
        }

        private async Task<(double lat, double lon)?> TryGeocodeWithNominatim(string query)
        {
            try
            {
                await Task.Delay(1500);

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(query)}&limit=1&addressdetails=1&countrycodes=ru";
                
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                
                req.Headers.Add("User-Agent", "TransportNotificationBot/1.0 (https://github.com/transport-bot; contact@transport-bot.ru)");
                req.Headers.Add("Accept", "application/json");
                req.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.8");
                req.Headers.Add("Referer", "https://transport-bot.ru");

                _logger.LogInformation("Geocoding query: {Query}", query);
                var resp = await httpClient.SendAsync(req);

                if (!resp.IsSuccessStatusCode) 
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Geocoding request failed: {StatusCode}, Response: {Response}", resp.StatusCode, errorContent);
                    return null;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<NominatimItem>>(json);
                var first = items?.FirstOrDefault();
                if (first == null) 
                {
                    _logger.LogInformation("No geocoding results for query: {Query}", query);
                    return null;
                }

                if (double.TryParse(first.lat, System.Globalization.CultureInfo.InvariantCulture, out var la)
                    && double.TryParse(first.lon, System.Globalization.CultureInfo.InvariantCulture, out var lo))
                {
                    _logger.LogInformation("Geocoded '{Query}' to lat={Lat}, lon={Lon}", query, la, lo);
                    return (la, lo);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nominatim geocoding failed for query: {Query}", query);
                return null;
            }
        }

        private class NominatimItem
        {
            public string lat { get; set; } = string.Empty;
            public string lon { get; set; } = string.Empty;
        }

        private class YandexGeocoderResponse
        {
            [JsonPropertyName("response")]
            public YandexGeocoderResponseData? Response { get; set; }
        }

        private class YandexGeocoderResponseData
        {
            [JsonPropertyName("GeoObjectCollection")]
            public YandexGeoObjectCollection? GeoObjectCollection { get; set; }
        }

        private class YandexGeoObjectCollection
        {
            [JsonPropertyName("featureMember")]
            public List<YandexFeatureMember>? FeatureMember { get; set; }
        }

        private class YandexFeatureMember
        {
            [JsonPropertyName("GeoObject")]
            public YandexGeoObject? GeoObject { get; set; }
        }

        private class YandexGeoObject
        {
            [JsonPropertyName("Point")]
            public YandexPoint? Point { get; set; }
        }

        private class YandexPoint
        {
            [JsonPropertyName("pos")]
            public string? Pos { get; set; }
        }
    }

    public class YandexScheduleBoardResponse
    {
        [JsonPropertyName("schedule")]
        public List<YandexBoardItem>? Schedule { get; set; }
        
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public class YandexBoardItem
    {
        [JsonPropertyName("departure")]
        [JsonConverter(typeof(JsonDateTimeOffsetConverter))]
        public DateTime? Departure { get; set; }
        
        [JsonPropertyName("thread")]
        public YandexThread? Thread { get; set; }
    }

    public class YandexSearchResponse
    {
        [JsonPropertyName("segments")]
        public List<YandexSearchSegment>? Segments { get; set; }
    }

    public class YandexSearchSegment
    {
        [JsonPropertyName("departure")]
        [JsonConverter(typeof(JsonDateTimeOffsetConverter))]
        public DateTime Departure { get; set; }
        
        [JsonPropertyName("thread")]
        public YandexThread? Thread { get; set; }
    }
    
    public class JsonDateTimeOffsetConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
                
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString))
                    return null;
                    
                if (DateTimeOffset.TryParse(dateString, out var dateTimeOffset))
                    return dateTimeOffset.LocalDateTime;
                    
                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
                    return dateTime;
            }
            
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    public class YandexThread
    {
        [JsonPropertyName("uid")]
        public string? Uid { get; set; }
        
        [JsonPropertyName("number")]
        public string? Number { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("transport_type")]
        public string? TransportType { get; set; }
    }

    public class YandexStationsResponse
    {
        [JsonPropertyName("stations")]
        public List<YandexStation>? Stations { get; set; }
    }

    public class YandexStation
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("station_type")]
        public string? StationType { get; set; }
        
        [JsonPropertyName("transport_type")]
        public string? TransportType { get; set; }
        
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        
        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }
}
