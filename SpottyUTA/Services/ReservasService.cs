using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpottyUTA.Data;
using SpottyUTA.Models;

namespace SpottyUTA.Services
{
    public class ReservasService : IReservasService
    {
        private readonly SpottyUtaContext _context;
        private readonly ISalasService _salasService;
        private readonly ILogger<ReservasService> _logger;

        public ReservasService(
            SpottyUtaContext context,
            ISalasService salasService,
            ILogger<ReservasService> logger)
        {
            _context = context;
            _salasService = salasService;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage, string? BloqueAsignado)> CrearReservaAsync(
            int salaId,
            int usuarioId,
            string horaInput,
            bool chkTerminosResolved)
        {
            if (!chkTerminosResolved)
            {
                return (false, "Debes aceptar el Reglamento de Boxes para completar la reserva.", null);
            }

            if (!TimeOnly.TryParse(horaInput, out TimeOnly horaInicio))
            {
                return (false, "El formato de hora ingresado no es válido.", null);
            }

            var ahora = DateTime.Now;
            var fechaHoy = DateOnly.FromDateTime(ahora);

            var horaActualControl = TimeOnly.FromDateTime(ahora);
            bool yaTieneReservaActiva = await _context.Reservas
                .AnyAsync(r => r.UsuarioId == usuarioId &&
                               r.Fecha == fechaHoy &&
                               r.HoraFin > horaActualControl &&
                               (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"));

            if (yaTieneReservaActiva)
            {
                return (false, "¡Asignación denegada! Tu cuenta ya posee un box reservado u ocupado actualmente. Debes esperar a que expire tu bloque vigente.", null);
            }

            var diaSemana = ahora.DayOfWeek;
            TimeOnly apertura;
            TimeOnly cierre;

            var horario = _salasService.ObtenerHorarioOperacion(ahora);

            if (horario.Cerrada)
            {
                return (false, "La biblioteca está cerrada los domingos. No es posible registrar reservas.", null);
            }

            apertura = horario.Apertura;
            cierre = horario.Cierre;

            if (diaSemana == DayOfWeek.Saturday)
            {
                var salaInfo = await _context.Salas.FindAsync(salaId);
                if (salaInfo == null)
                {
                    return (false, "Sala no encontrada.", null);
                }
                if (salaInfo.Piso != 1)
                {
                    return (false, "En jornada de fin de semana solo están habilitados los boxes del 1° piso.", null);
                }
            }

            var ahoraTimeOnly = TimeOnly.FromDateTime(ahora);
            if (horaInicio < ahoraTimeOnly.AddMinutes(-5))
            {
                return (false, "No puedes reservar con una hora de inicio en el pasado.", null);
            }

            if (horaInicio >= cierre || horaInicio > cierre.AddMinutes(-30))
            {
                return (false, $"No es posible reservar con menos de 30 minutos disponibles antes del cierre (cierre: {cierre:HH:mm}).", null);
            }

            TimeOnly horaFinSugerida = horaInicio.AddHours(2);
            if (horaFinSugerida > cierre)
            {
                horaFinSugerida = cierre;
            }

            var reservasExistentes = await _context.Reservas
                .Where(r => r.SalaId == salaId && r.Fecha == fechaHoy && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .ToListAsync();

            var overlapAtStart = reservasExistentes.FirstOrDefault(r => horaInicio >= r.HoraInicio && horaInicio < r.HoraFin);
            if (overlapAtStart != null)
            {
                return (false, $"El box ya se encuentra ocupado en ese horario ({overlapAtStart.HoraInicio:HH:mm} - {overlapAtStart.HoraFin:HH:mm}).", null);
            }

            var primeraReservaFutura = reservasExistentes.Where(r => r.HoraInicio > horaInicio).OrderBy(r => r.HoraInicio).FirstOrDefault();
            if (primeraReservaFutura != null && primeraReservaFutura.HoraInicio < horaFinSugerida)
            {
                horaFinSugerida = primeraReservaFutura.HoraInicio;
            }

            var duracionMinutos = (horaFinSugerida.ToTimeSpan() - horaInicio.ToTimeSpan()).TotalMinutes;
            if (duracionMinutos < 30)
            {
                return (false, "El tiempo útil disponible es inferior al mínimo requerido de 30 minutos tras ajustes. Elige otro horario.", null);
            }

            var nuevaReserva = new Reserva
            {
                SalaId = salaId,
                UsuarioId = usuarioId,
                Fecha = fechaHoy,
                HoraInicio = horaInicio,
                HoraFin = horaFinSugerida,
                EstadoReserva = "A"
            };

            _context.Reservas.Add(nuevaReserva);
            await _context.SaveChangesAsync();

            // Broadcast real-time updates over SignalR
            await _salasService.BroadcastEstadosAsync();

            string bloqueAsignado = $"{horaInicio:HH:mm} - {horaFinSugerida:HH:mm}";
            return (true, null, bloqueAsignado);
        }

        public async Task<(bool Success, string? ErrorMessage, string? SuccessMessage, string? WarningMessage)> GestionarAccionDashboardAsync(
            int reservaId,
            string accion,
            string? usuarioRol)
        {
            if (usuarioRol != "Administrador")
            {
                return (false, "Acceso denegado: Se requieren permisos de administración.", null, null);
            }

            var reserva = await _context.Reservas
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva == null)
            {
                return (false, "La reserva especificada no existe.", null, null);
            }

            string? successMessage = null;
            string? warningMessage = null;

            if (accion == "ocupar")
            {
                reserva.EstadoReserva = "Activa";

                var sala = await _context.Salas.FindAsync(reserva.SalaId);
                if (sala != null)
                {
                    sala.EstadoActual = "O";
                }

                successMessage = "Asistencia confirmada. El box ahora figura en uso.";
            }
            else if (accion == "liberar")
            {
                var sala = await _context.Salas.FindAsync(reserva.SalaId);
                if (sala != null)
                {
                    sala.EstadoActual = "D";
                }

                reserva.EstadoReserva = "Cancelada";

                if (reserva.Usuario != null)
                {
                    reserva.Usuario.ContadorInasistencias += 1;

                    if (reserva.Usuario.ContadorInasistencias >= 3)
                    {
                        reserva.Usuario.EstaBloqueado = true;
                        warningMessage = $"Reserva liberada. El alumno {reserva.Usuario.NombreCompleto} ha sido bloqueado por acumular 3 faltas.";
                    }
                    else
                    {
                        successMessage = $"Reserva liberada. Se registró 1 falta para el alumno (Total: {reserva.Usuario.ContadorInasistencias}).";
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Broadcast real-time updates over SignalR
            await _salasService.BroadcastEstadosAsync();

            return (true, null, successMessage, warningMessage);
        }

        public async Task<(bool Success, string? ErrorMessage, string? SuccessMessage, string? WarningMessage)> RegistrarAsistenciaAsync(
            int usuarioId,
            string estadoAsistencia,
            int adminSalaId,
            string? usuarioRol)
        {
            if (usuarioRol != "Administrador")
            {
                return (false, "No tienes permisos de funcionario.", null, null);
            }

            var sala = await _context.Salas.FirstOrDefaultAsync(s => s.Id == adminSalaId);

            var reservaVigente = await _context.Reservas
                .Where(r => r.SalaId == adminSalaId && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            var estudianteIdToFind = (reservaVigente != null) ? reservaVigente.UsuarioId : usuarioId;
            var estudiante = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == estudianteIdToFind);

            string? successMessage = null;
            string? warningMessage = null;

            if (estadoAsistencia == "Presente")
            {
                if (sala != null)
                {
                    sala.EstadoActual = "O";
                    _context.Entry(sala).State = EntityState.Modified;
                }

                successMessage = "Asistencia confirmada. Box ocupado.";
            }
            else if (estadoAsistencia == "LiberarTemprano")
            {
                if (sala != null)
                {
                    sala.EstadoActual = "D";
                    _context.Entry(sala).State = EntityState.Modified;
                }

                if (reservaVigente != null)
                {
                    reservaVigente.EstadoReserva = "C";
                    _context.Entry(reservaVigente).State = EntityState.Modified;
                }

                successMessage = "El Box ha sido liberado tempranamente. Queda libre en la grilla.";
            }
            else if (estadoAsistencia == "Inasistencia")
            {
                if (sala != null)
                {
                    sala.EstadoActual = "D";
                    _context.Entry(sala).State = EntityState.Modified;
                }

                if (reservaVigente != null)
                {
                    reservaVigente.EstadoReserva = "I";
                    _context.Entry(reservaVigente).State = EntityState.Modified;
                }

                if (estudiante != null)
                {
                    estudiante.ContadorInasistencias += 1;
                    if (estudiante.ContadorInasistencias >= 3)
                    {
                        estudiante.EstaBloqueado = true;
                        warningMessage = $"{estudiante.NombreCompleto} fue bloqueado por acumular 3 inasistencias.";
                    }
                    else
                    {
                        successMessage = $"Inasistencia registrada para {estudiante.NombreCompleto}.";
                    }
                    _context.Entry(estudiante).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();

            // Broadcast real-time updates over SignalR
            await _salasService.BroadcastEstadosAsync();

            return (true, null, successMessage, warningMessage);
        }
    }
}
