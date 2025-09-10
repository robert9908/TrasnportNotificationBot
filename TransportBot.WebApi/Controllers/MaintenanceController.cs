using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportBot.Infrastructure.Data;

namespace TransportBot.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly TransportDbContext _context;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(TransportDbContext context, ILogger<MaintenanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("cleanup-invalid-stations")]
        public async Task<IActionResult> CleanupInvalidStations()
        {
            try
            {
                var invalidStationCodes = new[] { "1", "12", "14" };
                
                var invalidSubscriptions = await _context.Subscriptions
                    .Include(s => s.User)
                    .Where(s => invalidStationCodes.Contains(s.ExternalStopCode) && s.IsActive)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} invalid subscriptions to cleanup", invalidSubscriptions.Count);

                foreach (var subscription in invalidSubscriptions)
                {
                    _logger.LogInformation("Deactivating subscription {Id} with invalid station code {Code} for user {UserId}", 
                        subscription.Id, subscription.ExternalStopCode, subscription.User?.TelegramId);
                    subscription.IsActive = false;
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = $"Cleaned up {invalidSubscriptions.Count} invalid subscriptions",
                    DeactivatedSubscriptions = invalidSubscriptions.Select(s => new {
                        s.Id,
                        s.ExternalStopCode,
                        UserId = s.User?.TelegramId
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up invalid stations");
                return StatusCode(500, new { Error = "Failed to cleanup invalid stations" });
            }
        }

        [HttpGet("active-subscriptions")]
        public async Task<IActionResult> GetActiveSubscriptions()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .Select(s => new {
                        s.Id,
                        s.ExternalStopCode,
                        s.ExternalRouteNumber,
                        UserId = s.User!.TelegramId,
                        s.NotifyBeforeMinutes,
                        s.CreatedAt
                    })
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscriptions");
                return StatusCode(500, new { Error = "Failed to get active subscriptions" });
            }
        }
    }
}
