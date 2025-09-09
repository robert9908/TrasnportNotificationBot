using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface ITransportStopRepository
    {
        Task<TransportStop?> GetByIdAsync(int id);
        Task<IEnumerable<TransportStop>> GetAllAsync(string? city = null);
        Task<IEnumerable<TransportStop>> GetNearbyStopsAsync(double latitude, double longitude, double radiusKm = 1.0);
        Task AddAsync(TransportStop stop);
        Task UpdateAsync(TransportStop stop);
        Task DeleteAsync(int id); 
    }
}
