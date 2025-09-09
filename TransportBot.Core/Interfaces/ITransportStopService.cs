using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface ITransportStopService
    {
        Task<TransportStop?> GetStopAsync(int id);
        Task<IEnumerable<TransportStop>> GetStopsAsync(string? city = null);
        Task<IEnumerable<TransportStop>> GetNearbyStopsAsync(double latitude, double longitude, double radiusKm = 1.0);
        Task<TransportStop> CreateStopAsync (TransportStop stop);
        Task<TransportStop?> UpdateStopAsync (TransportStop stop);  
        Task<bool> DeleteAsync(int id);
    }
}
