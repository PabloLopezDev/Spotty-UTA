using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpottyUTA.Models;

namespace SpottyUTA.Services
{
    public class LibrarySchedule
    {
        public TimeOnly Apertura { get; set; }
        public TimeOnly Cierre { get; set; }
        public bool SoloPrimerPiso { get; set; }
        public bool Cerrada { get; set; }
    }

    public interface ISalasService
    {
        LibrarySchedule ObtenerHorarioOperacion(DateTime ahora);
        string ObtenerEstadoSala(Sala sala, List<Reserva> reservasHoy, TimeOnly horaActual, LibrarySchedule horario);
        Task<List<object>> ObtenerPayloadEstadosSalasAsync();
        Task BroadcastEstadosAsync();
    }
}
