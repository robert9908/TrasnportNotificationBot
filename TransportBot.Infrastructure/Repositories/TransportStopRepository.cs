using Microsoft.EntityFrameworkCore;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using TransportBot.Infrastructure.Data;

namespace TransportBot.Infrastructure.Repositories
{
    public class TransportStopRepository : ITransportStopRepository
    {
        private readonly TransportDbContext _context;

        public TransportStopRepository(TransportDbContext context)
        {
            _context = context;
        }

        public async Task<TransportStop?> GetByIdAsync(int id)
        {
            return await _context.Stops.FindAsync(id);
        }

        public async Task<IEnumerable<TransportStop>> GetAllAsync(string? city = null)
        {
            var query = _context.Stops.AsQueryable();
            
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(s => s.Name.Contains(city));
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<TransportStop>> GetNearbyStopsAsync(double latitude, double longitude, double radiusKm = 1.0)
        {
            var stops = await _context.Stops.ToListAsync();
            
            return stops.Where(stop => 
                CalculateDistance(latitude, longitude, stop.Latitude, stop.Longitude) <= radiusKm
            ).OrderBy(stop => 
                CalculateDistance(latitude, longitude, stop.Latitude, stop.Longitude)
            );
        }

        public async Task AddAsync(TransportStop stop)
        {
            _context.Stops.Add(stop);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TransportStop stop)
        {
            _context.Stops.Update(stop);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var stop = await _context.Stops.FindAsync(id);
            if (stop != null)
            {
                _context.Stops.Remove(stop);
                await _context.SaveChangesAsync();
            }
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371;
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadius * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
