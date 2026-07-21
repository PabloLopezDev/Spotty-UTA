using System;
using System.Collections.Generic;

namespace SpottyUTA.Models;

/// <summary>
/// Representa a un usuario (Estudiante o Administrador) registrado en el sistema Spotty UTA.
/// </summary>
public partial class Usuario
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nombre completo del usuario.
    /// </summary>
    public string NombreCompleto { get; set; } = null!;

    /// <summary>
    /// Correo institucional de la Universidad de Tarapacá (@uta.cl o @alumnos.uta.cl).
    /// </summary>
    public string CorreoUta { get; set; } = null!;

    /// <summary>
    /// Contador de inasistencias sin justificar acumuladas por el estudiante.
    /// </summary>
    public int ContadorInasistencias { get; set; }

    /// <summary>
    /// Indica si el usuario se encuentra bloqueado debido a inasistencias o sanción manual.
    /// </summary>
    public bool EstaBloqueado { get; set; }

    /// <summary>
    /// Rol asignado en el sistema ("Estudiante" o "Administrador").
    /// </summary>
    public string Rol { get; set; } = null!;

    /// <summary>
    /// Colección de reservas asociadas al usuario.
    /// </summary>
    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
