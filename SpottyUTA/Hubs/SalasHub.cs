using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SpottyUTA.Hubs
{
    public class SalasHub : Hub
    {
        public async Task NotificarCambioSala()
        {
            await Clients.All.SendAsync("ActualizarMatrizSalas");
        }
    }
}