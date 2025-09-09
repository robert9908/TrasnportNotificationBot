using Microsoft.AspNetCore.Mvc;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;

namespace TransportBot.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransportStopController : ControllerBase
    {
        private readonly ITransportStopService _transportStopService;

        public TransportStopController(ITransportStopService transportStopService)
        {
            _transportStopService = transportStopService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransportStop>>> GetStops([FromQuery] string? city = null)
        {
            var stops = await _transportStopService.GetStopsAsync(city);
            return Ok(stops);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransportStop>> GetStop(int id)
        {
            var stop = await _transportStopService.GetStopAsync(id);
            if (stop == null) return NotFound();
            return Ok(stop);
        }

        [HttpGet("nearby")]
        public async Task<ActionResult<IEnumerable<TransportStop>>> GetNearbyStops(
            [FromQuery] double latitude, 
            [FromQuery] double longitude, 
            [FromQuery] double radiusKm = 1.0)
        {
            var stops = await _transportStopService.GetNearbyStopsAsync(latitude, longitude, radiusKm);
            return Ok(stops);
        }

        [HttpPost]
        public async Task<ActionResult<TransportStop>> CreateStop([FromBody] CreateStopRequest request)
        {
            var stop = new TransportStop
            {
                ExternalId = request.ExternalId,
                Name = request.Name,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            var createdStop = await _transportStopService.CreateStopAsync(stop);
            return CreatedAtAction(nameof(GetStop), new { id = createdStop.Id }, createdStop);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TransportStop>> UpdateStop(int id, [FromBody] UpdateStopRequest request)
        {
            var stop = new TransportStop
            {
                Id = id,
                ExternalId = request.ExternalId,
                Name = request.Name,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            var updatedStop = await _transportStopService.UpdateStopAsync(stop);
            if (updatedStop == null) return NotFound();
            return Ok(updatedStop);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteStop(int id)
        {
            var result = await _transportStopService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }

    public record CreateStopRequest(string ExternalId, string Name, double Latitude, double Longitude);
    public record UpdateStopRequest(string ExternalId, string Name, double Latitude, double Longitude);
}
