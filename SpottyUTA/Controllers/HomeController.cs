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
        private readonly Services.ISalasService _salasService;

        public HomeController(SpottyUtaContext context, Services.ISalasService salasService)
        {
            _context = context;
            _salasService = salasService;
        }

        public async Task<IActionResult> Index()
        {
            var ahora = DateTime.Now;
            var fechaActual = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            ViewBag.DiaSemana = ahora.DayOfWeek;

            var reservasHoy = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == fechaActual &&
                       r.HoraFin >= horaActual &&
                       (r.EstadoReserva == "A" || r.EstadoReserva == "Activa"))
                .OrderBy(r => r.HoraInicio)
                .ToListAsync();

            ViewBag.ReservasHoy = reservasHoy;

            var salas = await _context.Salas
                .AsNoTracking()
                .OrderBy(s => s.Piso)
                .ThenBy(s => s.Nombre)
                .ToListAsync();

            var horario = _salasService.ObtenerHorarioOperacion(ahora);

            foreach (var sala in salas)
            {
                sala.Reservas = new List<Reserva>();
                sala.EstadoActual = _salasService.ObtenerEstadoSala(sala, reservasHoy, horaActual, horario);
            }

            return View(salas);
        }

        public IActionResult Privacy() => View();

        public IActionResult Reglamento() => View();
    }
}

