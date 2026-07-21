using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Models;

namespace SpottyUTA.Data;

/// <summary>
/// Contexto de acceso a datos de Entity Framework Core para la base de datos Spotty UTA.
/// </summary>
public partial class SpottyUtaContext : DbContext
{
    /// <summary>
    /// Constructor por defecto para inicialización del contexto.
    /// </summary>
    public SpottyUtaContext()
    {
    }

    /// <summary>
    /// Constructor que recibe las opciones de configuración de DbContext.
    /// </summary>
    /// <param name="options">Opciones de configuración de la base de datos.</param>
    public SpottyUtaContext(DbContextOptions<SpottyUtaContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Conjunto de datos para la entidad Reserva.
    /// </summary>
    public virtual DbSet<Reserva> Reservas { get; set; }

    /// <summary>
    /// Conjunto de datos para la entidad Sala.
    /// </summary>
    public virtual DbSet<Sala> Salas { get; set; }

    /// <summary>
    /// Conjunto de datos para la entidad Usuario.
    /// </summary>
    public virtual DbSet<Usuario> Usuarios { get; set; }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reservas__3214EC07046726AA");

            entity.Property(e => e.EstadoReserva)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Sala).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.SalaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservas_Salas");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservas_Usuarios");
        });

        modelBuilder.Entity<Sala>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Salas__3214EC070B60B09E");

            entity.Property(e => e.EstadoActual)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC0730046BB9");

            entity.HasIndex(e => e.CorreoUta, "UQ__Usuarios__980CB6720270BC48").IsUnique();

            entity.Property(e => e.CorreoUta)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("CorreoUTA");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Rol)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
