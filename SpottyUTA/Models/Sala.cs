using System;
using System.Collections.Generic;

namespace SpottyUTA.Models;

/// <summary>
/// Representa una sala o box de estudio dentro del edificio de la biblioteca UTA.
/// </summary>
public partial class Sala
{
    /// <summary>
    /// Identificador único de la sala.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nombre descriptivo del box de estudio (ej. "Box 1", "Box 2").
    /// </summary>
    public string Nombre { get; set; } = null!;

    /// <summary>
    /// Número de piso en el que está situada la sala (1, 2 o 3).
    /// </summary>
    public int Piso { get; set; }

    /// <summary>
    /// Estado operativo actual de la sala ("D" = Disponible, "R" = Reservada, "O" = Ocupada, "M" = Mantenimiento).
    /// </summary>
    public string EstadoActual { get; set; } = null!;

    /// <summary>
    /// Colección de reservas efectuadas sobre esta sala.
    /// </summary>
    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
