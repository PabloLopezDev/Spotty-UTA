using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Hubs;
using SpottyUTA.Models;

namespace SpottyUTA.Services
{
    public class SalasService : ISalasService
    {
        private readonly SpottyUtaContext _context;
        private readonly IHubContext<SalasHub> _hubContext;

        public SalasService(SpottyUtaContext context, IHubContext<SalasHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public LibrarySchedule ObtenerHorarioOperacion(DateTime ahora)
        {
            if (ahora.DayOfWeek == DayOfWeek.Sunday)
            {
                return new LibrarySchedule { Apertura = TimeOnly.MinValue, Cierre = TimeOnly.MinValue, SoloPrimerPiso = false, Cerrada = true };
            }

            if (ahora.DayOfWeek == DayOfWeek.Saturday)
            {
                return new LibrarySchedule { Apertura = new TimeOnly(9, 0), Cierre = new TimeOnly(13, 0), SoloPrimerPiso = true, Cerrada = false };
            }

            return new LibrarySchedule { Apertura = new TimeOnly(8, 0), Cierre = new TimeOnly(21, 0), SoloPrimerPiso = false, Cerrada = false };
        }

        public string ObtenerEstadoSala(Sala sala, List<Reserva> reservasHoy, TimeOnly horaActual, LibrarySchedule horario)
        {
            if (horario.Cerrada)
            {
                return "I";
            }

            if (horario.SoloPrimerPiso && sala.Piso != 1)
            {
                return "I";
            }

            if (sala.EstadoActual == "I")
            {
                return "I";
            }

            var reservasDeEstaSala = reservasHoy.Where(r => r.SalaId == sala.Id).ToList();
            var reservaActual = reservasDeEstaSala.FirstOrDefault(r => horaActual >= r.HoraInicio && horaActual < r.HoraFin);

            if (reservaActual != null)
            {
                // Si el administrador ya confirmó la asistencia presencial en el mesón
                if (sala.EstadoActual == "O")
                {
                    return "O"; // Ocupado (Rojo)
                }

                // Si no ha confirmado, tiene 20 minutos de tolerancia para presentarse
                var minutosTranscurridos = (horaActual.ToTimeSpan() - reservaActual.HoraInicio.ToTimeSpan()).TotalMinutes;
                if (minutosTranscurridos <= 20)
                {
                    return "R"; // Reservado / En espera (Amarillo)
                }
                else
                {
                    // Pasaron los 20 minutos y no se confirmó la asistencia -> Se libera la sala
                    return "D"; // Disponible (Verde)
                }
            }

            // Si no hay reserva en curso, verificamos si hay una reserva próxima en los siguientes 20 minutos
            var proximaReservaCercana = reservasDeEstaSala.Any(r =>
                r.HoraInicio > horaActual &&
                (r.HoraInicio.ToTimeSpan() - horaActual.ToTimeSpan()).TotalMinutes <= 20);

            return proximaReservaCercana ? "R" : "D";
        }

        public async Task<List<object>> ObtenerPayloadEstadosSalasAsync()
        {
            var ahora = SpottyUTA.Helpers.SimulationTime.Now;
            var fechaActual = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            var reservasHoy = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == fechaActual && r.HoraFin >= horaActual && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .ToListAsync();

            var salas = await _context.Salas.AsNoTracking().ToListAsync();
            var horario = ObtenerHorarioOperacion(ahora);

            var payload = new List<object>();

            foreach (var s in salas)
            {
                string estado = ObtenerEstadoSala(s, reservasHoy, horaActual, horario);
                string? inicioStr = null;
                string? finStr = null;
                long? cierreUnix = null;

                var reservasDeEstaSala = reservasHoy.Where(r => r.SalaId == s.Id).ToList();
                var reservaActual = reservasDeEstaSala.FirstOrDefault(r => horaActual >= r.HoraInicio && horaActual < r.HoraFin);

                if (reservaActual != null)
                {
                    inicioStr = reservaActual.HoraInicio.ToString("HH:mm");
                    finStr = reservaActual.HoraFin.ToString("HH:mm");

                    var fechaYHoraCierre = SpottyUTA.Helpers.SimulationTime.Today.Add(reservaActual.HoraFin.ToTimeSpan());
                    cierreUnix = new DateTimeOffset(fechaYHoraCierre).ToUnixTimeSeconds();
                }

                payload.Add(new { Id = s.Id, Estado = estado, Inicio = inicioStr, Fin = finStr, CierreUnix = cierreUnix });
            }

            return payload;
        }

        public async Task BroadcastEstadosAsync()
        {
            try
            {
                var payload = await ObtenerPayloadEstadosSalasAsync();
                await _hubContext.Clients.All.SendAsync("ActualizarMatrizSalas", payload);
            }
            catch
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("ActualizarMatrizSalas");
                }
                catch { }
            }
        }
    }
}
