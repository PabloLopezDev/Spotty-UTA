using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace SpottyUTA.Controllers

{

    public class HomeController : Controller

    {

        private readonly SpottyUtaContext _context;



        public HomeController(SpottyUtaContext context)

        {

            _context = context;

        }



        public async Task<IActionResult> Index()

        {

            DateTime ahoraDateTime = DateTime.Now;

            DateOnly fechaActual = DateOnly.FromDateTime(ahoraDateTime);

            TimeOnly horaActualTime = TimeOnly.FromDateTime(ahoraDateTime);



            ViewBag.DiaSemana = ahoraDateTime.DayOfWeek;



            // 1. Buscamos por "A" o "Activa" las reservas vigentes o futuras de hoy

            var reservasHoy = await _context.Reservas

            .Where(r => r.Fecha == fechaActual &&

            r.HoraFin >= horaActualTime &&

            (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))

            .OrderBy(r => r.HoraInicio)

            .ToListAsync();



            ViewBag.ReservasHoy = reservasHoy;



            // Traer todas las salas y determinaciones de jornada
            var salas = await _context.Salas.OrderBy(s => s.Piso).ThenBy(s => s.Nombre).ToListAsync();

            // 🚨 REGLA DE APERTURA Y CIERRE DE LA BIBLIOTECA (UTA)
            // Lunes-Viernes: 08:00 - 21:00
            // Sábado: 09:00 - 13:00 (solo 1er piso habilitado)
            // Domingo: cerrado (todas inactivas)

            TimeOnly apertura;
            TimeOnly cierre;
            bool soloPrimerPiso = false;

            if (ahoraDateTime.DayOfWeek == DayOfWeek.Sunday)
            {
                // Domingo: cerrado
                apertura = TimeOnly.MinValue;
                cierre = TimeOnly.MinValue;
            }
            else if (ahoraDateTime.DayOfWeek == DayOfWeek.Saturday)
            {
                apertura = new TimeOnly(9, 0);
                cierre = new TimeOnly(13, 0);
                soloPrimerPiso = true;
            }
            else
            {
                apertura = new TimeOnly(8, 0);
                cierre = new TimeOnly(21, 0);
            }

            // Biblioteca cerrada si es domingo o si la hora actual está fuera del rango de apertura
            bool bibliotecaCerrada = (ahoraDateTime.DayOfWeek == DayOfWeek.Sunday) || (horaActualTime < apertura) || (horaActualTime >= cierre);


            // 2. Cálculo de estados adaptativo en tiempo real

            foreach (var sala in salas)

            {

                // CASO CRÍTICO: Si la biblioteca está cerrada, forzamos estado Inactiva ("I") y saltamos la sala
                if (bibliotecaCerrada)
                {
                    sala.EstadoActual = "I";
                    sala.Reservas = new List<Reserva>();
                    continue;
                }

                // Si es jornada de fin de semana, solo el primer piso está habilitado
                if (soloPrimerPiso && sala.Piso != 1)
                {
                    sala.EstadoActual = "I";
                    sala.Reservas = new List<Reserva>();
                    continue;
                }

                // Si está abierta, calculamos su estado normal (D, R u O)
                var reservasDeEstaSala = reservasHoy.Where(r => r.SalaId == sala.Id).ToList();



                // Caso 1: ¿La hora actual está dentro de una reserva?

                var reservaActual = reservasDeEstaSala.FirstOrDefault(r => horaActualTime >= r.HoraInicio && horaActualTime < r.HoraFin);



                if (reservaActual != null)

                {

                    double minutosTranscurridos = (horaActualTime.ToTimeSpan() - reservaActual.HoraInicio.ToTimeSpan()).TotalMinutes;



                    if (minutosTranscurridos <= 15)

                    {

                        sala.EstadoActual = "R"; // Estado naranjo de espera (tolerancia)

                    }

                    else

                    {

                        sala.EstadoActual = "O"; // Se consolidó como ocupado

                    }

                }

                else

                {

                    // Caso 2: ¿Hay una reserva futura que empiece en menos de 20 minutos?

                    bool proximaReservaCercana = reservasDeEstaSala.Any(r =>

                    r.HoraInicio > horaActualTime &&

                    (r.HoraInicio.ToTimeSpan() - horaActualTime.ToTimeSpan()).TotalMinutes <= 20);



                    if (proximaReservaCercana)

                    {

                        sala.EstadoActual = "R";

                    }

                    else

                    {

                        sala.EstadoActual = "D"; // Totalmente libre

                    }

                }

            }



            return View(salas);

        }



        public IActionResult Privacy() => View();

        public IActionResult Reglamento() => View();

    }

}

