using System;
using System.Collections.Generic;

namespace SpottyUTA.Models;

/// <summary>
/// Representa la reserva de una sala de estudio asignada a un usuario.
/// </summary>
public partial class Reserva
{
    /// <summary>
    /// Identificador único de la reserva.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificador del usuario que realizó la reserva.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Identificador de la sala reservada.
    /// </summary>
    public int SalaId { get; set; }

    /// <summary>
    /// Fecha de la reserva.
    /// </summary>
    public DateOnly Fecha { get; set; }

    /// <summary>
    /// Hora programada de inicio de uso de la sala.
    /// </summary>
    public TimeOnly HoraInicio { get; set; }

    /// <summary>
    /// Hora programada de término de la reserva.
    /// </summary>
    public TimeOnly HoraFin { get; set; }

    /// <summary>
    /// Estado del ciclo de vida de la reserva ("Activa", "Cancelada", "Completada", "Inasistencia").
    /// </summary>
    public string EstadoReserva { get; set; } = null!;

    /// <summary>
    /// Referencia a la entidad de la sala reservada.
    /// </summary>
    public virtual Sala Sala { get; set; } = null!;

    /// <summary>
    /// Referencia a la entidad del usuario titular de la reserva.
    /// </summary>
    public virtual Usuario Usuario { get; set; } = null!;
}
