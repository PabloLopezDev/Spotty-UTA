using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpottyUTA.Models;

namespace SpottyUTA.Services
{
    /// <summary>
    /// Modelo que representa el horario de operación de la biblioteca para un día específico.
    /// </summary>
    public class LibrarySchedule
    {
        /// <summary>
        /// Hora de apertura de la biblioteca.
        /// </summary>
        public TimeOnly Apertura { get; set; }

        /// <summary>
        /// Hora de cierre de la biblioteca.
        /// </summary>
        public TimeOnly Cierre { get; set; }

        /// <summary>
        /// Indica si solo está habilitado el primer piso (aplica los sábados).
        /// </summary>
        public bool SoloPrimerPiso { get; set; }

        /// <summary>
        /// Indica si la biblioteca permanece cerrada (aplica los domingos).
        /// </summary>
        public bool Cerrada { get; set; }
    }

    /// <summary>
    /// Interfaz del servicio de gestión de salas de estudio.
    /// </summary>
    public interface ISalasService
    {
        /// <summary>
        /// Obtiene el horario de operación de la biblioteca según el día de la semana.
        /// </summary>
        /// <param name="ahora">Fecha y hora actual del sistema.</param>
        /// <returns>Instancia de <see cref="LibrarySchedule"/> con apertura, cierre y restricciones del día.</returns>
        LibrarySchedule ObtenerHorarioOperacion(DateTime ahora);

        /// <summary>
        /// Evalúa el estado operativo real de una sala considerando reservas activas, tolerancia y horario.
        /// </summary>
        /// <param name="sala">Entidad de la sala a evaluar.</param>
        /// <param name="reservasHoy">Lista de reservas activas del día.</param>
        /// <param name="horaActual">Hora actual del sistema.</param>
        /// <param name="horario">Horario de operación vigente.</param>
        /// <returns>Código de estado: "D" (Disponible), "R" (Reservada), "O" (Ocupada), "I" (Inactiva).</returns>
        string ObtenerEstadoSala(Sala sala, List<Reserva> reservasHoy, TimeOnly horaActual, LibrarySchedule horario);

        /// <summary>
        /// Genera el payload JSON con el estado actual de todas las salas para transmisión vía SignalR.
        /// </summary>
        /// <returns>Lista de objetos anónimos con Id, Estado, Inicio, Fin y CierreUnix de cada sala.</returns>
        Task<List<object>> ObtenerPayloadEstadosSalasAsync();

        /// <summary>
        /// Envía el payload de estados de salas a todos los clientes conectados mediante SignalR.
        /// </summary>
        Task BroadcastEstadosAsync();
    }
}
