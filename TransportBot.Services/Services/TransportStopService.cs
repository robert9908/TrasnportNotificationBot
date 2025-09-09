using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;

namespace TransportBot.Services.Services
{
    public class TransportStopService : ITransportStopService
    {
        private readonly ITransportStopRepository _repository;
        private readonly ITransportApiService _transportApiService;

        public TransportStopService(ITransportStopRepository repository, ITransportApiService transportApiService)
        {
            _repository = repository;
            _transportApiService = transportApiService;
        }

        public async Task<TransportStop?> GetStopAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TransportStop>> GetStopsAsync(string? city = null)
        {
            return await _repository.GetAllAsync(city);
        }

        public async Task<IEnumerable<TransportStop>> GetNearbyStopsAsync(double latitude, double longitude, double radiusKm = 1.0)
        {
            return await _repository.GetNearbyStopsAsync(latitude, longitude, radiusKm);
        }

        public async Task<TransportStop> CreateStopAsync(TransportStop stop)
        {
            stop.CreatedAt = DateTime.UtcNow;
            await _repository.AddAsync(stop);
            return stop;
        }

        public async Task<TransportStop?> UpdateStopAsync(TransportStop stop)
        {
            var existing = await _repository.GetByIdAsync(stop.Id);
            if (existing == null)
                return null;

            await _repository.UpdateAsync(stop);
            return stop;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var stop = await _repository.GetByIdAsync(id);
            if (stop == null)
                return false;

            await _repository.DeleteAsync(id);
            return true;
        }
    }
}
