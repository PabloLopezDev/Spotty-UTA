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
    /// <summary>
    /// Controlador principal del panel de administración de Spotty UTA.
    /// Gestiona las vistas de Dashboard, Reservas Activas, Gestión de Salas y Usuarios,
    /// así como las acciones CRUD sobre salas y la gestión de bloqueos de usuarios.
    /// </summary>
    public class AdministradorController : Controller
    {
        private readonly IReservasService _reservasService;
        private readonly ISalasService _salasService;
        private readonly SpottyUtaContext _context;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AdministradorController"/>.
        /// </summary>
        /// <param name="reservasService">Servicio de lógica de negocio de reservas.</param>
        /// <param name="salasService">Servicio de lógica de negocio de salas.</param>
        /// <param name="context">Contexto de acceso a datos de Entity Framework Core.</param>
        public AdministradorController(IReservasService reservasService, ISalasService salasService, SpottyUtaContext context)
        {
            _reservasService = reservasService;
            _salasService = salasService;
            _context = context;
        }

        /// <summary>
        /// Muestra el Dashboard principal del administrador con KPIs de salas,
        /// reservas activas del momento, próximas reservas y actividad reciente.
        /// </summary>
        /// <param name="partial">Si es true, devuelve solo el contenido parcial para refresco AJAX.</param>
        /// <returns>Vista completa del Dashboard o PartialView para refresco vía SignalR.</returns>
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

            // 4. Actividad reciente: últimas reservas creadas, canceladas o usadas (ID desc)
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

        /// <summary>
        /// Muestra la vista de Reservas Activas con tabla filtrable, KPIs y vista de ocupación temporal.
        /// </summary>
        /// <param name="partial">Si es true, devuelve solo el contenido parcial para refresco AJAX.</param>
        /// <returns>Vista completa de Reservas Activas o PartialView para refresco vía SignalR.</returns>
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

            // 3. Calcular métricas KPI completas
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

        /// <summary>
        /// Registra la asistencia de un estudiante desde el panel del mesón de atención.
        /// </summary>
        /// <param name="usuarioId">Identificador del usuario.</param>
        /// <param name="estadoAsistencia">Estado a registrar: "Presente", "LiberarTemprano" o "Inasistencia".</param>
        /// <param name="adminSalaId">Identificador de la sala gestionada.</param>
        /// <returns>Redirección a Home/Index con mensajes de resultado.</returns>
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

        /// <summary>
        /// Activa o desactiva el modo de simulación de tiempo del sistema.
        /// </summary>
        /// <param name="enable">True para activar, false para desactivar.</param>
        /// <returns>Redirección al Dashboard con mensaje de confirmación.</returns>
        [HttpPost]
        public async Task<IActionResult> ToggleSimulationMode(bool enable)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Login", "Auth");
            }

            SpottyUTA.Helpers.SimulationTime.IsEnabled = enable;
            
            // Transmitir estados inmediatamente para que todas las vistas se actualicen en tiempo real
            await _salasService.BroadcastEstadosAsync();
            
            TempData["SuccessMessage"] = enable ? "Modo simulación activado (Lunes 10:00 AM)." : "Modo simulación desactivado (Tiempo real).";
            return RedirectToAction("Dashboard");
        }

        /// <summary>
        /// Muestra la vista de Gestión de Salas con la matriz de estados por pisos,
        /// KPIs de salas activas/inhabilitadas y opciones de administración.
        /// </summary>
        /// <param name="tab">Pestaña activa del filtro ("all", "piso1", "piso2", "piso3").</param>
        /// <param name="partial">Si es true, devuelve solo el contenido parcial para refresco AJAX.</param>
        /// <returns>Vista completa de Gestión de Salas o PartialView para refresco vía SignalR.</returns>
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

        /// <summary>
        /// Agrega una nueva sala de estudio al sistema.
        /// Valida nombre único y rango de piso válido (1-3).
        /// </summary>
        /// <param name="nombre">Nombre descriptivo de la nueva sala.</param>
        /// <param name="piso">Número de piso donde se ubicará la sala (1, 2 o 3).</param>
        /// <param name="activeTab">Pestaña activa para mantener el contexto del filtro.</param>
        /// <returns>Redirección a GestionSalas con mensaje de resultado.</returns>
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

        /// <summary>
        /// Alterna el estado de una sala entre Activa ("D") e Inhabilitada ("I").
        /// </summary>
        /// <param name="salaId">Identificador de la sala a modificar.</param>
        /// <param name="activeTab">Pestaña activa para mantener el contexto del filtro.</param>
        /// <returns>Redirección a GestionSalas con mensaje de resultado.</returns>
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

        /// <summary>
        /// Elimina definitivamente una sala del sistema. Solo se permite si no tiene
        /// reservas activas en curso. Si tiene historial de reservas, se inhabilita en su lugar.
        /// </summary>
        /// <param name="salaId">Identificador de la sala a eliminar.</param>
        /// <param name="activeTab">Pestaña activa para mantener el contexto del filtro.</param>
        /// <returns>Redirección a GestionSalas con mensaje de resultado.</returns>
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

        /// <summary>
        /// Muestra la vista de gestión de usuarios con filtros por rol y estado,
        /// KPIs de usuarios habilitados, bloqueados y en riesgo.
        /// </summary>
        /// <param name="tab">Pestaña activa del filtro ("all", "habilitados", "bloqueados", "estudiantes", "admins").</param>
        /// <param name="partial">Si es true, devuelve solo el contenido parcial para refresco AJAX.</param>
        /// <returns>Vista completa de Usuarios o PartialView.</returns>
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

        /// <summary>
        /// Alterna el estado de bloqueo de un usuario (bloquear/desbloquear).
        /// </summary>
        /// <param name="usuarioId">Identificador del usuario a modificar.</param>
        /// <param name="activeTab">Pestaña activa para mantener el contexto del filtro.</param>
        /// <returns>Redirección a Usuarios con mensaje de resultado.</returns>
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

        /// <summary>
        /// Reinicia a cero el contador de inasistencias de un usuario y desbloquea su cuenta.
        /// </summary>
        /// <param name="usuarioId">Identificador del usuario.</param>
        /// <param name="activeTab">Pestaña activa para mantener el contexto del filtro.</param>
        /// <returns>Redirección a Usuarios con mensaje de confirmación.</returns>
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
