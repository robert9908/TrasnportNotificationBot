using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace TransportBot.WebApi.Middleware
{
    public class SimpleRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<string, DateTime> _requests = new();
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
        private readonly int _maxRequests = 100;

        public SimpleRateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var now = DateTime.UtcNow;

            var expiredKeys = _requests.Where(kvp => now - kvp.Value > _timeWindow).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _requests.TryRemove(key, out _);
            }

            var clientRequests = _requests.Count(kvp => kvp.Key.StartsWith(clientId));

            if (clientRequests >= _maxRequests)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            _requests.TryAdd($"{clientId}_{Guid.NewGuid()}", now);

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddSimpleRateLimiting(this IServiceCollection services)
        {
            return services;
        }

        public static IApplicationBuilder UseSimpleRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SimpleRateLimitingMiddleware>();
        }
    }
}
