using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SpottyUTA.Hubs
{
    /// <summary>
    /// Hub de SignalR que actúa como punto de conexión WebSocket para la comunicación
    /// en tiempo real entre el servidor y los clientes del navegador.
    /// </summary>
    public class SalasHub : Hub
    {
        /// <summary>
        /// Notifica a todos los clientes conectados que ha ocurrido un cambio en el estado de las salas.
        /// Envía el evento "ActualizarMatrizSalas" para que los clientes refresquen su interfaz.
        /// </summary>
        public async Task NotificarCambioSala()
        {
            await Clients.All.SendAsync("ActualizarMatrizSalas");
        }
    }
}