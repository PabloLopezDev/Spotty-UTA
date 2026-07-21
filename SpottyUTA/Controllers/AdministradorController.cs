using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Models;
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

        [HttpGet]
        public async Task<IActionResult> GestionSalas(string tab = "all", bool partial = false)
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
            ViewBag.ActiveTab = tab;

            // 1. Obtener todas las salas ordenadas por piso y nombre
            var salas = await _context.Salas.Include(s => s.Reservas).AsNoTracking().OrderBy(s => s.Piso).ThenBy(s => s.Id).ToListAsync();
            ViewBag.Salas = salas;

            // 2. Reservas activas de hoy para cálculo de estado en vivo
            var reservasHoy = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == hoy && r.HoraFin >= horaActual && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .ToListAsync();

            var horario = _salasService.ObtenerHorarioOperacion(ahora);

            // 3. Métricas KPI de Gestión
            int totalSalas = salas.Count;
            int activasCount = salas.Count(s => s.EstadoActual != "I");
            int inhabilitadasCount = salas.Count(s => s.EstadoActual == "I");
            
            // Map de estado actual dinámico por sala
            var estadosSalasMap = new Dictionary<int, string>();
            foreach (var sala in salas)
            {
                string estado = _salasService.ObtenerEstadoSala(sala, reservasHoy, horaActual, horario);
                estadosSalasMap[sala.Id] = estado;
            }

            ViewBag.EstadosSalasMap = estadosSalasMap;
            ViewBag.KpiTotalSalas = totalSalas;
            ViewBag.KpiActivas = activasCount;
            ViewBag.KpiInhabilitadas = inhabilitadasCount;

            if (partial)
            {
                return PartialView("_GestionSalasContent");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarSala(string nombre, int piso, string activeTab = "all")
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrWhiteSpace(nombre) || piso < 1 || piso > 3)
            {
                TempData["ErrorMessage"] = "Debes ingresar un nombre de sala válido y seleccionar un piso entre 1 y 3.";
                return RedirectToAction("GestionSalas", new { tab = activeTab });
            }

            nombre = nombre.Trim();

            // Verificar duplicados de nombre
            bool existe = await _context.Salas.AnyAsync(s => s.Nombre.ToLower() == nombre.ToLower());
            if (existe)
            {
                TempData["ErrorMessage"] = $"Ya existe una sala registrada con el nombre '{nombre}'.";
                return RedirectToAction("GestionSalas", new { tab = activeTab });
            }

            var nuevaSala = new Sala
            {
                Nombre = nombre,
                Piso = piso,
                EstadoActual = "D" // Disponible por defecto
            };

            _context.Salas.Add(nuevaSala);
            await _context.SaveChangesAsync();

            await _salasService.BroadcastEstadosAsync();

            TempData["SuccessMessage"] = $"La sala '{nombre}' ha sido registrada exitosamente en el Piso {piso}.";
            return RedirectToAction("GestionSalas", new { tab = activeTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEstadoSala(int salaId, string activeTab = "all")
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            var sala = await _context.Salas.FindAsync(salaId);
            if (sala == null)
            {
                TempData["ErrorMessage"] = "La sala especificada no fue encontrada.";
                return RedirectToAction("GestionSalas", new { tab = activeTab });
            }

            if (sala.EstadoActual == "I")
            {
                sala.EstadoActual = "D"; // Re-activar
                TempData["SuccessMessage"] = $"La sala '{sala.Nombre}' fue reactivada exitosamente.";
            }
            else
            {
                sala.EstadoActual = "I"; // Inhabilitar / Desactivar
                TempData["WarningMessage"] = $"La sala '{sala.Nombre}' ha sido inhabilitada por mantenimiento.";
            }

            await _context.SaveChangesAsync();
            await _salasService.BroadcastEstadosAsync();

            return RedirectToAction("GestionSalas", new { tab = activeTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarSala(int salaId, string activeTab = "all")
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            var sala = await _context.Salas
                .Include(s => s.Reservas)
                .FirstOrDefaultAsync(s => s.Id == salaId);

            if (sala == null)
            {
                TempData["ErrorMessage"] = "La sala especificada no existe.";
                return RedirectToAction("GestionSalas", new { tab = activeTab });
            }

            // Verificar reservas en curso hoy
            var ahora = SpottyUTA.Helpers.SimulationTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            bool tieneReservasEnUso = sala.Reservas.Any(r => r.Fecha == hoy && r.HoraFin >= horaActual && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"));
            if (tieneReservasEnUso)
            {
                TempData["ErrorMessage"] = $"No se puede eliminar la sala '{sala.Nombre}' porque actualmente tiene una reserva activa en curso.";
                return RedirectToAction("GestionSalas", new { tab = activeTab });
            }

            try
            {
                _context.Salas.Remove(sala);
                await _context.SaveChangesAsync();
                await _salasService.BroadcastEstadosAsync();
                TempData["SuccessMessage"] = $"La sala '{sala.Nombre}' ha sido eliminada del sistema.";
            }
            catch (DbUpdateException)
            {
                _context.Entry(sala).State = EntityState.Unchanged;
                sala.EstadoActual = "I";
                await _context.SaveChangesAsync();
                await _salasService.BroadcastEstadosAsync();
                TempData["WarningMessage"] = $"La sala '{sala.Nombre}' tiene registros de reservas previas en el historial, por lo que se inhabilitó en su lugar para proteger los reportes.";
            }

            return RedirectToAction("GestionSalas", new { tab = activeTab });
        }

        [HttpGet]
        public async Task<IActionResult> Usuarios(string tab = "all", bool partial = false)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.NombreAdministrador = HttpContext.Session.GetString("UsuarioNombre") ?? "Administrador";
            ViewBag.ActiveTab = tab;

            // 1. Obtener todos los usuarios ordenados por nombre con sus reservas
            var usuarios = await _context.Usuarios
                .Include(u => u.Reservas)
                .AsNoTracking()
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();

            ViewBag.Usuarios = usuarios;

            // 2. Métricas KPI de Usuarios
            int totalUsuarios = usuarios.Count;
            int habilitadosCount = usuarios.Count(u => !u.EstaBloqueado);
            int bloqueadosCount = usuarios.Count(u => u.EstaBloqueado);
            int enRiesgoCount = usuarios.Count(u => !u.EstaBloqueado && u.ContadorInasistencias > 0);

            ViewBag.KpiTotalUsuarios = totalUsuarios;
            ViewBag.KpiHabilitados = habilitadosCount;
            ViewBag.KpiBloqueados = bloqueadosCount;
            ViewBag.KpiEnRiesgo = enRiesgoCount;

            if (partial)
            {
                return PartialView("_UsuariosContent");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBloqueoUsuario(int usuarioId, string activeTab = "all")
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "El usuario especificado no existe.";
                return RedirectToAction("Usuarios", new { tab = activeTab });
            }

            if (usuario.EstaBloqueado)
            {
                usuario.EstaBloqueado = false;
                TempData["SuccessMessage"] = $"El usuario '{usuario.NombreCompleto}' fue desbloqueado y habilitado exitosamente.";
            }
            else
            {
                usuario.EstaBloqueado = true;
                TempData["WarningMessage"] = $"El usuario '{usuario.NombreCompleto}' ha sido bloqueado manualmente.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Usuarios", new { tab = activeTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReiniciarInasistencias(int usuarioId, string activeTab = "all")
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "El usuario especificado no existe.";
                return RedirectToAction("Usuarios", new { tab = activeTab });
            }

            usuario.ContadorInasistencias = 0;
            usuario.EstaBloqueado = false;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"El historial de inasistencias de '{usuario.NombreCompleto}' ha sido reiniciado a 0 y su acceso restablecido.";
            return RedirectToAction("Usuarios", new { tab = activeTab });
        }
    }
}
