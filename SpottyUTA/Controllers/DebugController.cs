using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SpottyUTA.Hubs;

namespace SpottyUTA.Controllers
{
    [ApiController]
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        private readonly IHubContext<SalasHub> _hub;

        public DebugController(IHubContext<SalasHub> hub)
        {
            _hub = hub;
        }

        // GET /debug/notify -> envía un broadcast de prueba con payload simplificado
        [HttpGet("notify")]
        public async Task<IActionResult> Notify()
        {
            var sample = new[] { new { Id = 1, Estado = "R" }, new { Id = 2, Estado = "O" } };
            await _hub.Clients.All.SendAsync("ActualizarMatrizSalas", sample);
            return Ok(new { sent = true });
        }
    }
}
