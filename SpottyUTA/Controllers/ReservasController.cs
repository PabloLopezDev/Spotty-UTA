using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpottyUTA.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    public class ReservasController : Controller
    {
        private readonly IReservasService _reservasService;
        private readonly ILogger<ReservasController> _logger;

        public ReservasController(IReservasService reservasService, ILogger<ReservasController> logger)
        {
            _reservasService = reservasService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearReserva(int salaId, int usuarioId, string horaInput, bool chkTerminos = false)
        {
            int? usuarioLogueadoId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioLogueadoId == null)
            {
                TempData["ErrorMessage"] = "Acceso denegado: Debes iniciar sesión con tu correo @alumnos.uta.cl para reservar.";
                return RedirectToAction("Index", "Home");
            }

            var formValues = Request.Form["chkTerminos"];
            bool chkTerminosResolved = chkTerminos;
            try
            {
                if (formValues.Count > 0)
                {
                    chkTerminosResolved = formValues.Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch { }

            _logger?.LogInformation("CrearReserva: Request.Form chkTerminos raw values = {vals}; resolved = {resolved}", string.Join(",", formValues.ToArray()), chkTerminosResolved);

            var (success, errorMessage, bloqueAsignado) = await _reservasService.CrearReservaAsync(
                salaId,
                usuarioLogueadoId.Value,
                horaInput,
                chkTerminosResolved);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["MostrarModalExito"] = true;
                TempData["BloqueAsignado"] = bloqueAsignado;
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GestionarAccionDashboard(int reservaId, string accion)
        {
            var usuarioRol = HttpContext.Session.GetString("UsuarioRol");
            var (success, errorMessage, successMessage, warningMessage) = await _reservasService.GestionarAccionDashboardAsync(
                reservaId,
                accion,
                usuarioRol);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
                if (usuarioRol != "Administrador")
                {
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction("Dashboard", "Administrador");
            }

            if (successMessage != null)
            {
                TempData["SuccessMessage"] = successMessage;
            }
            if (warningMessage != null)
            {
                TempData["WarningMessage"] = warningMessage;
            }

            return RedirectToAction("Dashboard", "Administrador");
        }
    }
}
