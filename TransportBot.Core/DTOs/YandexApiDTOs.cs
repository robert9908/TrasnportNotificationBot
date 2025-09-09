using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace TransportBot.Core.DTOs
{
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
}
