using System;
using System.Threading.Tasks;

namespace SpottyUTA.Services
{
    public interface IReservasService
    {
        Task<(bool Success, string? ErrorMessage, string? BloqueAsignado)> CrearReservaAsync(
            int salaId,
            int usuarioId,
            string horaInput,
            bool chkTerminosResolved);

        Task<(bool Success, string? ErrorMessage, string? SuccessMessage, string? WarningMessage)> GestionarAccionDashboardAsync(
            int reservaId,
            string accion,
            string? usuarioRol);

        Task<(bool Success, string? ErrorMessage, string? SuccessMessage, string? WarningMessage)> RegistrarAsistenciaAsync(
            int usuarioId,
            string estadoAsistencia,
            int adminSalaId,
            string? usuarioRol);
    }
}
