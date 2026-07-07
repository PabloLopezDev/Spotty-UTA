using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using SpottyUTA.Data;
using SpottyUTA.Models;
using Microsoft.AspNetCore.SignalR;
using SpottyUTA.Hubs;
using Microsoft.AspNetCore.SignalR; // SignalR
using SpottyUTA.Hubs;              // Hub de salas

namespace SpottyUTA.Controllers
{
    public class ReservasController : Controller
    {
        private readonly SpottyUtaContext _context;
        private readonly IHubContext<SalasHub> _hubContext; // Contexto de SignalR
        private readonly ILogger<ReservasController> _logger;

        // Modificamos el constructor para inyectar el Hub de salas
        public ReservasController(SpottyUtaContext context, IHubContext<SalasHub> hubContext, ILogger<ReservasController> logger)
        {
            _context = context;
            _hubContext = hubContext; // Guardamos la referencia
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 1. Mantenemos el parámetro 'usuarioId' para que no tire error de firma, pero NO lo usaremos adentro
        public async Task<IActionResult> CrearReserva(int salaId, int usuarioId, string horaInput, bool chkTerminos = false)
        {
            // Validar autenticación institucional por servidor
            int? usuarioLogueadoId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioLogueadoId == null)
            {
                TempData["ErrorMessage"] = "Acceso denegado: Debes iniciar sesión con tu correo @alumnos.uta.cl para reservar.";
                return RedirectToAction("Index", "Home");
            }

            // Validar aceptación de reglamento (checkbox enviado desde el formulario)
            var formValues = Request.Form["chkTerminos"];
            bool chkTerminosResolved = chkTerminos;
            try
            {
                if (formValues.Count > 0)
                {
                    chkTerminosResolved = formValues.Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch { /* no bloquear en caso de lectura inesperada */ }

            _logger?.LogInformation("CrearReserva: Request.Form chkTerminos raw values = {vals}; resolved = {resolved}", string.Join(",", formValues.ToArray()), chkTerminosResolved);

            if (!chkTerminosResolved)
            {
                TempData["ErrorMessage"] = "Debes aceptar el Reglamento de Boxes para completar la reserva.";
                return RedirectToAction("Index", "Home");
            }

            if (!TimeOnly.TryParse(horaInput, out TimeOnly horaInicio))
            {
                TempData["ErrorMessage"] = "El formato de hora ingresado no es válido.";
                return RedirectToAction("Index", "Home");
            }

            var ahora = DateTime.Now;
            var fechaHoy = DateOnly.FromDateTime(ahora);

            // Restricción de 1 sola reserva activa por alumno UTA
            // Buscamos si el usuario de la sesión ya tiene un bloque vigente hoy que coincida con el tiempo actual
            var horaActualControl = TimeOnly.FromDateTime(ahora);
            bool yaTieneReservaActiva = await _context.Reservas
                .AnyAsync(r => r.UsuarioId == usuarioLogueadoId.Value &&
                               r.Fecha == fechaHoy &&
                               r.HoraFin > horaActualControl &&
                               (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"));

            if (yaTieneReservaActiva)
            {
                TempData["ErrorMessage"] = "¡Asignación denegada! Tu cuenta ya posee un box reservado u ocupado actualmente. Debes esperar a que expire tu bloque vigente.";
                return RedirectToAction("Index", "Home");
            }

            // Regla 6: adaptación de horario según el día
            var diaSemana = ahora.DayOfWeek;
            TimeOnly apertura = default;
            TimeOnly cierre;

            if (diaSemana == DayOfWeek.Sunday)
            {
                TempData["ErrorMessage"] = "La biblioteca está cerrada los domingos. No es posible registrar reservas.";
                return RedirectToAction("Index", "Home");
            }
            else if (diaSemana == DayOfWeek.Saturday)
            {
                apertura = new TimeOnly(9, 0);
                cierre = new TimeOnly(13, 0);

                // Verificar que la sala sea del primer piso
                var salaInfo = await _context.Salas.FindAsync(salaId);
                if (salaInfo == null)
                {
                    TempData["ErrorMessage"] = "Sala no encontrada.";
                    return RedirectToAction("Index", "Home");
                }
                if (salaInfo.Piso != 1)
                {
                    TempData["ErrorMessage"] = "En jornada de fin de semana solo están habilitados los boxes del 1° piso.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                // Lunes a Viernes
                apertura = new TimeOnly(8, 0);
                cierre = new TimeOnly(21, 0);
            }

            // Regla 4: No permitir horas de inicio en el pasado (tolerancia 5 minutos)
            var ahoraTimeOnly = TimeOnly.FromDateTime(ahora);
            if (horaInicio < ahoraTimeOnly.AddMinutes(-5))
            {
                TempData["ErrorMessage"] = "No puedes reservar con una hora de inicio en el pasado.";
                return RedirectToAction("Index", "Home");
            }

            // Regla 5: Margen mínimo de 30 minutos antes del cierre
            if (horaInicio >= cierre || horaInicio > cierre.AddMinutes(-30))
            {
                TempData["ErrorMessage"] = $"No es posible reservar con menos de 30 minutos disponibles antes del cierre (cierre: {cierre:HH:mm}).";
                return RedirectToAction("Index", "Home");
            }

            // Max 2 horas por solicitud
            TimeOnly horaFinSugerida = horaInicio.AddHours(2);
            if (horaFinSugerida > cierre) horaFinSugerida = cierre;

            var reservasExistentes = await _context.Reservas
                .Where(r => r.SalaId == salaId && r.Fecha == fechaHoy && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .ToListAsync();

            // Regla 3 & 2: Detectar solapamientos y recorte inteligente
            var overlapAtStart = reservasExistentes.FirstOrDefault(r => horaInicio >= r.HoraInicio && horaInicio < r.HoraFin);
            if (overlapAtStart != null)
            {
                TempData["ErrorMessage"] = $"El box ya se encuentra ocupado en ese horario ({overlapAtStart.HoraInicio:HH:mm} - {overlapAtStart.HoraFin:HH:mm}).";
                return RedirectToAction("Index", "Home");
            }

            var primeraReservaFutura = reservasExistentes.Where(r => r.HoraInicio > horaInicio).OrderBy(r => r.HoraInicio).FirstOrDefault();
            if (primeraReservaFutura != null && primeraReservaFutura.HoraInicio < horaFinSugerida)
            {
                horaFinSugerida = primeraReservaFutura.HoraInicio;
            }

            var duracionMinutos = (horaFinSugerida.ToTimeSpan() - horaInicio.ToTimeSpan()).TotalMinutes;
            if (duracionMinutos < 30)
            {
                TempData["ErrorMessage"] = "El tiempo útil disponible es inferior al mínimo requerido de 30 minutos tras ajustes. Elige otro horario.";
                return RedirectToAction("Index", "Home");
            }

            // Asignación real: usamos 'usuarioLogueadoId.Value' en vez de la variable 'usuarioId' que venía del HTML
            var nuevaReserva = new Reserva
            {
                SalaId = salaId,
                UsuarioId = usuarioLogueadoId.Value,
                Fecha = fechaHoy,
                HoraInicio = horaInicio,
                HoraFin = horaFinSugerida,
                EstadoReserva = "A"
            };

            try
            {
                _context.Reservas.Add(nuevaReserva);
                await _context.SaveChangesAsync();

                // [Aquí se mantiene idéntico todo tu bloque de SignalR que genera el payload de notificaciones...]
                try
                {
                    var ahoraNotif = DateTime.Now;
                    var fechaNotif = DateOnly.FromDateTime(ahoraNotif);
                    var horaNotif = TimeOnly.FromDateTime(ahoraNotif);

                    var reservasHoyNotif = await _context.Reservas
                        .Where(r => r.Fecha == fechaNotif && r.HoraFin >= horaNotif && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                        .ToListAsync();

                    var salasNotif = await _context.Salas.ToListAsync();
                    var payload = new List<object>();

                    foreach (var s in salasNotif)
                    {
                        string estado = "D";
                        var reservasDeEstaSala = reservasHoyNotif.Where(r => r.SalaId == s.Id).ToList();
                        var reservaActual = reservasDeEstaSala.FirstOrDefault(r => horaNotif >= r.HoraInicio && horaNotif < r.HoraFin);
                        string inicioStr = null;
                        string finStr = null;
                        long? cierreUnix = null;

                        if (reservaActual != null)
                        {
                            double minutesTrans = (horaNotif.ToTimeSpan() - reservaActual.HoraInicio.ToTimeSpan()).TotalMinutes;
                            estado = minutesTrans <= 15 ? "R" : "O";
                            inicioStr = reservaActual.HoraInicio.ToString("HH:mm");
                            finStr = reservaActual.HoraFin.ToString("HH:mm");

                            var fechaYHoraCierre = DateTime.Today.Add(reservaActual.HoraFin.ToTimeSpan());
                            cierreUnix = new DateTimeOffset(fechaYHoraCierre).ToUnixTimeSeconds();
                        }
                        else
                        {
                            bool proximaReservaCercana = reservasDeEstaSala.Any(r => r.HoraInicio > horaNotif && (r.HoraInicio.ToTimeSpan() - horaNotif.ToTimeSpan()).TotalMinutes <= 20);
                            estado = proximaReservaCercana ? "R" : "D";
                        }

                        payload.Add(new { Id = s.Id, Estado = estado, Inicio = inicioStr, Fin = finStr, CierreUnix = cierreUnix });
                    }

                    await _hubContext.Clients.All.SendAsync("ActualizarMatrizSalas", payload);
                }
                catch
                {
                    try { await _hubContext.Clients.All.SendAsync("ActualizarMatrizSalas"); } catch { }
                }

                TempData["MostrarModalExito"] = true;
                TempData["BloqueAsignado"] = $"{horaInicio:HH:mm} - {horaFinSugerida:HH:mm}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error de BD: {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GestionarAccionDashboard(int reservaId, string accion)
        {
            // 🔒 Validar que quien ejecute esto sea Administrador
            var usuarioRol = HttpContext.Session.GetString("UsuarioRol");
            if (usuarioRol != "Administrador")
            {
                TempData["ErrorMessage"] = "Acceso denegado: Se requieren permisos de administración.";
                return RedirectToAction("Index", "Home");
            }

            // Buscar la reserva activa que se está mostrando en la tabla
            var reserva = await _context.Reservas
                .Include(r => r.Usuario) // Incluimos al usuario para poder modificar sus faltas si es necesario
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva == null)
            {
                TempData["ErrorMessage"] = "La reserva especificada no existe.";
                return RedirectToAction("AdminDashboard");
            }

            var ahora = DateTime.Now;
            var horaActual = TimeOnly.FromDateTime(ahora);

            if (accion == "ocupar")
            {
                // El alumno llegó al mesón: pasamos la reserva a estado Ocupado
                // En la lógica de HomeController, si han pasado menos de 15 minutos se calcula como R.
                reserva.EstadoReserva = "Activa";

                // Si necesitas modificar una columna de la tabla Sala para que cambie a "O" inmediatamente:
                var sala = await _context.Salas.FindAsync(reserva.SalaId);
                if (sala != null)
                {
                    sala.EstadoActual = "O"; // Ocupado directo
                }

                TempData["SuccessMessage"] = "Asistencia confirmada. El box ahora figura en uso.";
            }
            else if (accion == "liberar")
            {
                // Alumno no llegó (inasistencia) o terminó antes.
                var sala = await _context.Salas.FindAsync(reserva.SalaId);
                if (sala != null)
                {
                    sala.EstadoActual = "D"; // Vuelve a estar disponible
                }

                // Cambiamos el estado de la asignación a cancelada/finalizada
                reserva.EstadoReserva = "Cancelada";

                // Si fue por inasistencia, sumamos al contador del estudiante
                if (reserva.Usuario != null)
                {
                    reserva.Usuario.ContadorInasistencias += 1;

                    // Regla de negocio: si llega a 3 inasistencias, se bloquea automáticamente
                    if (reserva.Usuario.ContadorInasistencias >= 3)
                    {
                        reserva.Usuario.EstaBloqueado = true;
                        TempData["WarningMessage"] = $"Reserva liberada. El alumno {reserva.Usuario.NombreCompleto} ha sido bloqueado por acumular 3 faltas.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Reserva liberada. Se registró 1 falta para el alumno (Total: {reserva.Usuario.ContadorInasistencias}).";
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Actualizar la grilla en vivo
            try
            {
                // Aquí se puede invocar la lógica para enviar el nuevo payload con la sala modificada.
            }
            catch { /* Evitar caídas si SignalR no responde */ }

            // Redirigir de vuelta al panel de administración que diseñaste
            return RedirectToAction("AdminDashboard");
        }
    }
}