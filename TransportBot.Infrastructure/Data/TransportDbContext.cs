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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Stop)
                .WithMany()
                .HasForeignKey(s => s.StopId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Route)
                .WithMany()
                .HasForeignKey(s => s.RouteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Route)
                .WithMany()
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Stop)
                .WithMany()
                .HasForeignKey(rs => rs.StopId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransportStatus>()
                .HasOne(ts => ts.Route)
                .WithMany()
                .HasForeignKey(ts => ts.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransportStatus>()
                .HasOne(ts => ts.Stop)
                .WithMany()
                .HasForeignKey(ts => ts.StopId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.TelegramId)
                .IsUnique();

            modelBuilder.Entity<TransportStop>()
                .HasIndex(s => s.ExternalId);

            modelBuilder.Entity<Route>()
                .HasIndex(r => r.ExternalId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
