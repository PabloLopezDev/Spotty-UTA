using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpottyUTA.Data;
using SpottyUTA.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpottyUTA.Services
{
    /// <summary>
    /// Background service that periodically checks the DB for current/future reservations
    /// and broadcasts an update to all connected SignalR clients so the UI does not need
    /// a full page refresh when reservations are changed externally or when time elapses.
    /// </summary>
    public class SalasStateBroadcaster : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

        public SalasStateBroadcaster(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await BroadcastEstadosAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // keep alive on errors
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task BroadcastEstadosAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SpottyUtaContext>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<SalasHub>>();

            var ahora = DateTime.Now;
            var fechaActual = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            var reservasHoy = await context.Reservas
                .Where(r => r.Fecha == fechaActual && r.HoraFin >= horaActual && (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .ToListAsync(cancellationToken);

            var salas = await context.Salas.ToListAsync(cancellationToken);

            var payload = new List<object>();

            foreach (var s in salas)
            {
                string estado = "D";
                string inicioStr = null;
                string finStr = null;

                bool bibliotecaCerrada = false;

                if (bibliotecaCerrada)
                {
                    estado = "I";
                }
                else
                {
                    var reservasDeEstaSala = reservasHoy.Where(r => r.SalaId == s.Id).ToList();
                    var reservaActual = reservasDeEstaSala.FirstOrDefault(r => horaActual >= r.HoraInicio && horaActual < r.HoraFin);
                    if (reservaActual != null)
                    {
                        double minutosTranscurridos = (horaActual.ToTimeSpan() - reservaActual.HoraInicio.ToTimeSpan()).TotalMinutes;
                        estado = minutosTranscurridos <= 15 ? "R" : "O";
                        inicioStr = reservaActual.HoraInicio.ToString("HH:mm");
                        finStr = reservaActual.HoraFin.ToString("HH:mm");
                    }
                    else
                    {
                        bool proximaReservaCercana = reservasDeEstaSala.Any(r => r.HoraInicio > horaActual && (r.HoraInicio.ToTimeSpan() - horaActual.ToTimeSpan()).TotalMinutes <= 20);
                        estado = proximaReservaCercana ? "R" : "D";
                    }
                }

                payload.Add(new { Id = s.Id, Estado = estado, Inicio = inicioStr, Fin = finStr });
            }

            await hub.Clients.All.SendAsync("ActualizarMatrizSalas", payload, cancellationToken);
        }
    }
}
