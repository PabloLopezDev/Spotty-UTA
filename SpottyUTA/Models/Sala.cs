using System;
using System.Collections.Generic;

namespace SpottyUTA.Models;

public partial class Sala
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int Piso { get; set; }

    public string EstadoActual { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
