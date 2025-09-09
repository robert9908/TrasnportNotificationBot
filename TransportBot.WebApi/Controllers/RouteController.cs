using Microsoft.AspNetCore.Mvc;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using RouteEntity = TransportBot.Core.Entities.Route;

namespace TransportBot.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteEntity>>> GetAllRoutes()
        {
            var routes = await _routeService.GetAllRoutesAsync();
            return Ok(routes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RouteEntity>> GetRoute(int id)
        {
            var route = await _routeService.GetRouteAsync(id);
            if (route == null)
                return NotFound();

            return Ok(route);
        }

        [HttpGet("by-stop/{stopId}")]
        public async Task<ActionResult<IEnumerable<RouteEntity>>> GetRoutesByStop(int stopId)
        {
            var routes = await _routeService.GetRoutesByStopAsync(stopId);
            return Ok(routes);
        }

        [HttpPost]
        public async Task<ActionResult<RouteEntity>> CreateRoute(RouteEntity route)
        {
            var createdRoute = await _routeService.CreateRouteAsync(route);
            return CreatedAtAction(nameof(GetRoute), new { id = createdRoute.Id }, createdRoute);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoute(int id, RouteEntity route)
        {
            if (id != route.Id)
                return BadRequest();

            var updatedRoute = await _routeService.UpdateRouteAsync(route);
            if (updatedRoute == null)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var result = await _routeService.DeleteRouteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
