using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using TransportBot.WebApi.DTOs;

namespace TransportBot.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterRequest request)
        {
            var user = await _service.RegisterAsync(request.TelegramId, request.UserName);
            return Ok(user);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(int id)
        {
            var user = await _service.GetAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<User>> Update(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _service.UpdateAsync(id, request.UserName, request.Latitude, request.Longitude);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
