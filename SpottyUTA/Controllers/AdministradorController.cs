using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly IReservasService _reservasService;
        private readonly ISalasService _salasService;
        private readonly SpottyUtaContext _context;

        public AdministradorController(IReservasService reservasService, ISalasService salasService, SpottyUtaContext context)
        {
            _reservasService = reservasService;
            _salasService = salasService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(bool partial = false)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            var ahora = SpottyUTA.Helpers.SimulationTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            ViewBag.NombreAdministrador = HttpContext.Session.GetString("UsuarioNombre") ?? "Administrador";

            // 1. Calcular KPIs de Salas de forma fuertemente tipada
            var salas = await _context.Salas.AsNoTracking().ToListAsync();
            var reservasHoyParaCalculo = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == hoy && r.HoraFin >= horaActual && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .ToListAsync();
            var horario = _salasService.ObtenerHorarioOperacion(ahora);

            int disponibles = 0;
            int reservadas = 0;
            int ocupadas = 0;

            foreach (var sala in salas)
            {
                string estado = _salasService.ObtenerEstadoSala(sala, reservasHoyParaCalculo, horaActual, horario);
                if (estado == "D") disponibles++;
                else if (estado == "R") reservadas++;
                else if (estado == "O") ocupadas++;
            }

            // Total de reservas creadas hoy (activas, canceladas o completadas)
            int totalReservasHoy = await _context.Reservas
                .CountAsync(r => r.Fecha == hoy);

            ViewBag.KpiTotalReservas = totalReservasHoy;
            ViewBag.KpiDisponibles = disponibles;
            ViewBag.KpiReservadas = reservadas;
            ViewBag.KpiOcupadas = ocupadas;
            ViewBag.SalasTotalesCount = salas.Count;

            // 2. Reservas Activas del momento
            var reservasActivas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Sala)
                .Where(r => r.Fecha == hoy && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .ToListAsync();

            ViewBag.ReservasActivas = reservasActivas;

            // 3. Próximas reservas (que inician después de ahora)
            var proximasReservas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Sala)
                .Where(r => r.Fecha == hoy && r.HoraInicio > horaActual && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .Take(4)
                .ToListAsync();

            ViewBag.ProximasReservas = proximasReservas;

            // 4. Actividad reciente: últimas reservas creadas, canceladas o usadas de hoy/recientes (ID desc)
            var actividadReciente = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Sala)
                .OrderByDescending(r => r.Id)
                .Take(6)
                .ToListAsync();

            ViewBag.ActividadReciente = actividadReciente;

            if (partial)
            {
                return PartialView("_DashboardContent");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ReservasActivas(bool partial = false)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            var ahora = SpottyUTA.Helpers.SimulationTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            ViewBag.NombreAdministrador = HttpContext.Session.GetString("UsuarioNombre") ?? "Administrador";

            // 1. Obtener todas las salas ordenadas por piso y código
            var salas = await _context.Salas.AsNoTracking().OrderBy(s => s.Piso).ThenBy(s => s.Id).ToListAsync();
            ViewBag.Salas = salas;

            // 2. Obtener todas las reservas activas del día
            var reservasActivas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Sala)
                .Where(r => r.Fecha == hoy && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .ToListAsync();

            ViewBag.ReservasActivas = reservasActivas;

            // 3. Calcular métricas KPI completas (coincidentes con Figma: Total, Disponibles, Reservadas, Ocupadas)
            var horario = _salasService.ObtenerHorarioOperacion(ahora);

            int disponibles = 0;
            int reservadas = 0;
            int ocupadas = 0;

            foreach (var sala in salas)
            {
                string estado = _salasService.ObtenerEstadoSala(sala, reservasActivas, horaActual, horario);
                if (estado == "D") disponibles++;
                else if (estado == "R") reservadas++;
                else if (estado == "O") ocupadas++;
            }

            ViewBag.KpiTotalActivas = reservasActivas.Count;
            ViewBag.KpiDisponibles = disponibles;
            ViewBag.KpiReservadas = reservadas;
            ViewBag.KpiOcupadas = ocupadas;
            ViewBag.SalasTotalesCount = salas.Count;
            ViewBag.HoraActualSimulada = ahora;

            if (partial)
            {
                return PartialView("_ReservasActivasContent");
            }
            return View();
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

        [HttpPost]
        public async Task<IActionResult> ToggleSimulationMode(bool enable)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            SpottyUTA.Helpers.SimulationTime.IsEnabled = enable;
            
            // Broadcast states immediately so all views (dashboard, floor layout) update in real-time!
            await _salasService.BroadcastEstadosAsync();
            
            TempData["SuccessMessage"] = enable ? "Modo simulación activado (Lunes 10:00 AM)." : "Modo simulación desactivado (Tiempo real).";
            return RedirectToAction("Dashboard");
        }
    }
}
