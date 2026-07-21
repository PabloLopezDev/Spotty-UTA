using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    /// <summary>
    /// Controlador API para la gestión de las salas, provee endpoints para obtener y actualizar sus estados.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SalasApiController : ControllerBase
    {
        private readonly SpottyUtaContext _context;
        private readonly Services.ISalasService _salasService;

        /// <summary>
        /// Inicializa una nueva instancia del controlador de la API de salas.
        /// </summary>
        /// <param name="context">Contexto de base de datos de SpottyUTA.</param>
        /// <param name="salasService">Servicio para gestionar las salas.</param>
        public SalasApiController(SpottyUtaContext context, Services.ISalasService salasService)
        {
            _context = context;
            _salasService = salasService;
        }

        /// <summary>
        /// Obtiene el estado actual de todas las salas.
        /// </summary>
        /// <returns>Una respuesta HTTP OK que contiene el payload con los estados de las salas.</returns>
        [HttpGet("estados")]
        public async Task<IActionResult> GetEstados()
        {
            var resultado = await _salasService.ObtenerPayloadEstadosSalasAsync();
            return Ok(resultado);
        }

        /// <summary>
        /// Cambia el estado de una sala específica en la base de datos y notifica a los clientes.
        /// </summary>
        /// <param name="id">El identificador único de la sala.</param>
        /// <param name="nuevoEstado">El nuevo estado que se le asignará a la sala.</param>
        /// <returns>Una respuesta HTTP OK si fue exitoso, o NotFound si la sala no existe.</returns>
        [HttpPost("{id}/cambiar-estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
        {
            var sala = await _context.Salas.FindAsync(id);
            if (sala == null) return NotFound();

            sala.EstadoActual = nuevoEstado;
            await _context.SaveChangesAsync();

            await _salasService.BroadcastEstadosAsync();

            return Ok(new { success = true, mensaje = "Estado actualizado en BD" });
        }
    }
}
