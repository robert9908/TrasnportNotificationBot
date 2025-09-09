using System.Text.Json.Serialization;

namespace TransportBot.Core.DTOs
{
    public class TransportArrival
    {
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string RouteType { get; set; } = string.Empty;
        public string? RouteNumber { get; set; }
        public int MinutesUntilArrival { get; set; }
        public DateTime EstimatedArrival { get; set; }
    }

    public class ExternalStation
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? TransportType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }
    }

    public class GeocodeResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
