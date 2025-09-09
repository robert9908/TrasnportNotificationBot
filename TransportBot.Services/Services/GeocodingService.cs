using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransportBot.Core.Interfaces;
using TransportBot.Core.DTOs;
using TransportBot.Core.Configuration;

namespace TransportBot.Services.Services
{
    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeocodingService> _logger;
        private readonly GeocodingConfiguration _config;

        public GeocodingService(
            HttpClient httpClient, 
            ILogger<GeocodingService> logger, 
            IOptions<GeocodingConfiguration> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;
            
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        }

        public async Task<GeocodeResult?> GeocodeAsync(string query)
        {
            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"search?format=json&q={encodedQuery}&limit={_config.ResultLimit}";
                
                _logger.LogInformation("Geocoding query: {Query}", query);
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Geocoding request failed: {StatusCode}", response.StatusCode);
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<NominatimResult>>(content);
                
                var firstResult = results?.FirstOrDefault();
                if (firstResult == null)
                {
                    _logger.LogInformation("No geocoding results for query: {Query}", query);
                    return null;
                }
                
                if (!double.TryParse(firstResult.Lat, out var lat) || 
                    !double.TryParse(firstResult.Lon, out var lon))
                {
                    _logger.LogWarning("Invalid coordinates in geocoding result for query: {Query}", query);
                    return null;
                }
                
                return new GeocodeResult
                {
                    Latitude = lat,
                    Longitude = lon,
                    DisplayName = firstResult.DisplayName ?? query
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding query: {Query}", query);
                return null;
            }
        }
    }

    internal class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;
        
        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
        
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
