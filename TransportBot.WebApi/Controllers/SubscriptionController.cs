using Microsoft.AspNetCore.Mvc;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;

namespace TransportBot.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost]
        public async Task<ActionResult<Subscription>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            var subscription = await _subscriptionService.CreateSubscriptionAsync(
                request.UserId, 
                request.StopId, 
                request.RouteId, 
                request.NotifyBeforeMinutes);
            
            return Ok(subscription);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetUserSubscriptions(int userId)
        {
            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);
            return Ok(subscriptions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Subscription>> GetSubscription(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionAsync(id);
            if (subscription == null) return NotFound();
            return Ok(subscription);
        }

        [HttpPut("{id}/toggle")]
        public async Task<ActionResult> ToggleSubscription(int id, [FromBody] ToggleSubscriptionRequest request)
        {
            var result = await _subscriptionService.ToggleSubscriptionAsync(id, request.IsActive);
            if (!result) return NotFound();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubscription(int id)
        {
            var result = await _subscriptionService.DeleteSubscriptionAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }

    public record CreateSubscriptionRequest(int UserId, int StopId, int RouteId, int NotifyBeforeMinutes);
    public record ToggleSubscriptionRequest(bool IsActive);
}
