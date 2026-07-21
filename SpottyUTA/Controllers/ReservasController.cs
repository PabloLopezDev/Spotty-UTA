using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpottyUTA.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    /// <summary>
    /// Controlador que gestiona las operaciones de creación y administración de reservas.
    /// </summary>
    public class ReservasController : Controller
    {
        private readonly IReservasService _reservasService;
        private readonly ILogger<ReservasController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ReservasController"/>.
        /// </summary>
        /// <param name="reservasService">Servicio de lógica de negocio de reservas.</param>
        /// <param name="logger">Logger para registro de eventos y diagnóstico.</param>
        public ReservasController(IReservasService reservasService, ILogger<ReservasController> logger)
        {
            _reservasService = reservasService;
            _logger = logger;
        }

        /// <summary>
        /// Crea una nueva reserva de sala de estudio para el usuario autenticado.
        /// Valida sesión activa y delega la lógica de negocio al servicio de reservas.
        /// </summary>
        /// <param name="salaId">Identificador de la sala a reservar.</param>
        /// <param name="usuarioId">Identificador del usuario (proporcionado por el formulario).</param>
        /// <param name="horaInput">Hora de inicio en formato "HH:mm".</param>
        /// <param name="chkTerminos">Indica si el checkbox de aceptación del reglamento fue marcado.</param>
        /// <returns>Redirección a Home/Index con mensaje de éxito o error.</returns>
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

        /// <summary>
        /// Ejecuta acciones administrativas sobre una reserva existente desde el dashboard del administrador.
        /// Las acciones posibles son: "ocupar" (confirmar asistencia), "liberar" (cancelar sin sanción)
        /// y "falta" (marcar inasistencia).
        /// </summary>
        /// <param name="reservaId">Identificador de la reserva objetivo.</param>
        /// <param name="accion">Código de acción a ejecutar.</param>
        /// <returns>Redirección al Dashboard del Administrador con mensajes de resultado.</returns>
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
