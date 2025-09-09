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
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database initialization completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing database");
                throw;
            }
        }
    }
}
