using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SpottyUTA.Hubs
{
    // El Hub es el "servidor central" al que se conectarán todos los navegadores
    public class SalasHub : Hub
    {
        // Este método permite avisar a todos que la grilla debe actualizarse
        public async Task NotificarCambioSala()
        {
            await Clients.All.SendAsync("ActualizarMatrizSalas");
        }
    }
}