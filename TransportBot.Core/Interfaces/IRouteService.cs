using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface IRouteService
    {
        Task<Route?> GetRouteAsync(int id);
        Task<IEnumerable<Route>> GetAllRoutesAsync();
        Task<IEnumerable<Route>> GetRoutesByStopAsync(int stopId);
        Task<Route> CreateRouteAsync(Route route);
        Task<Route?> UpdateRouteAsync(Route route);
        Task<bool> DeleteRouteAsync(int id);
    }
}
