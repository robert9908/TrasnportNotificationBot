using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportBot.Infrastructure.Data;

namespace TransportBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly TransportDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(TransportDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                
                var healthStatus = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Database = "Connected",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                
                var healthStatus = new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message,
                    Database = "Disconnected"
                };

                return StatusCode(503, healthStatus);
            }
        }
    }
}
