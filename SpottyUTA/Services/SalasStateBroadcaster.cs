using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpottyUTA.Data;

namespace SpottyUTA.Services
{
    /// <summary>
    /// Servicio en segundo plano que periódicamente (cada 15 segundos) consulta la base de datos
    /// para limpiar automáticamente salas cuyo estado "Ocupado" haya expirado y transmitir
    /// el estado actualizado de todas las salas a los clientes conectados vía SignalR.
    /// </summary>
    public class SalasStateBroadcaster : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SalasStateBroadcaster"/>.
        /// </summary>
        /// <param name="services">Proveedor de servicios para crear scopes de inyección de dependencias.</param>
        /// <exception cref="ArgumentNullException">Se lanza si <paramref name="services"/> es nulo.</exception>
        public SalasStateBroadcaster(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Bucle principal del servicio en segundo plano. Se ejecuta continuamente hasta que se solicite la cancelación.
        /// </summary>
        /// <param name="stoppingToken">Token de cancelación proporcionado por el host.</param>
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
                    // Mantener el servicio activo ante errores transitorios
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

        /// <summary>
        /// Ejecuta un ciclo de limpieza de estados expirados y transmite el estado actualizado
        /// de todas las salas a los clientes conectados vía SignalR.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación.</param>
        private async Task BroadcastEstadosAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SpottyUtaContext>();
            var salasService = scope.ServiceProvider.GetRequiredService<ISalasService>();

            // Limpieza automática de estados "Ocupado" (O) que hayan expirado
            var ahora = SpottyUTA.Helpers.SimulationTime.Now;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            var salasOcupadas = await context.Salas
                .Where(s => s.EstadoActual == "O")
                .ToListAsync(cancellationToken);

            bool cambiosRealizados = false;
            foreach (var sala in salasOcupadas)
            {
                var tieneReservaVigenteActiva = await context.Reservas
                    .AnyAsync(r => r.SalaId == sala.Id && 
                                   r.Fecha == hoy && 
                                   r.HoraInicio <= horaActual && 
                                   r.HoraFin > horaActual && 
                                   (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"), 
                               cancellationToken);

                if (!tieneReservaVigenteActiva)
                {
                    sala.EstadoActual = "D";
                    context.Entry(sala).State = EntityState.Modified;
                    cambiosRealizados = true;
                }
            }

            if (cambiosRealizados)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            await salasService.BroadcastEstadosAsync();
        }
    }
}
