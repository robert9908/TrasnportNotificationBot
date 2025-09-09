using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransportBot.Core.Entities;

namespace TransportBot.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransportDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TransportDbContext>>();

            try
            {
                if (await context.Stops.AnyAsync())
                {
                    logger.LogInformation("Database already contains data, skipping seed");
                    return;
                }

                logger.LogInformation("Seeding database with test data...");

                var stops = new List<TransportStop>
                {
                    new() { ExternalId = "stop_001", Name = "Красная площадь", Latitude = 55.7539, Longitude = 37.6208, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_002", Name = "Театральная площадь", Latitude = 55.7587, Longitude = 37.6176, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_003", Name = "Манежная площадь", Latitude = 55.7558, Longitude = 37.6173, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_004", Name = "Тверская улица", Latitude = 55.7558, Longitude = 37.6176, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_005", Name = "Арбатская площадь", Latitude = 55.7520, Longitude = 37.6037, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_006", Name = "Парк Горького", Latitude = 55.7357, Longitude = 37.5947, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_007", Name = "Воробьевы горы", Latitude = 55.7096, Longitude = 37.5425, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_008", Name = "МГУ", Latitude = 55.7033, Longitude = 37.5300, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_009", Name = "Лужники", Latitude = 55.7155, Longitude = 37.5550, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_010", Name = "Новодевичий монастырь", Latitude = 55.7267, Longitude = 37.5560, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_011", Name = "Кремль", Latitude = 55.7520, Longitude = 37.6175, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_012", Name = "ГУМ", Latitude = 55.7545, Longitude = 37.6211, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_013", Name = "Большой театр", Latitude = 55.7596, Longitude = 37.6189, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_014", Name = "Третьяковская галерея", Latitude = 55.7414, Longitude = 37.6207, CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "stop_015", Name = "Храм Христа Спасителя", Latitude = 55.7446, Longitude = 37.6057, CreatedAt = DateTime.UtcNow }
                };

                context.Stops.AddRange(stops);
                await context.SaveChangesAsync();

                var routes = new List<Route>
                {
                    new() { ExternalId = "route_001", Name = "Автобус №1", Type = "bus", CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "route_002", Name = "Автобус №15", Type = "bus", CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "route_003", Name = "Троллейбус №2", Type = "trolleybus", CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "route_004", Name = "Трамвай №39", Type = "tram", CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "route_005", Name = "Маршрутка №567м", Type = "minibus", CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "route_006", Name = "Автобус №101", Type = "bus", CreatedAt = DateTime.UtcNow },
                    new() { ExternalId = "route_007", Name = "Троллейбус №33", Type = "trolleybus", CreatedAt = DateTime.UtcNow }
                };

                context.Routes.AddRange(routes);
                await context.SaveChangesAsync();

                var routeStops = new List<RouteStop>
                {
                    new() { RouteId = 1, StopId = 1, Sequence = 1 },
                    new() { RouteId = 1, StopId = 2, Sequence = 2 },
                    new() { RouteId = 1, StopId = 3, Sequence = 3 },
                    new() { RouteId = 1, StopId = 4, Sequence = 4 },
                    new() { RouteId = 1, StopId = 5, Sequence = 5 },

                    new() { RouteId = 2, StopId = 6, Sequence = 1 },
                    new() { RouteId = 2, StopId = 7, Sequence = 2 },
                    new() { RouteId = 2, StopId = 8, Sequence = 3 },
                    new() { RouteId = 2, StopId = 9, Sequence = 4 },

                    new() { RouteId = 3, StopId = 11, Sequence = 1 },
                    new() { RouteId = 3, StopId = 12, Sequence = 2 },
                    new() { RouteId = 3, StopId = 13, Sequence = 3 },
                    new() { RouteId = 3, StopId = 14, Sequence = 4 },
                    new() { RouteId = 3, StopId = 15, Sequence = 5 },

                    new() { RouteId = 4, StopId = 1, Sequence = 1 },
                    new() { RouteId = 4, StopId = 11, Sequence = 2 },
                    new() { RouteId = 4, StopId = 15, Sequence = 3 },
                    new() { RouteId = 4, StopId = 6, Sequence = 4 },

                    new() { RouteId = 5, StopId = 5, Sequence = 1 },
                    new() { RouteId = 5, StopId = 10, Sequence = 2 },
                    new() { RouteId = 5, StopId = 9, Sequence = 3 },
                    new() { RouteId = 5, StopId = 7, Sequence = 4 },

                    new() { RouteId = 6, StopId = 2, Sequence = 1 },
                    new() { RouteId = 6, StopId = 4, Sequence = 2 },
                    new() { RouteId = 6, StopId = 5, Sequence = 3 },
                    new() { RouteId = 6, StopId = 15, Sequence = 4 },
                    new() { RouteId = 6, StopId = 6, Sequence = 5 },

                    new() { RouteId = 7, StopId = 8, Sequence = 1 },
                    new() { RouteId = 7, StopId = 7, Sequence = 2 },
                    new() { RouteId = 7, StopId = 9, Sequence = 3 },
                    new() { RouteId = 7, StopId = 10, Sequence = 4 },
                    new() { RouteId = 7, StopId = 15, Sequence = 5 },
                    new() { RouteId = 7, StopId = 14, Sequence = 6 }
                };

                context.RouteStops.AddRange(routeStops);
                await context.SaveChangesAsync();

                logger.LogInformation("Database seeded successfully with {StopsCount} stops, {RoutesCount} routes, and {RouteStopsCount} route-stop connections", 
                    stops.Count, routes.Count, routeStops.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding database");
                throw;
            }
        }
    }
}
