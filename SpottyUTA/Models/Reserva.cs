using System;
using System.Collections.Generic;

namespace SpottyUTA.Models;

public partial class Reserva
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int SalaId { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public string EstadoReserva { get; set; } = null!;

    public virtual Sala Sala { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
