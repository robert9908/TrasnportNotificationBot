namespace TransportBot.Core.Configuration
{
    public class YandexApiConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.rasp.yandex.net/v3.0/";
        public string UserAgent { get; set; } = "TransportNotificationBot/1.0";
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class GeocodingConfiguration
    {
        public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";
        public int TimeoutSeconds { get; set; } = 10;
        public int ResultLimit { get; set; } = 1;
    }

    public class MosgorpassConfiguration
    {
        public string BaseUrl { get; set; } = "https://api.mosgorpass.ru/";
        public int TimeoutSeconds { get; set; } = 15;
        public bool IsEnabled { get; set; } = false;
    }
}
