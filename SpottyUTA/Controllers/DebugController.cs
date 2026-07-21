using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SpottyUTA.Hubs;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    /// <summary>
    /// Controlador para operaciones de depuración y pruebas de la aplicación.
    /// </summary>
    [ApiController]
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        private readonly IHubContext<SalasHub> _hub;

        /// <summary>
        /// Inicializa una nueva instancia del controlador de depuración.
        /// </summary>
        /// <param name="hub">Contexto del Hub de SignalR para envío de mensajes en tiempo real.</param>
        public DebugController(IHubContext<SalasHub> hub)
        {
            _hub = hub;
        }

        /// <summary>
        /// Envía un broadcast de prueba con un payload simplificado a todos los clientes conectados.
        /// </summary>
        /// <returns>Un resultado HTTP OK indicando que el mensaje fue enviado.</returns>
        [HttpGet("notify")]
        public async Task<IActionResult> Notify()
        {
            var sample = new[] { new { Id = 1, Estado = "R" }, new { Id = 2, Estado = "O" } };
            await _hub.Clients.All.SendAsync("ActualizarMatrizSalas", sample);
            return Ok(new { sent = true });
        }
    }
}
