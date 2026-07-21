using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    /// <summary>
    /// Controlador principal que gestiona las vistas públicas y el inicio de la aplicación.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly SpottyUtaContext _context;
        private readonly Services.ISalasService _salasService;

        /// <summary>
        /// Inicializa una nueva instancia del controlador principal.
        /// </summary>
        /// <param name="context">Contexto de base de datos de SpottyUTA.</param>
        /// <param name="salasService">Servicio para operaciones relacionadas con las salas.</param>
        public HomeController(SpottyUtaContext context, Services.ISalasService salasService)
        {
            _context = context;
            _salasService = salasService;
        }

        /// <summary>
        /// Muestra la página principal de la aplicación, cargando la disponibilidad de las salas.
        /// </summary>
        /// <returns>La vista principal con las salas y sus estados, o redirección si es administrador.</returns>
        public async Task<IActionResult> Index()
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol == "Administrador")
            {
                return RedirectToAction("Dashboard", "Administrador");
            }

            var ahora = SpottyUTA.Helpers.SimulationTime.Now;
            var fechaActual = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            ViewBag.DiaSemana = ahora.DayOfWeek;

            var reservasHoy = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == fechaActual &&
                       r.HoraFin >= horaActual &&
                       (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .ToListAsync();

            ViewBag.ReservasHoy = reservasHoy;

            var salas = await _context.Salas
                .AsNoTracking()
                .OrderBy(s => s.Piso)
                .ThenBy(s => s.Nombre)
                .ToListAsync();

            var horario = _salasService.ObtenerHorarioOperacion(ahora);

            foreach (var sala in salas)
            {
                sala.Reservas = new List<Reserva>();
                sala.EstadoActual = _salasService.ObtenerEstadoSala(sala, reservasHoy, horaActual, horario);
            }

            return View(salas);
        }

        /// <summary>
        /// Muestra la política de privacidad de la aplicación.
        /// </summary>
        /// <returns>La vista de privacidad.</returns>
        public IActionResult Privacy() => View();

        /// <summary>
        /// Muestra el reglamento de uso de las salas de la aplicación.
        /// </summary>
        /// <returns>La vista del reglamento.</returns>
        public IActionResult Reglamento() => View();
    }
}
