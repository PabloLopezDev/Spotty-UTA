using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpottyUTA.Services;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly IReservasService _reservasService;

        public AdministradorController(IReservasService reservasService)
        {
            _reservasService = reservasService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAsistencia(int usuarioId, string estadoAsistencia, int adminSalaId)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            
            var (success, errorMessage, successMessage, warningMessage) = await _reservasService.RegistrarAsistenciaAsync(
                usuarioId,
                estadoAsistencia,
                adminSalaId,
                rol);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index", "Home");
            }

            if (successMessage != null)
            {
                TempData["SuccessMessage"] = successMessage;
            }
            if (warningMessage != null)
            {
                TempData["WarningMessage"] = warningMessage;
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
