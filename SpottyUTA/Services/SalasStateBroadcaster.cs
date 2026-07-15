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
            var salasService = scope.ServiceProvider.GetRequiredService<ISalasService>();
            await salasService.BroadcastEstadosAsync();
        }
    }
}
