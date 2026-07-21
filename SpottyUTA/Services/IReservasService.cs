using System;
using System.Threading.Tasks;

namespace SpottyUTA.Services
{
    /// <summary>
    /// Interfaz del servicio de gestión de reservas de boxes de estudio.
    /// </summary>
    public interface IReservasService
    {
        /// <summary>
        /// Crea una nueva reserva de sala de estudio, validando reglas de negocio
        /// como reglamento aceptado, formato de hora, duplicados, horario operativo,
        /// solapamientos y duración mínima.
        /// </summary>
        /// <param name="salaId">Identificador de la sala a reservar.</param>
        /// <param name="usuarioId">Identificador del usuario solicitante.</param>
        /// <param name="horaInput">Hora de inicio en formato "HH:mm".</param>
        /// <param name="chkTerminosResolved">Indica si el usuario aceptó el reglamento de boxes.</param>
        /// <returns>
        /// Tupla con: éxito de la operación, mensaje de error (si aplica), y bloque horario asignado (si fue exitoso).
        /// </returns>
        Task<(bool Success, string? ErrorMessage, string? BloqueAsignado)> CrearReservaAsync(
            int salaId,
            int usuarioId,
            string horaInput,
            bool chkTerminosResolved);

        /// <summary>
        /// Gestiona acciones administrativas sobre una reserva existente:
        /// "ocupar" (confirmar asistencia), "liberar" (cancelar sin sanción),
        /// o "falta" (marcar inasistencia con posible bloqueo).
        /// </summary>
        /// <param name="reservaId">Identificador de la reserva objetivo.</param>
        /// <param name="accion">Código de acción: "ocupar", "liberar" o "falta".</param>
        /// <param name="usuarioRol">Rol del usuario que ejecuta la acción.</param>
        /// <returns>
        /// Tupla con: éxito de la operación, mensaje de error, mensaje de éxito y mensaje de advertencia.
        /// </returns>
        Task<(bool Success, string? ErrorMessage, string? SuccessMessage, string? WarningMessage)> GestionarAccionDashboardAsync(
            int reservaId,
            string accion,
            string? usuarioRol);

        /// <summary>
        /// Registra la asistencia de un estudiante desde el panel del mesón de atención:
        /// "Presente" (confirmar), "LiberarTemprano" (cancelar) o "Inasistencia" (sancionar).
        /// </summary>
        /// <param name="usuarioId">Identificador del usuario.</param>
        /// <param name="estadoAsistencia">Estado a registrar: "Presente", "LiberarTemprano" o "Inasistencia".</param>
        /// <param name="adminSalaId">Identificador de la sala gestionada por el administrador.</param>
        /// <param name="usuarioRol">Rol del usuario que ejecuta la acción.</param>
        /// <returns>
        /// Tupla con: éxito de la operación, mensaje de error, mensaje de éxito y mensaje de advertencia.
        /// </returns>
        Task<(bool Success, string? ErrorMessage, string? SuccessMessage, string? WarningMessage)> RegistrarAsistenciaAsync(
            int usuarioId,
            string estadoAsistencia,
            int adminSalaId,
            string? usuarioRol);
    }
}
