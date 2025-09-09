using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface IRouteRepository
    {
        Task<Route?> GetByIdAsync(int id);
        Task<IEnumerable<Route>> GetAllAsync();
        Task<IEnumerable<Route>> GetByStopIdAsync(int stopId);
        Task AddAsync(Route route);
        Task UpdateAsync(Route route);
        Task DeleteAsync(int id);
    }
}
