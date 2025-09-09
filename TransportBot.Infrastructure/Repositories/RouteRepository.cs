using Microsoft.EntityFrameworkCore;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using TransportBot.Infrastructure.Data;

namespace TransportBot.Infrastructure.Repositories
{
    public class RouteRepository : IRouteRepository
    {
        private readonly TransportDbContext _context;

        public RouteRepository(TransportDbContext context)
        {
            _context = context;
        }

        public async Task<Route?> GetByIdAsync(int id)
        {
            return await _context.Routes.FindAsync(id);
        }

        public async Task<IEnumerable<Route>> GetAllAsync()
        {
            return await _context.Routes.ToListAsync();
        }

        public async Task<IEnumerable<Route>> GetByStopIdAsync(int stopId)
        {
            return await _context.RouteStops
                .Where(rs => rs.StopId == stopId)
                .Include(rs => rs.Route)
                .Select(rs => rs.Route)
                .ToListAsync();
        }

        public async Task AddAsync(Route route)
        {
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Route route)
        {
            _context.Routes.Update(route);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route != null)
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
            }
        }
    }
}
