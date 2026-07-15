using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using System.Threading.Tasks;
using System.Linq;

namespace SpottyUTA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalasApiController : ControllerBase
    {
        private readonly SpottyUtaContext _context;
        private readonly Services.ISalasService _salasService;

        public SalasApiController(SpottyUtaContext context, Services.ISalasService salasService)
        {
            _context = context;
            _salasService = salasService;
        }

        [HttpGet("estados")]
        public async Task<IActionResult> GetEstados()
        {
            var resultado = await _salasService.ObtenerPayloadEstadosSalasAsync();
            return Ok(resultado);
        }

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
