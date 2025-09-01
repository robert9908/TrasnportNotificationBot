using Microsoft.EntityFrameworkCore;
using TransportBot.Core.Entities;

namespace TransportBot.Infrastructure.Data
{
    public class TransportDbContext : DbContext
    {   
        public TransportDbContext(DbContextOptions<TransportDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<TransportStop> Stops { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }  
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<TransportStatus> Statuses { get; set; }

    }
}
