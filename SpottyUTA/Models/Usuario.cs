using System;
using System.Collections.Generic;

namespace SpottyUTA.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string CorreoUta { get; set; } = null!;

    public int ContadorInasistencias { get; set; }

    public bool EstaBloqueado { get; set; }

    public string Rol { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
