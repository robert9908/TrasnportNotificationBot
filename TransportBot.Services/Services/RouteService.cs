using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;

namespace TransportBot.Services.Services
{
    public class RouteService : IRouteService
    {
        private readonly IRouteRepository _repository;

        public RouteService(IRouteRepository repository)
        {
            _repository = repository;
        }

        public async Task<Route?> GetRouteAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Route>> GetAllRoutesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<Route>> GetRoutesByStopAsync(int stopId)
        {
            return await _repository.GetByStopIdAsync(stopId);
        }

        public async Task<Route> CreateRouteAsync(Route route)
        {
            route.CreatedAt = DateTime.UtcNow;
            await _repository.AddAsync(route);
            return route;
        }

        public async Task<Route?> UpdateRouteAsync(Route route)
        {
            var existing = await _repository.GetByIdAsync(route.Id);
            if (existing == null)
                return null;

            await _repository.UpdateAsync(route);
            return route;
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            var route = await _repository.GetByIdAsync(id);
            if (route == null)
                return false;

            await _repository.DeleteAsync(id);
            return true;
        }
    }
}
